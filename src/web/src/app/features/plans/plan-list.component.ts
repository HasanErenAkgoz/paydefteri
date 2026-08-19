import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { switchMap } from 'rxjs';
import { PlanDto, PlanExportDto, PlanInviteDto, PlanTemplatePreviewDto, PlanType, TemplateListItemDto } from '../../core/models/api.models';
import { MembershipApi } from '../../core/services/membership.api';
import { PlanContextService } from '../../core/services/plan-context.service';
import { PlansApi } from '../../core/services/plans.api';
import { TemplatesApi } from '../../core/services/templates.api';
import { isExpensePlan, planHomeCommands } from '../../core/utils/plan-routes';
import { ToastService } from '../../shared/toast/toast.service';
import { ConfirmService } from '../../shared/confirm/confirm.service';
import { formatDateTr } from '../../shared/utils/format';
import { IconTrashComponent } from '../../shared/icons/icon-trash.component';
import { CurrencyTryPipe } from '../../shared/pipes/currency-try.pipe';

@Component({
  selector: 'app-plan-list',
  standalone: true,
  imports: [FormsModule, RouterLink, IconTrashComponent, CurrencyTryPipe],
  templateUrl: './plan-list.component.html',
  styleUrl: './plan-list.component.scss',
})
export class PlanListComponent implements OnInit {
  private readonly plansApi = inject(PlansApi);
  private readonly membershipApi = inject(MembershipApi);
  private readonly templatesApi = inject(TemplatesApi);
  readonly planContext = inject(PlanContextService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  private readonly confirm = inject(ConfirmService);

  readonly plans = signal<PlanDto[]>([]);
  readonly archivedPlans = signal<PlanDto[]>([]);
  readonly pendingInvites = signal<PlanInviteDto[]>([]);
  readonly templates = signal<TemplateListItemDto[]>([]);
  readonly loading = signal(true);
  readonly creating = signal(false);
  readonly parsingDocument = signal(false);
  readonly accepting = signal(false);
  readonly showCustomForm = signal(false);
  readonly manageMode = signal(false);
  readonly showArchived = signal(false);
  readonly activeTab = signal<'my-plans' | 'templates' | 'import' | 'blank' | 'invites' | 'archived'>('my-plans');
  readonly planSearch = signal('');

  readonly previewModal = signal<PlanTemplatePreviewDto | null>(null);
  readonly previewKey = signal<string>('');

  newTitle = '';
  newDescription = '';

  formatDateTr = formatDateTr;
  isExpensePlan = isExpensePlan;
  planHomeCommands = planHomeCommands;

  setTab(tab: 'my-plans' | 'templates' | 'import' | 'blank' | 'invites' | 'archived'): void {
    this.activeTab.set(tab);
  }

  readonly sortedPlans = computed(() =>
    [...this.plans()].sort((a, b) => String(b.createdAtUtc).localeCompare(String(a.createdAtUtc)))
  );

  readonly filteredPlans = computed(() => {
    const q = this.planSearch().trim().toLowerCase();
    const list = this.sortedPlans();
    if (!q) {
      return list;
    }
    return list.filter((p) => {
      const title = (p.title ?? '').toLowerCase();
      const desc = (p.description ?? '').toLowerCase();
      const type = isExpensePlan(p) ? 'gider' : 'taksit';
      return title.includes(q) || desc.includes(q) || type.includes(q);
    });
  });

  ngOnInit(): void {
    // Keep last planId so navbar tabs stay available while managing plans.
    const manage = this.route.snapshot.queryParamMap.get('manage') === '1';
    this.manageMode.set(manage);
    this.templatesApi.list().subscribe({
      next: (list) => this.templates.set(list.filter((t) => t.key !== 'empty')),
      error: () => this.templates.set([]),
    });
    this.reload(manage);
  }

  backToDashboard(): void {
    const id = this.planContext.planId() ?? this.sortedPlans()[0]?.id;
    if (id) {
      const plan =
        this.sortedPlans().find((p) => p.id === id) ??
        this.sortedPlans()[0];
      void this.router.navigate(planHomeCommands(id, plan?.planType ?? this.planContext.planType()));
      return;
    }
    void this.router.navigate(['/plans'], { queryParams: { manage: '1' } });
  }

  openPlan(plan: PlanDto): string[] {
    return planHomeCommands(plan.id, plan.planType);
  }

  planTypeLabel(plan: PlanDto): string {
    return isExpensePlan(plan) ? 'Gider' : 'Taksit';
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
      default:
        return '📋';
    }
  }

