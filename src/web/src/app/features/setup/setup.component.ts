import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { of, switchMap } from 'rxjs';
import {
  CustomShareDto,
  ExpenseBoardDto,
  ExpenseRecurrenceRequest,
  IbanMode,
  InstallmentDto,
  InstallmentRequest,
  PartnerDto,
  PlanDto,
  PlanExportDto,
  PlanInviteDto,
  PlanMemberDto,
  PlanTemplatePreviewDto,
  PlanDocumentPreviewDto,
  ShareType,
  TemplateListItemDto,
} from '../../core/models/api.models';
import { ExpensesApi } from '../../core/services/expenses.api';
import { InstallmentsApi } from '../../core/services/installments.api';
import { MembershipApi } from '../../core/services/membership.api';
import { PartnersApi } from '../../core/services/partners.api';
import { PlanContextService } from '../../core/services/plan-context.service';
import { PlansApi } from '../../core/services/plans.api';
import { TemplatesApi } from '../../core/services/templates.api';
import { AuthService } from '../../core/services/auth.service';
import { ShareService } from '../../core/platform/share.service';
import { isExpensePlan, planHomeCommands } from '../../core/utils/plan-routes';
import { CurrencyTryPipe } from '../../shared/pipes/currency-try.pipe';
import { MoneyInputDirective } from '../../shared/directives/money-input.directive';
import { ToastService } from '../../shared/toast/toast.service';
import { ConfirmService } from '../../shared/confirm/confirm.service';
import { formatDateTr, shareTypeLabel, shareTypeToNumber } from '../../shared/utils/format';
import { formatMoneyTr } from '../../shared/utils/money';
import { apiErrorMessage } from '../../shared/utils/api-error';
import { IconTrashComponent } from '../../shared/icons/icon-trash.component';

interface PartnerEditRow {
  id: string;
  name: string;
  color: string;
  defaultPct: number;
  sortOrder: number;
  linkedUserId: string | null;
  iban: string;
  inviteEmail: string;
}

interface PreviewPartnerDraft {
  name: string;
  color: string;
  defaultPct: number;
}

interface PreviewRowDraft {
  index: number;
  name: string;
  dueDate: string;
  totalAmount: number;
}

interface PreviewDraft {
  key: string;
  title: string;
  description: string;
  deliveryIndex: number;
  partners: PreviewPartnerDraft[];
  rows: PreviewRowDraft[];
  sourceLabel?: string;
  warnings?: string[];
}

const PARTNER_COLORS = ['#38bdf8', '#fb923c', '#a855f7', '#ec4899', '#10b981', '#f59e0b'];

