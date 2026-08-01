import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import {
  CustomShareDto,
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
  TemplateListItemDto,
} from '../../core/models/api.models';
import { InstallmentsApi } from '../../core/services/installments.api';
import { MembershipApi } from '../../core/services/membership.api';
import { PartnersApi } from '../../core/services/partners.api';
import { PlanContextService } from '../../core/services/plan-context.service';
import { PlansApi } from '../../core/services/plans.api';
import { TemplatesApi } from '../../core/services/templates.api';
import { AuthService } from '../../core/services/auth.service';
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
  imports: [FormsModule, CurrencyTryPipe, MoneyInputDirective, IconTrashComponent],
  templateUrl: './setup.component.html',
  styleUrl: './setup.component.scss',
})
export class SetupComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly plansApi = inject(PlansApi);
  private readonly partnersApi = inject(PartnersApi);
  private readonly installmentsApi = inject(InstallmentsApi);
  private readonly membershipApi = inject(MembershipApi);
  private readonly templatesApi = inject(TemplatesApi);
  private readonly planContext = inject(PlanContextService);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);
  private readonly auth = inject(AuthService);

  readonly plan = signal<PlanDto | null>(null);
  readonly partners = signal<PartnerDto[]>([]);
  readonly partnerRows = signal<PartnerEditRow[]>([]);
  readonly installments = signal<InstallmentDto[]>([]);
  readonly members = signal<PlanMemberDto[]>([]);
  readonly invites = signal<PlanInviteDto[]>([]);
  readonly templates = signal<TemplateListItemDto[]>([]);
  readonly previewDraft = signal<PreviewDraft | null>(null);
  readonly showInstModal = signal(false);
  readonly loading = signal(true);
  readonly busy = signal(false);

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

  inviteEmail = '';
  invitePartnerId = '';

  private planId = '';

  ngOnInit(): void {
    this.planId = this.route.snapshot.paramMap.get('id') ?? '';
    this.templatesApi.list().subscribe({
      next: (list) => this.templates.set(list),
      error: () => this.templates.set([]),
    });
    this.reload();
  }

  reload(): void {
    this.loading.set(true);
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
        this.planContext.setPlan(plan.id, plan.title, plan.description);
        this.partnersApi.list(this.planId).subscribe({
          next: (partners) => {
            this.applyPartners(partners);
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
      }))
    );
    if (!this.invitePartnerId && partners.length) {
      this.invitePartnerId = partners[0].id;
    }
    if (this.invitePartnerId && !partners.some((p) => p.id === this.invitePartnerId)) {
      this.invitePartnerId = partners[0]?.id ?? '';
    }
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
      this.openAddInstallment();
      void this.router.navigate([], { relativeTo: this.route, queryParams: {}, replaceUrl: true });
      return;
    }
    if (edit) {
      const inst = this.installments().find((i) => i.id === edit);
      if (inst) {
        this.startEditInstallment(inst);
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
      case 'empty':
        return '📄';
      default:
        return '📋';
    }
  }

  openPreview(key: string): void {
    if (key === 'empty') {
      this.seedTemplate('empty');
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

  addPreviewRow(): void {
    const d = this.previewDraft();
    if (!d) {
      return;
    }
    const last = d.rows[d.rows.length - 1];
    const nextDate = last?.dueDate ? this.addMonthsIso(last.dueDate, 1) : new Date().toISOString().slice(0, 10);
    d.rows.push({
      index: d.rows.length + 1,
      name: `${d.rows.length + 1}. Taksit`,
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
    if (!d.title.trim() || !d.rows.length || !d.partners.length) {
      this.toast.error('Başlık, ortak ve en az bir taksit gerekli.');
      return;
    }
    const pctSum = d.partners.reduce((s, p) => s + (Number(p.defaultPct) || 0), 0);
    if (Math.abs(pctSum - 100) > 0.05) {
      this.toast.error(`Ortak pay yüzdeleri toplamı 100 olmalı (şu an ${pctSum}).`);
      return;
    }
    if (
      !(await this.confirm.ask({
        title: d.key === 'document' ? 'Dosyadan aktar' : 'Şablonu aktar',
        message:
          d.key === 'document'
            ? `“${d.title.trim()}” dosya önizlemesi mevcut ortak/taksit verisinin üzerine yazılacak. Onaylıyor musunuz?`
            : `Düzenlenen “${d.title.trim()}” planı mevcut ortak/taksit verisinin üzerine yazılacak. Onaylıyor musunuz?`,
        confirmLabel: 'Aktar',
        success: true,
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
        this.plansApi.import(this.planId, payload).subscribe({
      next: () => {
        this.busy.set(false);
        this.closePreview();
        this.toast.success(
          d.key === 'document' ? 'Dosya plana aktarıldı.' : 'Düzenlenen şablon plana aktarıldı.'
        );
        this.reload();
      },
      error: (err) => this.fail(err, 'İçe aktarma başarısız.'),
    });
  }

  async seedTemplate(key: string): Promise<void> {
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

  sendInvite(): void {
    const email = this.inviteEmail.trim();
    if (!email || !this.invitePartnerId) {
      this.toast.error('Davet için e-posta ve ortak seçin.');
      return;
    }
    this.busy.set(true);
        this.membershipApi.invite(this.planId, email, this.invitePartnerId).subscribe({
      next: (invite) => {
        this.busy.set(false);
        this.inviteEmail = '';
        const link = this.inviteLink(invite);
        if (invite.emailSent) {
          this.toast.success(`Davet e-postası ${invite.email} adresine gönderildi.`);
        } else {
          this.toast.success(
            `Davet oluşturuldu (e-posta şu an kapalı veya gönderilemedi). Linki paylaşın: ${link}`
          );
        }
        this.loadMembership();
      },
      error: (err) => this.fail(err, 'Davet gönderilemedi.'),
    });
  }

  inviteLink(invite: PlanInviteDto): string {
    const origin = typeof window !== 'undefined' ? window.location.origin : '';
    return `${origin}/invite/${invite.token}`;
  }

  copyInviteLink(invite: PlanInviteDto): void {
    const link = this.inviteLink(invite);
    void navigator.clipboard.writeText(link).then(
      () => this.toast.success('Davet linki kopyalandı.'),
      () => this.toast.success(`Link: ${link}`)
    );
  }

  resendInvite(invite: PlanInviteDto): void {
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
          this.planContext.setPlan(plan.id, plan.title, plan.description);
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

  addPartnerRow(): void {
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
    const name = row.name.trim();
    if (!name) {
      return;
    }
    this.partnersApi
      .update(this.planId, row.id, {
        name,
        color: row.color,
        defaultPct: Number(row.defaultPct) || 0,
        sortOrder: row.sortOrder,
        iban: row.iban.trim() || null,
      })
      .subscribe({
        error: (err) => this.fail(err, 'Ortak güncellenemedi.'),
      });
  }

  saveAllPartners(): void {
    const rows = this.partnerRows();
    if (!rows.length) {
      return;
    }
    this.busy.set(true);
        let remaining = rows.length;
    let failed = false;
    for (const row of rows) {
      const name = row.name.trim();
      if (!name) {
        remaining -= 1;
        if (remaining === 0 && !failed) {
          this.refreshPartners('Ortaklar kaydedildi.');
        }
        continue;
      }
      this.partnersApi
        .update(this.planId, row.id, {
          name,
          color: row.color,
          defaultPct: Number(row.defaultPct) || 0,
          sortOrder: row.sortOrder,
          iban: row.iban.trim() || null,
        })
        .subscribe({
          next: () => {
            remaining -= 1;
            if (remaining === 0 && !failed) {
              this.refreshPartners('Ortaklar kaydedildi.');
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

  deletePartnerById(id: string): void {
    const p = this.partners().find((x) => x.id === id);
    if (!p) {
      return;
    }
    this.deletePartner(p);
  }

  async deletePartner(p: PartnerDto): Promise<void> {
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
    this.resetInstallmentForm();
    this.initCustomShares(null);
    this.showInstModal.set(true);
  }

  startEditInstallment(i: InstallmentDto): void {
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