  reload(manage = this.manageMode()): void {
    this.loading.set(true);
    this.plansApi.list().subscribe({
      next: (plans) => {
        this.plans.set(plans);
        if (!manage && plans.length > 0) {
          const plan = [...plans].sort((a, b) =>
            String(b.createdAtUtc).localeCompare(String(a.createdAtUtc))
          )[0];
          void this.router.navigate(planHomeCommands(plan.id, plan.planType));
          return;
        }
        if (!manage && plans.length === 0) {
          void this.router.navigate(['/plans'], { queryParams: { manage: '1' } });
          return;
        }
        this.membershipApi.myInvites().subscribe({
          next: (invites) => {
            this.pendingInvites.set(invites);
            this.ensurePlanContext(plans);
            this.loading.set(false);
            this.loadArchived();
          },
          error: () => {
            this.pendingInvites.set([]);
            this.ensurePlanContext(plans);
            this.loading.set(false);
            this.loadArchived();
          },
        });
      },
      error: (err) => {
        this.loading.set(false);
        this.toast.error(err?.error?.detail ?? 'Planlar yüklenemedi.');
      },
    });
  }

  private loadArchived(): void {
    this.plansApi.list(true).subscribe({
      next: (list) => this.archivedPlans.set(list),
      error: () => this.archivedPlans.set([]),
    });
  }

  private ensurePlanContext(plans: PlanDto[]): void {
    this.planContext.syncWithPlans(plans);
  }

  acceptInvite(invite: PlanInviteDto): void {
    this.accepting.set(true);
    this.membershipApi.accept(invite.token).subscribe({
      next: (plan) => {
        this.accepting.set(false);
        void this.router.navigate(planHomeCommands(plan.id, plan.planType));
      },
      error: (err) => {
        this.accepting.set(false);
        this.toast.error(err?.error?.detail ?? 'Davet kabul edilemedi.');
      },
    });
  }

  createFromTemplate(key: string): void {
    const t = this.templates().find((x) => x.key === key);
    this.creating.set(true);
    this.plansApi
      .create({
        title: t?.title ?? 'Yeni Plan',
        description: t?.description ?? '',
        planType: 'Installment',
      })
      .pipe(switchMap((plan) => this.plansApi.seed(plan.id, key)))
      .subscribe({
        next: (plan) => {
          this.creating.set(false);
          this.planContext.setPlan(plan.id, plan.title, plan.description, plan.planType);
          void this.router.navigate(planHomeCommands(plan.id, plan.planType));
        },
        error: (err) => {
          this.creating.set(false);
          this.toast.error(err?.error?.detail ?? 'Şablon plan oluşturulamadı.');
        },
      });
  }

  createEmpty(): void {
    this.creating.set(true);
    this.plansApi
      .create({ title: 'Yeni Özel Plan', description: 'Özel takip planı', planType: 'Installment' })
      .pipe(switchMap((plan) => this.plansApi.seed(plan.id, 'empty')))
      .subscribe({
        next: (plan) => {
          this.creating.set(false);
          this.planContext.setPlan(plan.id, plan.title, plan.description, plan.planType);
          void this.router.navigate(['/plans', plan.id, 'setup']);
        },
        error: (err) => {
          this.creating.set(false);
          this.toast.error(err?.error?.detail ?? 'Boş plan oluşturulamadı.');
        },
      });
  }

  createExpensePlan(): void {
    this.creating.set(true);
    this.plansApi
      .create({
        title: 'Ortak Gider Planı',
        description: 'Ev / ofis ortak harcamaları',
        planType: 'Expense' as PlanType,
      })
      .subscribe({
        next: (plan) => {
          this.creating.set(false);
          const planType = plan.planType ?? ('Expense' as PlanType);
          this.planContext.setPlan(plan.id, plan.title, plan.description, planType);
          void this.router.navigate(planHomeCommands(plan.id, planType));
        },
        error: (err) => {
          this.creating.set(false);
          this.toast.error(err?.error?.detail ?? 'Gider planı oluşturulamadı.');
        },
      });
  }

  previewCoupleExpenseSample(): void {
    this.previewExpenseSample('couple');
  }

  previewExpenseSample(key: 'couple' | 'trip' | 'teamlunch'): void {
    this.previewTemplate(key);
  }

  previewTemplate(key: string): void {
    this.creating.set(true);
    this.templatesApi.preview(key).subscribe({
      next: (dto) => {
        this.creating.set(false);
        this.previewKey.set(key);
        this.previewModal.set(dto);
      },
      error: (err) => {
        this.creating.set(false);
        this.toast.error(err?.error?.detail ?? 'Önizleme yüklenemedi.');
      },
    });
  }

  closePreviewModal(): void {
    this.previewModal.set(null);
    this.previewKey.set('');
  }

