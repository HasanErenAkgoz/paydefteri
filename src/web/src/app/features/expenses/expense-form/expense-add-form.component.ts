import { Component, OnChanges, SimpleChanges, inject, input, output } from '@angular/core';
import {
  ExpenseCategoryDto,
  ExpenseReceiptDraftDto,
  ExpenseRequest,
  ShareType,
} from '../../../core/models/api.models';
import { ExpensePartnerOption } from '../expense-partner-option';
import { ExpenseBaseFieldsComponent, ExpenseBaseFieldsState } from './expense-base-fields.component';
import { ExpenseCustomSharesComponent } from './expense-custom-shares.component';
import { ExpenseInstallmentPreviewComponent } from './expense-installment-preview.component';
import { ExpensePayerInputsComponent, ExpensePayerState } from './expense-payer-inputs.component';
import { amountsMatchTotal, getInstallmentPreview } from './expense-form-calculations';
import { CameraService } from '../../../core/platform/camera.service';

@Component({
  selector: 'app-expense-add-form',
  standalone: true,
  imports: [ExpenseBaseFieldsComponent, ExpenseCustomSharesComponent, ExpenseInstallmentPreviewComponent, ExpensePayerInputsComponent],
  templateUrl: './expense-add-form.component.html',
  styleUrl: './expense-add-form.component.scss',
})
export class ExpenseAddFormComponent implements OnChanges {
  readonly camera = inject(CameraService);
  readonly categories = input.required<ExpenseCategoryDto[]>();
  readonly partners = input.required<ExpensePartnerOption[]>();
  readonly saving = input.required<boolean>();
  readonly analyzingReceipt = input(false);
  readonly receiptDraft = input<ExpenseReceiptDraftDto | null>(null);
  readonly save = output<ExpenseRequest>();
  readonly receiptSelected = output<File>();
  readonly formCancelled = output<void>();
  readonly validationError = output<string>();