@Component({
  selector: 'app-setup',
  standalone: true,
  imports: [FormsModule, RouterLink, CurrencyTryPipe, MoneyInputDirective, IconTrashComponent],
  templateUrl: './setup.component.html',
  styleUrl: './setup.component.scss',
})
export class SetupComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly plansApi = inject(PlansApi);
  private readonly partnersApi = inject(PartnersApi);
  private readonly installmentsApi = inject(InstallmentsApi);
  private readonly expensesApi = inject(ExpensesApi);
  private readonly membershipApi = inject(MembershipApi);
  private readonly templatesApi = inject(TemplatesApi);
  private readonly planContext = inject(PlanContextService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);
  private readonly auth = inject(AuthService);
  private readonly share = inject(ShareService);

  readonly plan = signal<PlanDto | null>(null);
  /** Loaded plan wins; otherwise shell context (set before navigate) so layout stays stable. */
  readonly isExpensePlan = computed(() => {
    const loaded = this.plan();
    if (loaded) {
      return isExpensePlan(loaded);
    }
    return isExpensePlan(this.planContext.planType());
  });
  readonly partners = signal<PartnerDto[]>([]);
  readonly partnerRows = signal<PartnerEditRow[]>([]);
  readonly installments = signal<InstallmentDto[]>([]);
  readonly expenseBoard = signal<ExpenseBoardDto | null>(null);
  readonly members = signal<PlanMemberDto[]>([]);
  readonly invites = signal<PlanInviteDto[]>([]);
  readonly templates = signal<TemplateListItemDto[]>([]);
  readonly previewDraft = signal<PreviewDraft | null>(null);
  readonly showInstModal = signal(false);
  readonly loading = signal(true);
  readonly busy = signal(false);
  /** Plan structure edits (name, partners, installments, templates) — owner only. */
  readonly isOwner = signal(false);

  /** Current user already claimed a partner row — hide “Bu benim” elsewhere. */
  readonly selfAlreadyLinked = computed(() => {
    const uid = this.auth.getSessionUserId();
    if (!uid) {
      return false;
    }
    return this.partnerRows().some((p) => p.linkedUserId === uid);
  });

  readonly formatDateTr = formatDateTr;
  readonly shareTypeLabel = shareTypeLabel;

  title = '';
  description = '';
  deliveryInstallmentId = '';
  requireReceipt = false;
  ibanMode: IbanMode = 'None';
  settlementIban = '';
  remindersEnabled = false;
  reminderDaysBefore: number[] = [];
  reminderDaysAfter: number[] = [];
  readonly beforeDayOptions = [1, 3, 7, 10, 15];
  readonly afterDayOptions = [1, 3, 7, 15, 30];

  instName = '';
  instDueDate = '';
  instAmount = 0;
  instShareType = 0;
  editingInstallmentId: string | null = null;
  customShareAmounts: Record<string, number> = {};

  expName = '';
  expAmount: number | null = null;
  /** UI: Equal / Default / sole (tek ortak) / Custom */
  expShareUi: 'Equal' | 'Default' | 'sole' | 'Custom' = 'Equal';
  expSolePartnerId = '';
  expCustomShares: Record<string, number> = {};
  expPaidBy = '';
  expCategoryId = '';
  recFrequency: 'Monthly' | 'Weekly' | 'Yearly' = 'Monthly';
  recAnchorDay = 1;
  recStart = new Date().toISOString().slice(0, 10);

  newCategoryName = '';

  planId = '';

  ngOnInit(): void {
    this.templatesApi.list().subscribe({
      next: (list) => this.templates.set(list),
      error: () => this.templates.set([]),
    });
    this.route.paramMap.subscribe((params) => {
      const id = params.get('id') ?? '';
      if (!id) {
        void this.router.navigate(['/plans'], { queryParams: { manage: '1' } });
        return;
      }
      if (id === this.planId && this.plan()) {
        return;
      }
      this.planId = id;
      this.reload();
    });
  }

  reload(): void {
    this.loading.set(true);
    this.plan.set(null);
    this.expenseBoard.set(null);
    this.isOwner.set(false);
    this.plansApi.get(this.planId).subscribe({
      next: (plan) => {
        this.plan.set(plan);
        this.title = plan.title;
        this.description = plan.description;
        this.deliveryInstallmentId = plan.deliveryInstallmentId ?? '';
        this.requireReceipt = !!plan.requireReceipt;
        this.ibanMode = plan.ibanMode ?? 'None';
        this.settlementIban = plan.settlementIban ?? '';
        this.remindersEnabled = !!plan.remindersEnabled;
        this.reminderDaysBefore = [...(plan.reminderDaysBefore ?? [])];
        this.reminderDaysAfter = [...(plan.reminderDaysAfter ?? [])];
        this.planContext.setPlan(plan.id, plan.title, plan.description, plan.planType);
        this.resolveIsOwner(plan);
        this.partnersApi.list(this.planId).subscribe({
          next: (partners) => {
            this.applyPartners(partners);
            if (isExpensePlan(plan)) {
              this.installments.set([]);
              this.loadExpenseBoard();
              this.loadMembership();
              return;
            }
            this.expenseBoard.set(null);
            this.installmentsApi.list(this.planId).subscribe({
              next: (installments) => {
                this.installments.set(installments);
                this.loadMembership();
              },
              error: (err) => this.fail(err, 'Taksitler yüklenemedi.'),
            });
          },
          error: (err) => this.fail(err, 'Ortaklar yüklenemedi.'),
        });
      },
      error: (err) => {
        this.fail(err, 'Plan yüklenemedi.');
        if (err?.status === 404 || /not found/i.test(String(err?.error?.detail ?? ''))) {
          this.planContext.clear();
          void this.router.navigate(['/plans'], { queryParams: { manage: '1' } });
        }
      },
    });
  }

  private resolveIsOwner(_plan: PlanDto): void {
    this.plansApi.dashboard(this.planId).subscribe({
      next: (d) => this.isOwner.set(!!d.isOwner),
      error: () => this.isOwner.set(false),
    });
  }

  private ensureOwnerAction(): boolean {
    if (this.isOwner()) {
      return true;
    }
    this.toast.error('Bu değişikliği yalnızca planı kuran kişi yapabilir.');
    return false;
  }

  private applyPartners(partners: PartnerDto[]): void {
    this.partners.set(partners);
    this.partnerRows.set(
      partners.map((p) => ({
        id: p.id,
        name: p.name,
        color: p.color,
        defaultPct: p.defaultPct,
        sortOrder: p.sortOrder,
        linkedUserId: p.linkedUserId,
        iban: p.iban ?? '',
        inviteEmail: p.inviteEmail ?? '',
      }))
    );
    if (!this.expPaidBy && partners[0]) {
      this.expPaidBy = partners[0].id;
    }
    if (!this.expSolePartnerId && partners[0]) {
      this.expSolePartnerId = partners[0].id;
    }
    this.syncExpCustomShares();
  }

  private loadExpenseBoard(): void {
    this.expensesApi.board(this.planId).subscribe({
      next: (board) => {
        this.expenseBoard.set(board);
        if (board.isOwner != null) {
          this.isOwner.set(!!board.isOwner);
        }
        if (!this.expCategoryId && board.categories[0]) {
          this.expCategoryId = board.categories[0].id;
        }
      },
      error: (err) => this.toast.error(apiErrorMessage(err, 'Gider ayarları yüklenemedi.')),
    });
  }

  frequencyLabel(f: string | number): string {
    if (f === 'Weekly' || f === 1) {
      return 'Haftalık';
    }
    if (f === 'Yearly' || f === 2) {
      return 'Yıllık';
    }
    return 'Aylık';
  }

  onExpShareUiChange(): void {
    if (this.expShareUi === 'sole' && !this.expSolePartnerId) {
      this.expSolePartnerId = this.partners()[0]?.id ?? '';
    }
    if (this.expShareUi === 'Custom') {
      this.syncExpCustomShares(true);
    }
  }

  onExpAmountChange(): void {
    if (this.expShareUi === 'Custom') {
      this.syncExpCustomShares(true);
    }
  }

  private syncExpCustomShares(forceEqualSplit = false): void {
    const partners = this.partners();
    if (!partners.length) {
      return;
    }
    const total = Number(this.expAmount || 0);
    const next: Record<string, number> = { ...this.expCustomShares };
    const equal = partners.length ? Math.round((total / partners.length) * 100) / 100 : 0;
    let assigned = 0;
    partners.forEach((p, i) => {
      if (forceEqualSplit || next[p.id] == null) {
        if (i === partners.length - 1) {
          next[p.id] = Math.round((total - assigned) * 100) / 100;
        } else {
          next[p.id] = equal;
          assigned += equal;
        }
      }
    });
    this.expCustomShares = next;
  }

  expCustomShareSum(): number {
    return this.partners().reduce((s, p) => s + (Number(this.expCustomShares[p.id]) || 0), 0);
  }

  expCustomSharesMatch(): boolean {
    return Math.abs(this.expCustomShareSum() - Number(this.expAmount || 0)) <= 0.01;
  }

  private buildExpenseShares(total: number): {
    shareType: ShareType;
    customShares: CustomShareDto[];
  } | null {
    if (this.expShareUi === 'sole') {
      if (!this.expSolePartnerId) {
        this.toast.error('Pay sahibi ortağını seçin.');
        return null;
      }
      return {
        shareType: 'Custom',
        customShares: this.partners().map((p) => ({
          partnerId: p.id,
          amount: p.id === this.expSolePartnerId ? total : 0,
        })),
      };
    }
    if (this.expShareUi === 'Custom') {
      if (!this.expCustomSharesMatch()) {
        this.toast.error('Özel payların toplamı tutara eşit olmalı.');
        return null;
      }
      return {
        shareType: 'Custom',
        customShares: this.partners().map((p) => ({
          partnerId: p.id,
          amount: Number(this.expCustomShares[p.id] || 0),
        })),
      };
    }
    return {
      shareType: this.expShareUi === 'Default' ? 'Default' : 'Equal',
      customShares: [],
    };
  }

  addRecurrence(): void {
    if (!this.ensureOwnerAction()) {
      return;
    }
    const total = Number(this.expAmount);
    if (!this.expName.trim() || !(total > 0)) {
      this.toast.error('Tekrarlayan gider adı ve tutarı gerekli.');
      return;
    }
    const shares = this.buildExpenseShares(total);
    if (!shares) {
      return;
    }
    const body: ExpenseRecurrenceRequest = {
      name: this.expName.trim(),
      totalAmount: total,
      shareType: shares.shareType,
      categoryId: this.expCategoryId || null,
      defaultPaidByPartnerId: this.expPaidBy || null,
      frequency: this.recFrequency,
      anchorDay: this.recAnchorDay,
      startDate: this.recStart,
      endDate: null,
      customShares: shares.customShares,
    };
    this.busy.set(true);
    this.expensesApi.createRecurrence(this.planId, body).subscribe({
      next: () => {
        this.busy.set(false);
        this.expName = '';
        this.expAmount = null;
        this.toast.success('Tekrarlayan şablon eklendi — dönemler otomatik oluşur.');
        this.loadExpenseBoard();
      },
      error: (err) => this.fail(err, 'Tekrarlayan gider eklenemedi.'),
    });
  }

  async removeRecurrence(id: string): Promise<void> {
    if (!this.ensureOwnerAction()) {
      return;
    }
    if (
      !(await this.confirm.ask({
        title: 'Tekrarlayanı sil',
        message: 'Şablon silinsin mi? Oluşmuş giderler kalır.',
        danger: true,
      }))
    ) {
      return;
    }
    this.expensesApi.deleteRecurrence(this.planId, id).subscribe({
      next: () => {
        this.toast.success('Şablon silindi.');
        this.loadExpenseBoard();
      },
      error: (err) => this.fail(err, 'Silinemedi.'),
    });
  }

  addCategory(): void {
    if (!this.ensureOwnerAction()) {
      return;
    }
    const name = this.newCategoryName.trim();
    if (!name) {
      return;
    }
    this.busy.set(true);
    this.expensesApi.createCategory(this.planId, name).subscribe({
      next: () => {
        this.busy.set(false);
        this.newCategoryName = '';
        this.toast.success('Kategori eklendi.');
        this.loadExpenseBoard();
      },
      error: (err) => this.fail(err, 'Kategori eklenemedi.'),
    });
  }

  /** Refresh partners without blanking the whole Setup page. */
  private refreshPartners(message?: string): void {
    this.partnersApi.list(this.planId).subscribe({
      next: (partners) => {
        this.busy.set(false);
        this.applyPartners(partners);
        if (message) {
          this.toast.success(message);
        }
      },
      error: (err) => this.fail(err, 'Ortaklar yenilenemedi.'),
    });
  }

  private loadMembership(): void {
    this.membershipApi.members(this.planId).subscribe({
      next: (members) => {
        this.members.set(members);
        this.membershipApi.invites(this.planId).subscribe({
          next: (invites) => {
            this.invites.set(invites);
            this.loading.set(false);
            this.openFromQuery();
          },
          error: () => {
            this.invites.set([]);
            this.loading.set(false);
            this.openFromQuery();
          },
        });
      },
      error: (err) => this.fail(err, 'Üyeler yüklenemedi.'),
    });
  }

  private openFromQuery(): void {
    const add = this.route.snapshot.queryParamMap.get('add');
    const edit = this.route.snapshot.queryParamMap.get('edit');
    if (add === '1') {
      // Open after view settles so the modal is not wiped by loading teardown.
      queueMicrotask(() => this.openAddInstallment());
      void this.router.navigate([], { relativeTo: this.route, queryParams: {}, replaceUrl: true });
      return;
    }
    if (edit) {
      const needle = edit.toLowerCase();
      const inst = this.installments().find((i) => i.id.toLowerCase() === needle);
      if (inst) {
        queueMicrotask(() => this.startEditInstallment(inst));
      } else {
        this.toast.error('Düzenlenecek taksit bulunamadı.');
      }
      void this.router.navigate([], { relativeTo: this.route, queryParams: {}, replaceUrl: true });
    }
  }

  templateIcon(key: string): string {
    switch (key) {
      case 'fuzul':
        return '🏠';
      case 'eminevim':
        return '🏡';
      case 'birevim':
        return '🚗';
      case 'katilimevim':
        return '🏢';
      case 'sinpas':
        return '🏬';
      case 'couple':
        return '💑';
      case 'trip':
        return '🏖️';
      case 'teamlunch':
        return '🍽️';
      case 'empty':
        return '📄';
      default:
        return '📋';
    }
  }

  isExpensePreviewKey(key: string): boolean {
    return key === 'couple' || key === 'trip' || key === 'teamlunch';
  }

  openPreview(key: string): void {
    if (key === 'empty') {
      void this.createInstallmentPlan();
      return;
    }
    this.busy.set(true);
    this.templatesApi.preview(key).subscribe({
      next: (dto) => {
        this.busy.set(false);
        this.previewDraft.set(this.toPreviewDraft(dto));
      },
      error: (err) => this.fail(err, 'Önizleme yüklenemedi.'),
    });
  }

  onPlanDocumentSelected(event: Event): void {
    if (!this.ensureOwnerAction()) {
      return;
    }
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) {
      return;
    }
    const lower = file.name.toLowerCase();
    if (!/\.(pdf|xlsx|xls|csv)$/.test(lower)) {
      this.toast.error('PDF, Excel (.xlsx) veya CSV yükleyin.');
      return;
    }
    this.busy.set(true);
    this.plansApi.parseDocument(this.planId, file).subscribe({
      next: (dto) => {
        this.busy.set(false);
        this.previewDraft.set(this.documentToPreviewDraft(dto));
        this.toast.success(`${dto.installmentCount} taksit önizlendi — kontrol edip aktarın.`);
      },
      error: (err) => this.fail(err, 'Dosya okunamadı.'),
    });
  }

  private documentToPreviewDraft(dto: PlanDocumentPreviewDto): PreviewDraft {
    const partners: PreviewPartnerDraft[] = (dto.partners?.length ? dto.partners : []).map((p) => ({
      name: p.name,
      color: p.color,
      defaultPct: Number(p.defaultPct) || 0,
    }));
    if (!partners.length) {
      partners.push(
        { name: 'Ortak 1', color: '#38bdf8', defaultPct: 50 },
        { name: 'Ortak 2', color: '#fb923c', defaultPct: 50 }
      );
    }
    const rows: PreviewRowDraft[] = (dto.installments ?? []).map((row, idx) => ({
      index: row.index ?? idx + 1,
      name: row.name,
      dueDate: String(row.dueDate).slice(0, 10),
      totalAmount: Number(row.totalAmount) || 0,
    }));
    return {
      key: 'document',
      title: dto.title,
      description: dto.description,
      deliveryIndex:
        dto.deliveryIndex >= 0 && dto.deliveryIndex < rows.length ? dto.deliveryIndex : -1,
      partners,
      rows,
      sourceLabel: `${dto.sourceKind} · ${dto.sourceFileName}`,
      warnings: dto.warnings ?? [],
    };
  }

  private toPreviewDraft(dto: PlanTemplatePreviewDto): PreviewDraft {
    const partners: PreviewPartnerDraft[] = Array.isArray(dto.partners)
      ? dto.partners.map((p) => ({
          name: p.name,
          color: p.color,
          defaultPct: Number(p.defaultPct) || 0,
        }))
      : [
          { name: 'Ortak 1', color: '#38bdf8', defaultPct: 50 },
          { name: 'Ortak 2', color: '#fb923c', defaultPct: 50 },
        ];
    const rows: PreviewRowDraft[] = (Array.isArray(dto.installments) ? dto.installments : []).map(
      (row, idx) => ({
        index: row.index ?? idx + 1,
        name: row.name,
        dueDate: String(row.dueDate).slice(0, 10),
        totalAmount: Number(row.totalAmount) || 0,
      })
    );
    return {
      key: dto.key,
      title: dto.title,
      description: dto.description,
      deliveryIndex:
        dto.deliveryIndex >= 0 && dto.deliveryIndex < rows.length ? dto.deliveryIndex : -1,
      partners,
      rows,
    };
  }

  closePreview(): void {
    this.previewDraft.set(null);
  }

  previewGrandTotal(): number {
    const d = this.previewDraft();
    if (!d) {
      return 0;
    }
    return d.rows.reduce((sum, r) => sum + (Number(r.totalAmount) || 0), 0);
  }

  previewPerPartner(amount: number): number {
    const n = this.previewDraft()?.partners.length || 1;
    return Math.round((Number(amount) / n) * 100) / 100;
  }

  previewDeliveryName(): string {
    const d = this.previewDraft();
    if (!d || d.deliveryIndex < 0 || d.deliveryIndex >= d.rows.length) {
      return '—';
    }
    return d.rows[d.deliveryIndex]?.name || '—';
  }

  addPreviewPartner(): void {
    const d = this.previewDraft();
    if (!d || d.partners.length >= 8) {
      if (d && d.partners.length >= 8) {
        this.toast.error('En fazla 8 ortak eklenebilir.');
      }
      return;
    }
    const i = d.partners.length;
    d.partners.push({
      name: `Ortak ${i + 1}`,
      color: PARTNER_COLORS[i % PARTNER_COLORS.length],
      defaultPct: 0,
    });
    this.equalizePreviewPartnerPct(d);
    this.previewDraft.set({
      ...d,
      partners: d.partners.map((p) => ({ ...p })),
    });
  }

  removePreviewPartner(idx: number): void {
    const d = this.previewDraft();
    if (!d || d.partners.length <= 1) {
      return;
    }
    d.partners.splice(idx, 1);
    this.equalizePreviewPartnerPct(d);
    this.previewDraft.set({
      ...d,
      partners: d.partners.map((p) => ({ ...p })),
    });
  }

  private equalizePreviewPartnerPct(d: PreviewDraft): void {
    const n = d.partners.length;
    if (n === 0) {
      return;
    }
    const base = Math.floor((10000 / n)) / 100;
    let assigned = 0;
    d.partners.forEach((p, i) => {
      if (i === n - 1) {
        p.defaultPct = Math.round((100 - assigned) * 100) / 100;
      } else {
        p.defaultPct = base;
        assigned += base;
      }
    });
  }

  addPreviewRow(): void {
    const d = this.previewDraft();
    if (!d) {
      return;
    }
    const last = d.rows[d.rows.length - 1];
    const nextDate = last?.dueDate ? this.addMonthsIso(last.dueDate, 1) : new Date().toISOString().slice(0, 10);
    d.rows.push({
      index: d.rows.length + 1,
      name: this.isExpensePreviewKey(d.key) ? `${d.rows.length + 1}. Gider` : `${d.rows.length + 1}. Taksit`,
      dueDate: nextDate,
      totalAmount: last?.totalAmount ?? 0,
    });
    this.reindexPreviewRows(d);
    this.previewDraft.set({ ...d, rows: [...d.rows] });
  }

  removePreviewRow(idx: number): void {
    const d = this.previewDraft();
    if (!d || d.rows.length <= 1) {
      return;
    }
    d.rows.splice(idx, 1);
    if (d.deliveryIndex >= 0 && d.deliveryIndex >= d.rows.length) {
      d.deliveryIndex = d.rows.length - 1;
    } else if (d.deliveryIndex >= idx && d.deliveryIndex > 0) {
      d.deliveryIndex -= 1;
    }
    this.reindexPreviewRows(d);
    this.previewDraft.set({ ...d, rows: [...d.rows] });
  }

  private reindexPreviewRows(d: PreviewDraft): void {
    d.rows.forEach((r, i) => {
      r.index = i + 1;
    });
  }

  private addMonthsIso(iso: string, months: number): string {
    const d = new Date(`${iso.slice(0, 10)}T00:00:00`);
    d.setMonth(d.getMonth() + months);
    return d.toISOString().slice(0, 10);
  }

  touchPreview(): void {
    const d = this.previewDraft();
    if (!d) {
      return;
    }
    this.previewDraft.set({
      ...d,
      partners: d.partners.map((p) => ({ ...p })),
      rows: d.rows.map((r) => ({ ...r })),
    });
  }

  async importPreviewDraft(): Promise<void> {
    const d = this.previewDraft();
    if (!d) {
      return;
    }
    if (d.key === 'couple' || d.key === 'trip' || d.key === 'teamlunch') {
      if (!d.title.trim() || d.partners.length < 1 || !d.rows.length) {
        this.toast.error('Başlık, en az bir ortak ve bir gider satırı gerekli.');
        return;
      }
      const pctSum = d.partners.reduce((s, p) => s + (Number(p.defaultPct) || 0), 0);
      if (Math.abs(pctSum - 100) > 0.05) {
        this.toast.error(`Ortak pay yüzdeleri toplamı 100 olmalı (şu an ${pctSum}).`);
        return;
      }
      if (
        !(await this.confirm.ask({
          title: 'Örnek planı aç',
          message: `“${d.title.trim()}” yeni bir ortak gider planı olarak açılacak (${d.rows.length} kalem). Mevcut planınız yerinde kalır.`,
          confirmLabel: 'Aç',
          success: true,
        }))
      ) {
        return;
      }
      await this.createExpenseSample(d.key as 'couple' | 'trip' | 'teamlunch', true);
      return;
    }
    if (!d.title.trim() || !d.rows.length || !d.partners.length) {
      this.toast.error('Başlık, ortak ve en az bir taksit gerekli.');
      return;
    }
    const pctSum = d.partners.reduce((s, p) => s + (Number(p.defaultPct) || 0), 0);
    if (Math.abs(pctSum - 100) > 0.05) {
      this.toast.error(`Ortak pay yüzdeleri toplamı 100 olmalı (şu an ${pctSum}).`);
      return;
    }
    const asNewPlan = d.key !== 'document';
    if (!asNewPlan && !this.ensureOwnerAction()) {
      return;
    }
    if (
      !(await this.confirm.ask({
        title: d.key === 'document' ? 'Dosyadan aktar' : 'Yeni plan aç',
        message: asNewPlan
          ? `“${d.title.trim()}” yeni bir taksit planı olarak açılacak. Mevcut planınız yerinde kalır.`
          : `“${d.title.trim()}” dosya önizlemesi mevcut ortak/taksit verisinin üzerine yazılacak. Onaylıyor musunuz?`,
        confirmLabel: asNewPlan ? 'Yeni plan aç' : 'Aktar',
        success: true,
        danger: !asNewPlan,
      }))
    ) {
      return;
    }

    const partnerIds = d.partners.map(() => crypto.randomUUID());
    const installmentIds = d.rows.map(() => crypto.randomUUID());
    const deliveryId =
      d.deliveryIndex >= 0 && d.deliveryIndex < installmentIds.length
        ? installmentIds[d.deliveryIndex]
        : null;

    const payload: PlanExportDto = {
      title: d.title.trim(),
      description: d.description.trim(),
      deliveryInstallmentId: deliveryId,
      partners: d.partners.map((p, i) => ({
        id: partnerIds[i],
        name: p.name.trim() || `Ortak ${i + 1}`,
        color: p.color || PARTNER_COLORS[i % PARTNER_COLORS.length],
        defaultPct: Number(p.defaultPct) || 0,
        sortOrder: i,
      })),
      installments: d.rows.map((r, i) => ({
        id: installmentIds[i],
        name: r.name.trim() || `${i + 1}. Taksit`,
        dueDate: r.dueDate,
        totalAmount: Number(r.totalAmount) || 0,
        shareType: 'Default',
        sortOrder: i,
        customShares: [],
        payments: [],
      })),
    };

    this.busy.set(true);
    const import$ = asNewPlan
      ? this.plansApi
          .create({
            title: payload.title,
            description: payload.description,
            planType: 'Installment',
          })
          .pipe(switchMap((plan) => this.plansApi.import(plan.id, payload)))
      : this.plansApi.import(this.planId, payload);

    import$.subscribe({
      next: (plan) => {
        this.busy.set(false);
        this.closePreview();
        if (asNewPlan) {
          this.planContext.setPlan(plan.id, plan.title, plan.description, plan.planType);
          this.toast.success(`Yeni plan açıldı: ${plan.title}`);
          void this.router.navigate(planHomeCommands(plan.id, plan.planType));
          return;
        }
        this.toast.success('Dosya plana aktarıldı.');
        this.reload();
      },
      error: (err) => this.fail(err, 'İçe aktarma başarısız.'),
    });
  }

  async createExpensePlan(): Promise<void> {
    if (
      !(await this.confirm.ask({
        title: 'Yeni gider planı',
        message:
          'Yeni bir Ortak Gider Planı açılacak. Bu planın yerine geçmez; Tüm Planlar’dan her ikisine de dönebilirsiniz.',
        confirmLabel: 'Gider planı aç',
        success: true,
      }))
    ) {
      return;
    }
    this.busy.set(true);
    this.plansApi
      .create({
        title: 'Ortak Gider Planı',
        description: 'Ev / ofis ortak harcamaları',
        planType: 'Expense',
      })
      .subscribe({
        next: (plan) => {
          this.busy.set(false);
          const planType = plan.planType ?? 'Expense';
          this.planContext.setPlan(plan.id, plan.title, plan.description, planType);
          this.toast.success('Ortak gider planı oluşturuldu.');
          void this.router.navigate(planHomeCommands(plan.id, planType));
        },
        error: (err) => this.fail(err, 'Gider planı oluşturulamadı.'),
      });
  }

  async createCoupleExpenseSample(fromPreview = false): Promise<void> {
    await this.createExpenseSample('couple', fromPreview);
  }

  async createExpenseSample(key: 'couple' | 'trip' | 'teamlunch', fromPreview = false): Promise<void> {
    const draft = fromPreview ? this.previewDraft() : null;
    const labels: Record<string, { title: string; confirm: string; fallbackTitle: string; fallbackDesc: string }> = {
      couple: {
        title: 'Örnek karı-koca planı',
        confirm:
          'Yeni bir örnek gider planı açılacak (rastgele isimler, fatura/market/mahsup). Mevcut planınız yerinde kalır.',
        fallbackTitle: 'Ev Ortak Giderleri',
        fallbackDesc: 'Örnek karı-koca ortak gider planı',
      },
      trip: {
        title: 'Örnek tatil planı',
        confirm:
          'Yeni bir tatil / arkadaş grubu gider planı açılacak (konaklama, yemek, ulaşım). Mevcut planınız yerinde kalır.',
        fallbackTitle: 'Tatil Harcama Defteri',
        fallbackDesc: 'Örnek tatil / arkadaş grubu harcama planı',
      },
      teamlunch: {
        title: 'Örnek ekip yemeği',
        confirm:
          'Yeni bir ekip öğle yemeği planı açılacak (eşit pay, yemek kartı örneği). Mevcut planınız yerinde kalır.',
        fallbackTitle: 'Ekip Öğle Yemeği',
        fallbackDesc: 'Örnek ekip yemeği mahsup planı',
      },
    };
    const meta = labels[key];
    if (
      !fromPreview &&
      !(await this.confirm.ask({
        title: meta.title,
        message: meta.confirm,
        confirmLabel: 'Örnekle aç',
        success: true,
      }))
    ) {
      return;
    }
    this.busy.set(true);
    const title = draft?.title?.trim() || meta.fallbackTitle;
    const description = draft?.description?.trim() || meta.fallbackDesc;
    const seedBody = draft
      ? {
          title,
          description,
          partners: draft.partners.map((p) => ({
            name: p.name.trim(),
            color: p.color,
            defaultPct: Number(p.defaultPct) || 0,
          })),
          expenses: draft.rows.map((r) => ({
            name: r.name.trim() || 'Gider',
            occurredOn: r.dueDate,
            totalAmount: Number(r.totalAmount) || 0,
          })),
        }
      : undefined;
    this.plansApi
      .create({
        title,
        description,
        planType: 'Expense',
      })
      .pipe(switchMap((plan) => this.plansApi.seed(plan.id, key, seedBody)))
      .subscribe({
        next: (plan) => {
          this.busy.set(false);
          this.closePreview();
          const planType = plan.planType ?? 'Expense';
          this.planContext.setPlan(plan.id, plan.title, plan.description, planType);
          this.toast.success(`Örnek plan hazır: ${plan.title}`);
          void this.router.navigate(planHomeCommands(plan.id, planType));
        },
        error: (err) => this.fail(err, 'Örnek gider planı oluşturulamadı.'),
      });
  }

  /** Opens a brand-new installment plan (optionally seeded). Does not replace the current plan. */
  async createInstallmentPlan(seedKey?: string): Promise<void> {
    const t = seedKey ? this.templates().find((x) => x.key === seedKey) : null;
    const label = t?.title ?? 'Yeni taksit planı';
    if (
      !(await this.confirm.ask({
        title: 'Yeni taksit planı',
        message: seedKey
          ? `“${label}” ile yeni bir taksit planı açılacak. Mevcut planınız yerinde kalır.`
          : 'Boş bir taksit planı açılacak. Mevcut planınız yerinde kalır; Tüm Planlar’dan geçiş yapabilirsiniz.',
        confirmLabel: 'Taksit planı aç',
        success: true,
      }))
    ) {
      return;
    }
    this.busy.set(true);
    this.plansApi
      .create({
        title: t?.title ?? 'Yeni Özel Plan',
        description: t?.description ?? 'Özel takip planı',
        planType: 'Installment',
      })
      .pipe(switchMap((plan) => (seedKey ? this.plansApi.seed(plan.id, seedKey) : of(plan))))
      .subscribe({
        next: (plan) => {
          this.busy.set(false);
          this.planContext.setPlan(plan.id, plan.title, plan.description, plan.planType);
          this.toast.success(`${label} oluşturuldu.`);
          void this.router.navigate(['/plans', plan.id, seedKey && seedKey !== 'empty' ? 'dashboard' : 'setup']);
        },
        error: (err) => this.fail(err, 'Taksit planı oluşturulamadı.'),
      });
  }

  async seedTemplate(key: string): Promise<void> {
    // Expense plans cannot host installment templates — open a new installment plan instead.
    if (this.isExpensePlan()) {
      await this.createInstallmentPlan(key === 'empty' ? undefined : key);
      return;
    }
    if (!this.ensureOwnerAction()) {
      return;
    }

    const t = this.templates().find((x) => x.key === key);
    const label = t?.title ?? key;
    const msg =
      key === 'empty'
        ? 'Tüm veriler temizlenip boş bir plan oluşturulacak. Emin misiniz?'
        : `${label} şablonu yüklenecek. Mevcut verileriniz yenilenecek. Onaylıyor musunuz?`;
    if (
      !(await this.confirm.ask({
        title: key === 'empty' ? 'Boş plana geç' : 'Şablon yükle',
        message: msg,
        confirmLabel: key === 'empty' ? 'Temizle' : 'Yükle',
        danger: key === 'empty',
        success: key !== 'empty',
      }))
    ) {
      return;
    }
    this.busy.set(true);
    this.plansApi.seed(this.planId, key).subscribe({
      next: () => {
        this.busy.set(false);
        this.toast.success(`${label} yüklendi.`);
        this.reload();
      },
      error: (err) => this.fail(err, 'Şablon yüklenemedi.'),
    });
  }

  inviteLink(invite: PlanInviteDto): string {
    const origin = typeof window !== 'undefined' ? window.location.origin : '';
    return `${origin}/invite/${invite.token}`;
  }

  async copyInviteLink(invite: PlanInviteDto): Promise<void> {
    const link = this.inviteLink(invite);
    try {
      const shared = await this.share.share({
        title: 'PayDefteri plan daveti',
        text: `${invite.partnerName} olarak plana katıl`,
        url: link,
      });
      this.toast.success(shared ? 'Davet paylaşılmaya hazır.' : `Link: ${link}`);
    } catch {
      this.toast.success(`Link: ${link}`);
    }
  }

  resendInvite(invite: PlanInviteDto): void {
    if (!this.ensureOwnerAction()) {
      return;
    }
    this.busy.set(true);
    this.membershipApi.resendInvite(this.planId, invite.id).subscribe({
      next: (updated) => {
        this.busy.set(false);
        const link = this.inviteLink(updated);
        if (updated.emailSent) {
          this.toast.success(`Davet e-postası ${updated.email} adresine yeniden gönderildi.`);
        } else {
          this.toast.success(
            `Davet yenilendi (e-posta şu an kapalı veya gönderilemedi). Link: ${link}`
          );
        }
        this.loadMembership();
      },
      error: (err) => this.fail(err, 'Davet yeniden gönderilemedi.'),
    });
  }

  memberInitial(m: PlanMemberDto): string {
    const label = this.memberLabel(m);
    return label.charAt(0).toUpperCase();
  }

  memberLabel(m: PlanMemberDto): string {
    const isMe = this.isCurrentMember(m);
    const sessionName = this.auth.getSessionDisplayName();
    const sessionEmail = this.auth.getSessionEmail();
    const apiName = (m.displayName || '').trim();
    const apiEmail = (m.email || '').trim();

    const name =
      (apiName && apiName !== 'Plan sahibi' && apiName !== 'Üye' && !/^[0-9a-f-]{36}$/i.test(apiName)
        ? apiName
        : null) ||
      (isMe ? sessionName : null) ||
      apiEmail ||
      (isMe ? sessionEmail : null);

    if (name) {
      return isMe ? `${name} (sen)` : name;
    }
    if (this.isOwnerRole(m.role)) {
      return isMe ? 'Sen (plan sahibi)' : 'Plan sahibi';
    }
    return 'Üye';
  }

  memberPartnerLabel(m: PlanMemberDto): string {
    if (m.partnerName) {
      return `bağlı pay: ${m.partnerName}`;
    }
    if (this.isOwnerRole(m.role) && this.isCurrentMember(m)) {
      return 'henüz bir ortak satırına bağlanmadın — yukarıda “Bu benim”';
    }
    if (this.isOwnerRole(m.role)) {
      return 'henüz ortak satırına bağlanmadı';
    }
    return 'ortağa bağlı değil';
  }

  memberEmail(m: PlanMemberDto): string | null {
    const api = (m.email || '').trim();
    if (api) {
      return api;
    }
    return this.isCurrentMember(m) ? this.auth.getSessionEmail() : null;
  }

  private isCurrentMember(m: PlanMemberDto): boolean {
    const uid = this.auth.getSessionUserId();
    if (uid && m.userId && uid === m.userId) {
      return true;
    }
    const email = this.auth.getSessionEmail();
    return !!(email && m.email && email.toLowerCase() === m.email.toLowerCase());
  }

  private isOwnerRole(role: string): boolean {
    const r = String(role).toLowerCase();
    return r === 'owner' || r === '0';
  }

  roleLabel(role: string): string {
    if (this.isOwnerRole(role)) {
      return 'Sahip';
    }
    const r = String(role).toLowerCase();
    if (r === 'member' || r === '1') {
      return 'Üye';
    }
    return role;
  }

  revokeInvite(invite: PlanInviteDto): void {
    if (!this.ensureOwnerAction()) {
      return;
    }
    this.busy.set(true);
        this.membershipApi.revoke(this.planId, invite.id).subscribe({
      next: () => {
        this.busy.set(false);
        this.toast.success('Davet iptal edildi.');
        this.loadMembership();
      },
      error: (err) => this.fail(err, 'Davet iptal edilemedi.'),
    });
  }

  linkSelf(p: PartnerDto): void {
    this.linkSelfById(p.id);
  }

  linkSelfById(partnerId: string): void {
    this.busy.set(true);
    this.membershipApi.linkSelf(this.planId, partnerId).subscribe({
      next: () => {
        this.membershipApi.members(this.planId).subscribe({
          next: (members) => this.members.set(members),
        });
        this.refreshPartners('Ortağı kendinize bağladınız.');
      },
      error: (err) => this.fail(err, 'Bağlama başarısız.'),
    });
  }

  savePlan(): void {
    if (!this.ensureOwnerAction()) {
      return;
    }
    this.busy.set(true);
        this.plansApi
      .update(this.planId, {
        title: this.title.trim(),
        description: this.description.trim(),
        deliveryInstallmentId: this.deliveryInstallmentId || null,
        requireReceipt: this.requireReceipt,
        ibanMode: this.ibanMode,
        settlementIban: this.ibanMode === 'Plan' ? this.settlementIban.trim() || null : null,
        remindersEnabled: this.remindersEnabled,
        reminderDaysBefore: [...this.reminderDaysBefore],
        reminderDaysAfter: [...this.reminderDaysAfter],
      })
      .subscribe({
        next: (plan) => {
          this.busy.set(false);
          this.plan.set(plan);
          this.requireReceipt = !!plan.requireReceipt;
          this.ibanMode = plan.ibanMode ?? 'None';
          this.settlementIban = plan.settlementIban ?? '';
          this.remindersEnabled = !!plan.remindersEnabled;
          this.reminderDaysBefore = [...(plan.reminderDaysBefore ?? [])];
          this.reminderDaysAfter = [...(plan.reminderDaysAfter ?? [])];
          this.planContext.setPlan(plan.id, plan.title, plan.description, plan.planType);
          this.toast.success('Plan kaydedildi.');
        },
        error: (err) => this.fail(err, 'Plan kaydedilemedi.'),
      });
  }

  toggleReminderDay(list: 'before' | 'after', day: number): void {
    const target = list === 'before' ? this.reminderDaysBefore : this.reminderDaysAfter;
    const idx = target.indexOf(day);
    if (idx >= 0) {
      target.splice(idx, 1);
    } else {
      target.push(day);
      target.sort((a, b) => a - b);
    }
  }

  hasReminderDay(list: 'before' | 'after', day: number): boolean {
    return (list === 'before' ? this.reminderDaysBefore : this.reminderDaysAfter).includes(day);
  }

  private partnerBody(row: PartnerEditRow) {
    return {
      name: row.name.trim(),
      color: row.color,
      defaultPct: Number(row.defaultPct) || 0,
      sortOrder: row.sortOrder,
      iban: row.iban.trim() || null,
      inviteEmail: row.inviteEmail.trim() || null,
    };
  }

  addPartnerRow(): void {
    if (!this.ensureOwnerAction()) {
      return;
    }
    const n = this.partnerRows().length;
    const color = PARTNER_COLORS[n % PARTNER_COLORS.length];
    const defaultPct = n === 0 ? 50 : Math.floor(100 / (n + 1));
    this.busy.set(true);
    this.partnersApi
      .create(this.planId, {
        name: `Ortak ${n + 1}`,
        color,
        defaultPct,
        sortOrder: n,
        iban: null,
        inviteEmail: null,
      })
      .subscribe({
        next: (created) => {
          this.busy.set(false);
          this.applyPartners([...this.partners(), created]);
          this.toast.success('Ortak eklendi.');
        },
        error: (err) => this.fail(err, 'Ortak eklenemedi.'),
      });
  }

  onPartnerColor(row: PartnerEditRow, color: string): void {
    row.color = color;
    this.persistPartner(row);
  }

  persistPartner(row: PartnerEditRow): void {
    if (!this.ensureOwnerAction()) {
      return;
    }
    const name = row.name.trim();
    if (!name) {
      return;
    }
    this.partnersApi.update(this.planId, row.id, this.partnerBody(row)).subscribe({
      error: (err) => this.fail(err, 'Ortak güncellenemedi.'),
    });
  }

  saveAllPartners(): void {
    if (!this.ensureOwnerAction()) {
      return;
    }
    const rows = this.partnerRows();
    if (!rows.length) {
      return;
    }

    const emails = rows
      .map((r) => r.inviteEmail.trim().toLowerCase())
      .filter((e) => !!e);
    const dup = emails.find((e, i) => emails.indexOf(e) !== i);
    if (dup) {
      this.toast.error(`Aynı e-posta birden fazla ortakta: ${dup}`);
      return;
    }

    this.busy.set(true);
    let remaining = rows.length;
    let failed = false;

    const finishSaves = () => {
      if (failed) {
        return;
      }
      this.syncPartnerInvites(rows);
    };

    for (const row of rows) {
      const name = row.name.trim();
      if (!name) {
        remaining -= 1;
        if (remaining === 0) {
          finishSaves();
        }
        continue;
      }
      this.partnersApi.update(this.planId, row.id, this.partnerBody(row)).subscribe({
        next: () => {
          remaining -= 1;
          if (remaining === 0) {
            finishSaves();
          }
        },
        error: (err) => {
          if (!failed) {
            failed = true;
            this.fail(err, 'Ortaklar kaydedilemedi.');
          }
        },
      });
    }
  }

  /** After partner save: create invites for unlinked partners with email (skip unchanged pending). */
  private syncPartnerInvites(rows: PartnerEditRow[]): void {
    const targets = rows.filter((r) => !r.linkedUserId && r.inviteEmail.trim());
    if (!targets.length) {
      this.refreshPartners('Ortaklar kaydedildi.');
      this.loadMembership();
      return;
    }

    const pending = this.invites();
    const toSend = targets.filter((row) => {
      const email = row.inviteEmail.trim().toLowerCase();
      const existing = pending.find((i) => i.partnerId === row.id);
      return !(existing && existing.email.toLowerCase() === email);
    });

    if (!toSend.length) {
      this.refreshPartners('Ortaklar kaydedildi.');
      this.loadMembership();
      return;
    }

    let remaining = toSend.length;
    let invited = 0;
    let failed = false;

    const done = () => {
      if (failed) {
        return;
      }
      this.refreshPartners(`Ortaklar kaydedildi · ${invited} davet gönderildi.`);
      this.loadMembership();
    };

    for (const row of toSend) {
      const email = row.inviteEmail.trim().toLowerCase();
      this.membershipApi.invite(this.planId, email, row.id).subscribe({
        next: () => {
          invited += 1;
          remaining -= 1;
          if (remaining === 0) {
            done();
          }
        },
        error: (err) => {
          if (!failed) {
            failed = true;
            this.fail(err, 'Ortaklar kaydedildi ama davet gönderilemedi.');
          }
        },
      });
    }
  }

  deletePartnerById(id: string): void {
    const p = this.partners().find((x) => x.id === id);
    if (!p) {
      return;
    }
    this.deletePartner(p);
  }

  async deletePartner(p: PartnerDto): Promise<void> {
    if (!this.ensureOwnerAction()) {
      return;
    }
    if (
      !(await this.confirm.ask({
        title: 'Ortağı sil',
        message: `“${p.name}” ortağını silmek istiyor musunuz? Bu işlem geri alınamaz.`,
        confirmLabel: 'Sil',
        danger: true,
      }))
    ) {
      return;
    }
    this.busy.set(true);
    this.partnersApi.delete(this.planId, p.id).subscribe({
      next: () => {
        this.busy.set(false);
        this.applyPartners(this.partners().filter((x) => x.id !== p.id));
        this.toast.success('Ortak silindi.');
      },
      error: (err) => this.fail(err, 'Ortak silinemedi.'),
    });
  }

  openAddInstallment(): void {
    if (!this.ensureOwnerAction()) {
      return;
    }
    this.resetInstallmentForm();
    this.initCustomShares(null);
    this.showInstModal.set(true);
  }

  startEditInstallment(i: InstallmentDto): void {
    if (!this.ensureOwnerAction()) {
      return;
    }
    this.editingInstallmentId = i.id;
    this.instName = i.name;
    this.instDueDate = i.dueDate;
    this.instAmount = i.totalAmount;
    this.instShareType = shareTypeToNumber(i.shareType);
    this.initCustomShares(i);
    this.showInstModal.set(true);
  }

  closeInstallmentModal(): void {
    this.showInstModal.set(false);
    this.resetInstallmentForm();
  }

  openDatePicker(event: Event): void {
    const input = event.target as HTMLInputElement;
    try {
      input.showPicker?.();
    } catch {
      // Already open / unsupported browser — native control still works.
    }
  }

  resetInstallmentForm(): void {
    this.editingInstallmentId = null;
    this.instName = '';
    this.instDueDate = '';
    this.instAmount = 0;
    this.instShareType = 0;
    this.customShareAmounts = {};
  }

  private initCustomShares(inst: InstallmentDto | null): void {
    const amounts: Record<string, number> = {};
    const partners = this.partners();
    const equal =
      partners.length > 0 ? Math.round((Number(this.instAmount) / partners.length) * 100) / 100 : 0;
    for (const p of partners) {
      const existing = inst?.customShares?.find((c) => c.partnerId === p.id);
      amounts[p.id] = existing ? existing.amount : equal;
    }
    this.customShareAmounts = amounts;
  }

  onShareTypeOrAmountChange(): void {
    if (this.instShareType !== 2) {
      return;
    }
    const partners = this.partners();
    if (!partners.length) {
      return;
    }
    const equal = Math.round((Number(this.instAmount) / partners.length) * 100) / 100;
    for (const p of partners) {
      if (this.customShareAmounts[p.id] == null) {
        this.customShareAmounts[p.id] = equal;
      }
    }
  }

  saveInstallment(): void {
    if (!this.ensureOwnerAction()) {
      return;
    }
    const customShares: CustomShareDto[] | null =
      Number(this.instShareType) === 2
        ? this.partners().map((p) => ({
            partnerId: p.id,
            amount: Number(this.customShareAmounts[p.id] ?? 0),
          }))
        : null;

    const totalAmount = Number(this.instAmount) || 0;
    if (customShares) {
      const shareSum = customShares.reduce((s, x) => s + (Number(x.amount) || 0), 0);
      if (Math.abs(shareSum - totalAmount) > 0.01) {
        this.toast.error(
          `Özel payların toplamı taksit tutarına eşit olmalı. Paylar: ${formatMoneyTr(shareSum)} ₺, taksit: ${formatMoneyTr(totalAmount)} ₺.`
        );
        return;
      }
    }

    const body: InstallmentRequest = {
      name: this.instName.trim(),
      dueDate: this.instDueDate,
      totalAmount,
      shareType: Number(this.instShareType),
      sortOrder: this.editingInstallmentId
        ? this.installments().find((x) => x.id === this.editingInstallmentId)?.sortOrder ?? 0
        : this.installments().length,
      customShares,
    };
    if (!body.name || !body.dueDate) {
      return;
    }
    this.busy.set(true);
        const req = this.editingInstallmentId
      ? this.installmentsApi.update(this.planId, this.editingInstallmentId, body)
      : this.installmentsApi.create(this.planId, body);
    req.subscribe({
      next: () => {
        this.busy.set(false);
        this.closeInstallmentModal();
        this.toast.success('Taksit kaydedildi.');
        this.reload();
      },
      error: (err) => this.fail(err, 'Taksit kaydedilemedi.'),
    });
  }

  customShareSum(): number {
    return this.partners().reduce((s, p) => s + (Number(this.customShareAmounts[p.id] ?? 0) || 0), 0);
  }

  customShareRemainder(): number {
    return Math.round(((Number(this.instAmount) || 0) - this.customShareSum()) * 100) / 100;
  }

  customSharesMatch(): boolean {
    return Math.abs(this.customShareRemainder()) <= 0.01;
  }

  async deleteInstallment(i: InstallmentDto): Promise<void> {
    if (!this.ensureOwnerAction()) {
      return;
    }
    if (
      !(await this.confirm.ask({
        title: 'Taksiti sil',
        message: `“${i.name}” taksitini silmek istiyor musunuz? Bu işlem geri alınamaz.`,
        confirmLabel: 'Sil',
        danger: true,
      }))
    ) {
      return;
    }
    this.busy.set(true);
    this.installmentsApi.delete(this.planId, i.id).subscribe({
      next: () => {
        this.busy.set(false);
        this.toast.success('Taksit silindi.');
        this.reload();
      },
      error: (err) => this.fail(err, 'Taksit silinemedi.'),
    });
  }

  private fail(err: unknown, fallback: string): void {
    this.busy.set(false);
    this.loading.set(false);
    this.toast.error(apiErrorMessage(err, fallback));
  }
}