  confirmAndCreateFromPreview(): void {
    const pt = this.previewModal();
    const key = this.previewKey();
    if (!pt || !key) {
      return;
    }
    this.closePreviewModal();

    if (key === 'couple' || key === 'trip' || key === 'teamlunch') {
      this.createExpenseSample(key as 'couple' | 'trip' | 'teamlunch', {
        title: pt.title,
        description: pt.description,
        partners: (pt.partners ?? []).map((p) => ({
          name: p.name,
          color: p.color,
          defaultPct: Number(p.defaultPct) || 0,
        })),
      });
    } else {
      this.createFromTemplate(key);
    }
  }

  createCoupleExpenseSample(seedBody?: {
    title?: string;
    description?: string;
    partners?: { name: string; color: string; defaultPct: number }[];
  }): void {
    this.createExpenseSample('couple', seedBody);
  }

  createExpenseSample(
    key: 'couple' | 'trip' | 'teamlunch',
    seedBody?: {
      title?: string;
      description?: string;
      partners?: { name: string; color: string; defaultPct: number }[];
    }
  ): void {
    this.creating.set(true);
    const defaults: Record<string, { title: string; description: string }> = {
      couple: { title: 'Ev Ortak Giderleri', description: 'Örnek karı-koca ortak gider planı' },
      trip: { title: 'Tatil Harcama Defteri', description: 'Örnek tatil / arkadaş grubu harcama planı' },
      teamlunch: { title: 'Ekip Öğle Yemeği', description: 'Örnek ekip yemeği mahsup planı' },
    };
    const fallback = defaults[key];
    const title = seedBody?.title?.trim() || fallback.title;
    const description = seedBody?.description?.trim() || fallback.description;
    this.plansApi
      .create({
        title,
        description,
        planType: 'Expense' as PlanType,
      })
      .pipe(switchMap((plan) => this.plansApi.seed(plan.id, key, seedBody)))
      .subscribe({
        next: (plan) => {
          this.creating.set(false);
          const planType = plan.planType ?? ('Expense' as PlanType);
          this.planContext.setPlan(plan.id, plan.title, plan.description, planType);
          this.toast.success(`Örnek plan hazır: ${plan.title}`);
          void this.router.navigate(planHomeCommands(plan.id, planType));
        },
        error: (err) => {
          this.creating.set(false);
          this.toast.error(err?.error?.detail ?? 'Örnek gider planı oluşturulamadı.');
        },
      });
  }

  create(): void {
    const title = this.newTitle.trim();
    if (!title) {
      return;
    }
    this.creating.set(true);
    this.plansApi
      .create({ title, description: this.newDescription.trim(), planType: 'Installment' })
      .subscribe({
        next: (plan) => {
          this.creating.set(false);
          this.newTitle = '';
          this.newDescription = '';
          this.showCustomForm.set(false);
          this.planContext.setPlan(plan.id, plan.title, plan.description, plan.planType);
          void this.router.navigate(planHomeCommands(plan.id, plan.planType));
        },
        error: (err) => {
          this.creating.set(false);
          this.toast.error(err?.error?.detail ?? 'Plan oluşturulamadı.');
        },
      });
  }

  async remove(plan: PlanDto, event: Event): Promise<void> {
    event.preventDefault();
    event.stopPropagation();
    if (
      !(await this.confirm.ask({
        title: 'Planı sil',
        message: `“${plan.title}” planını kalıcı olarak silmek istediğinize emin misiniz? Bu işlem geri alınamaz.`,
        confirmLabel: 'Sil',
        danger: true,
      }))
    ) {
      return;
    }
    this.plansApi.delete(plan.id).subscribe({
      next: () => {
        const remaining = this.plans().filter((p) => p.id !== plan.id);
        this.plans.set(remaining);
        this.archivedPlans.set(this.archivedPlans().filter((p) => p.id !== plan.id));

        if (remaining.length === 0) {
          this.plansApi
            .create({ title: 'Yeni Özel Plan', description: 'Özel takip planı' })
            .pipe(switchMap((created) => this.plansApi.seed(created.id, 'empty')))
            .subscribe({
              next: (created) => {
                this.planContext.setPlan(
                  created.id,
                  created.title,
                  created.description,
                  created.planType
                );
                this.toast.success('Yeni plan açıldı — kuruluma yönlendiriliyorsunuz.');
                void this.router.navigate(['/plans', created.id, 'setup']);
              },
              error: (err) => {
                this.planContext.clear();
                this.toast.error(err?.error?.detail ?? 'Yeni plan oluşturulamadı.');
                this.reload(true);
              },
            });
          return;
        }

        if (this.planContext.planId() === plan.id) {
          const next = [...remaining].sort((a, b) =>
            String(b.createdAtUtc).localeCompare(String(a.createdAtUtc))
          )[0];
          this.planContext.setPlan(next.id, next.title, next.description, next.planType);
        }
        this.toast.success('Plan silindi.');
        this.reload(true);
      },
      error: (err) => this.toast.error(err?.error?.detail ?? 'Plan silinemedi.'),
    });
  }