  state: ExpenseBaseFieldsState = this.defaultState();
  customShares: Record<string, number> = {};
  payerState: ExpensePayerState = { singlePayment: false, payerId: '', payments: {} };

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['partners'] && this.partners().length) {
      this.state = { ...this.state, solePartnerId: this.state.solePartnerId || this.partners()[0]!.id };
      this.payerState = { ...this.payerState, payerId: this.payerState.payerId || this.partners()[0]!.id };
      this.syncPayments();
    }
    if (changes['categories'] && !this.state.categoryId && this.categories()[0]) {
      this.state = { ...this.state, categoryId: this.categories()[0]!.id };
    }
    if (changes['receiptDraft'] && this.receiptDraft()) {
      this.applyReceiptDraft(this.receiptDraft()!);
    }
  }

  updateState(next: ExpenseBaseFieldsState): void {
    const shouldSync = next.amount !== this.state.amount || next.shareUi !== this.state.shareUi || next.solePartnerId !== this.state.solePartnerId;
    this.state = next;
    if (shouldSync) this.syncPayments();
  }

  preview(): { count: number; baseAmount: number; finalAmount: number } | null {
    return this.state.paymentMode === 'Installment'
      ? getInstallmentPreview(Number(this.state.amount), Number(this.state.installmentCount))
      : null;
  }

  sharePartners(): ExpensePartnerOption[] {
    if (this.state.shareUi === 'sole') return this.partners().filter((partner) => partner.id === this.state.solePartnerId);
    if (this.state.shareUi === 'Custom') {
      const selected = this.partners().filter((partner) => Number(this.customShares[partner.id]) > .005);
      return selected.length ? selected : this.partners();
    }
    return this.partners();
  }

  submit(): void {
    const amount = Number(this.state.amount);
    if (!this.state.name.trim() || !(amount > 0)) return this.validationError.emit('Gider adı ve tutarı zorunludur.');
    if (this.state.paymentMode === 'Installment' && !this.preview()) return this.validationError.emit('Taksit sayısı 2–120 arasında olmalıdır.');
    if (this.state.shareUi === 'Custom' && !amountsMatchTotal(this.customShares, amount)) return this.validationError.emit('Özel payların toplamı gider tutarına eşit olmalıdır.');

    const shareType: ShareType = this.state.shareUi === 'Custom' || this.state.shareUi === 'sole'
      ? 'Custom'
      : this.state.shareUi;
    const customShares = shareType === 'Custom' ? this.partners().map((partner) => ({ partnerId: partner.id, amount: this.state.shareUi === 'sole' ? (partner.id === this.state.solePartnerId ? amount : 0) : Number(this.customShares[partner.id] || 0) })) : [];
    const payments = this.state.status === 'Paid'
      ? (this.payerState.singlePayment || this.sharePartners().length <= 1
        ? [{ partnerId: this.payerState.payerId, amount }]
        : this.sharePartners().map((partner) => ({ partnerId: partner.id, amount: Number(this.payerState.payments[partner.id] || 0) })).filter((payment) => payment.amount > 0))
      : [];
    if (this.state.status === 'Paid' && (!this.payerState.payerId || !amountsMatchTotal(Object.fromEntries(payments.map((payment) => [payment.partnerId, payment.amount])), amount))) return this.validationError.emit('Ödeme toplamı gider tutarına eşit olmalıdır.');

    this.save.emit({ name: this.state.name.trim(), occurredOn: this.state.occurredOn, totalAmount: amount, shareType, status: this.state.status, paidByPartnerId: payments.length === 1 ? payments[0]!.partnerId : null, categoryId: this.state.categoryId || null, note: this.state.note.trim(), customShares, payments, installmentCount: this.state.paymentMode === 'Installment' ? Number(this.state.installmentCount) : 1 });
  }

  syncPayments(): void {
    const amount = Number(this.state.amount) || 0;
    const partners = this.sharePartners();
    const equal = partners.length ? Math.floor((amount / partners.length) * 100) / 100 : 0;
    let assigned = 0;
    const payments: Record<string, number> = {};
    partners.forEach((partner, index) => { payments[partner.id] = index === partners.length - 1 ? Math.round((amount - assigned) * 100) / 100 : equal; assigned += index === partners.length - 1 ? 0 : equal; });
    this.payerState = { ...this.payerState, payments };
  }

  selectReceipt(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file) this.receiptSelected.emit(file);
    input.value = '';
  }

  async captureNativeReceipt(): Promise<void> {
    await this.pickNativeReceipt(() => this.camera.captureReceipt());
  }

  async selectNativeReceipt(): Promise<void> {
    await this.pickNativeReceipt(() => this.camera.selectReceipt());
  }

  receiptReviewMessage(): string {
    const draft = this.receiptDraft();
    if (!draft) return '';
    const labels: Record<string, string> = {
      name: 'gider adı',
      totalAmount: 'tutar',
      occurredOn: 'tarih',
      categoryName: 'kategori',
      installmentCount: 'taksit sayısı',
    };
    const fields = draft.lowConfidenceFields.map((field) => labels[field] ?? field);
    return fields.length
      ? `Düşük güvenli alanlar: ${fields.join(', ')}. Lütfen kontrol edin.`
      : 'Fiş bilgileri forma aktarıldı. Kaydetmeden önce kontrol edin.';
  }

  private applyReceiptDraft(draft: ExpenseReceiptDraftDto): void {
    const installmentCount = Number(draft.installmentCount);
    const noteParts = [draft.note, draft.documentNumber ? `Belge no: ${draft.documentNumber}` : null]
      .filter((value): value is string => !!value?.trim());
    this.state = {
      ...this.state,
      name: draft.name?.trim() || this.state.name,
      amount: draft.totalAmount ?? this.state.amount,
      occurredOn: draft.occurredOn || this.state.occurredOn,
      categoryId: draft.categoryId || this.state.categoryId,
      paymentMode: installmentCount >= 2 ? 'Installment' : 'Cash',
      installmentCount: installmentCount >= 2 ? installmentCount : 1,
      note: noteParts.join(' · ') || this.state.note,
    };
    this.syncPayments();
  }

  private async pickNativeReceipt(pick: () => Promise<File | null>): Promise<void> {
    if (this.analyzingReceipt()) {
      return;
    }

    try {
      const file = await pick();
      if (file) {
        this.receiptSelected.emit(file);
      }
    } catch {
      this.validationError.emit('Fotoğraf açılamadı. Kamera ve fotoğraf izinlerini kontrol edin.');
    }
  }

  private defaultState(): ExpenseBaseFieldsState { return { name: '', amount: null, paymentMode: 'Cash', installmentCount: 1, occurredOn: new Date().toISOString().slice(0, 10), categoryId: '', shareUi: 'Equal', solePartnerId: '', status: 'Paid', note: '' }; }
}