  async archivePlan(plan: PlanDto, event: Event): Promise<void> {
    event.preventDefault();
    event.stopPropagation();
    if (
      !(await this.confirm.ask({
        title: 'Planı arşivle',
        message: `“${plan.title}” planını arşivlemek istediğinize emin misiniz? Sonra geri yükleyebilirsiniz.`,
        confirmLabel: 'Arşivle',
        success: true,
      }))
    ) {
      return;
    }
    this.plansApi.archive(plan.id).subscribe({
      next: () => {
        this.toast.success('Plan arşivlendi.');
        if (this.planContext.planId() === plan.id) {
          this.planContext.clear();
        }
        this.reload(true);
      },
      error: (err) => this.toast.error(err?.error?.detail ?? 'Plan arşivlenemedi.'),
    });
  }
  copyPlan(plan: PlanDto, event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.creating.set(true);
    this.plansApi.copy(plan.id).subscribe({
      next: (copied) => {
        this.creating.set(false);
        this.toast.success('Plan kopyalandı.');
        this.planContext.setPlan(copied.id, copied.title, copied.description, copied.planType);
        void this.router.navigate(['/plans', copied.id, 'setup']);
      },
      error: (err) => {
        this.creating.set(false);
        this.toast.error(err?.error?.detail ?? 'Kopyalama başarısız.');
      },
    });
  }

  restorePlan(plan: PlanDto, event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.plansApi.restore(plan.id).subscribe({
      next: () => {
        this.toast.success('Plan geri yüklendi.');
        this.reload(true);
      },
      error: (err) => this.toast.error(err?.error?.detail ?? 'Geri yükleme başarısız.'),
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
      this.toast.error('Desteklenen dosyalar: PDF, Excel (.xlsx) veya CSV.');
      return;
    }

    const cleanTitle = file.name.replace(/\.[^/.]+$/, '');
    this.parsingDocument.set(true);

    this.plansApi
      .create({
        title: cleanTitle,
        description: `${file.name} dosyasından aktarıldı`,
        planType: 'Installment' as PlanType,
      })
      .pipe(
        switchMap((plan) =>
          this.plansApi.parseDocument(plan.id, file).pipe(
            switchMap((dto) => {
              const partnersData = dto.partners?.length
                ? dto.partners
                : [
                    { name: 'Ortak 1', color: '#38bdf8', defaultPct: 50 },
                    { name: 'Ortak 2', color: '#fb923c', defaultPct: 50 },
                  ];

              const partnerIds = partnersData.map(() => crypto.randomUUID());
              const installmentIds = (dto.installments ?? []).map(() => crypto.randomUUID());
              const deliveryId =
                dto.deliveryIndex >= 0 && dto.deliveryIndex < installmentIds.length
                  ? installmentIds[dto.deliveryIndex]
                  : null;

              const exportDto: PlanExportDto = {
                title: dto.title || plan.title,
                description: dto.description || plan.description,
                deliveryInstallmentId: deliveryId,
                partners: partnersData.map((p, i) => ({
                  id: partnerIds[i],
                  name: p.name || `Ortak ${i + 1}`,
                  color: p.color || '#38bdf8',
                  defaultPct: Number(p.defaultPct) || 0,
                  sortOrder: i,
                })),
                installments: (dto.installments ?? []).map((row, i) => ({
                  id: installmentIds[i],
                  name: row.name,
                  dueDate: String(row.dueDate).slice(0, 10),
                  totalAmount: Number(row.totalAmount) || 0,
                  shareType: 'Default',
                  sortOrder: i,
                  customShares: [],
                  payments: [],
                })),
              };

              return this.plansApi.import(plan.id, exportDto);
            })
          )
        )
      )
      .subscribe({
        next: (importedPlan) => {
          this.parsingDocument.set(false);
          this.toast.success(`Plan dosyasından ${importedPlan.title} başarıyla oluşturuldu.`);
          this.planContext.setPlan(
            importedPlan.id,
            importedPlan.title,
            importedPlan.description,
            importedPlan.planType
          );
          void this.router.navigate(planHomeCommands(importedPlan.id, importedPlan.planType));
        },
        error: (err) => {
          this.parsingDocument.set(false);
          this.toast.error(err?.error?.detail ?? 'Dosya okunamadı veya plan aktarılamadı.');
        },
      });
  }
}
