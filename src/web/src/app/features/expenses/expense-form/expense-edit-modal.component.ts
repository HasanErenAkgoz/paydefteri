import { Component, OnChanges, SimpleChanges, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  ExpenseCategoryDto,
  ExpenseDto,
  ExpenseRequest,
  ShareType,
} from '../../../core/models/api.models';
import { ExpensePartnerOption } from '../expense-partner-option';
import { ExpenseCustomSharesComponent } from './expense-custom-shares.component';
import { amountsMatchTotal } from './expense-form-calculations';
import { ExpensePayerInputsComponent, ExpensePayerState } from './expense-payer-inputs.component';

type ExpenseShareUi = 'Equal' | 'Default' | 'sole' | 'Custom';

export interface ExpenseEditSaveEvent {
  expenseId: string;
  request: ExpenseRequest;
}

@Component({
  selector: 'app-expense-edit-modal',
  standalone: true,
  imports: [FormsModule, ExpenseCustomSharesComponent, ExpensePayerInputsComponent],
  templateUrl: './expense-edit-modal.component.html',
  styleUrl: './expense-edit-modal.component.scss',
})
export class ExpenseEditModalComponent implements OnChanges {
  readonly expense = input.required<ExpenseDto>();
  readonly categories = input.required<ExpenseCategoryDto[]>();
  readonly partners = input.required<ExpensePartnerOption[]>();
  readonly saving = input.required<boolean>();
  readonly save = output<ExpenseEditSaveEvent>();
  readonly cancel = output<void>();
  readonly validationError = output<string>();

  name = '';
  amount: number | null = null;
  occurredOn = '';
  categoryId = '';
  shareUi: ExpenseShareUi = 'Equal';
  solePartnerId = '';
  customShares: Record<string, number> = {};
  status: 'Paid' | 'Planned' = 'Planned';
  note = '';
  payerState: ExpensePayerState = { singlePayment: false, payerId: '', payments: {} };

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['expense'] || changes['partners']) {
      this.initialize(this.expense());
    }
  }

  sharePartners(): ExpensePartnerOption[] {
    if (this.shareUi === 'sole') {
      const sole = this.partners().find((partner) => partner.id === this.solePartnerId) ?? this.partners()[0];
      return sole ? [sole] : [];
    }
    if (this.shareUi === 'Custom') {
      const selected = this.partners().filter((partner) => Number(this.customShares[partner.id]) > 0.005);
      return selected.length ? selected : this.partners();
    }
    return this.partners();
  }

  onShareOrAmountChange(): void {
    if (this.shareUi === 'sole' && !this.solePartnerId) {
      this.solePartnerId = this.partners()[0]?.id ?? '';
    }
    if (this.shareUi === 'Custom') {
      this.distributeCustomShares();
    }
    this.syncPayments();
  }

  onCustomSharesChange(shares: Record<string, number>): void {
    this.customShares = shares;
    this.syncPayments();
  }

  syncPayments(): void {
    const total = Number(this.amount) || 0;
    const partners = this.sharePartners();
    const amounts = this.distribute(total, partners);
    if (this.shareUi === 'Custom') {
      for (const partner of partners) {
        amounts[partner.id] = Number(this.customShares[partner.id]) || 0;
      }
    } else if (this.shareUi === 'sole' && partners[0]) {
      amounts[partners[0].id] = total;
    }
    this.payerState = { ...this.payerState, payments: amounts };
  }

  submit(): void {
    const total = Number(this.amount);
    if (!this.name.trim() || !(total > 0)) {
      this.validationError.emit('Ad ve tutar gerekli.');
      return;
    }

    const shares = this.buildShares(total);
    if (!shares) {
      return;
    }

    const paymentResult = this.buildPayments(total);
    if (!paymentResult) {
      return;
    }

    this.save.emit({
      expenseId: this.expense().id,
      request: {
        name: this.name.trim(),
        occurredOn: this.occurredOn,
        totalAmount: total,
        shareType: shares.shareType,
        status: this.status,
        paidByPartnerId: paymentResult.paidByPartnerId,
        categoryId: this.categoryId || null,
        note: this.note.trim(),
        customShares: shares.customShares,
        payments: paymentResult.payments,
      },
    });
  }

  private initialize(expense: ExpenseDto): void {
    const partners = this.partners();
    this.name = expense.name;
    this.amount = Number(expense.totalAmount);
    this.occurredOn = String(expense.occurredOn).slice(0, 10);
    this.categoryId = expense.categoryId ?? '';
    this.status = expense.status === 'Paid' || expense.status === 1 ? 'Paid' : 'Planned';
    this.note = expense.note ?? '';
    this.customShares = Object.fromEntries(
      partners.map((partner) => {
        const customAmount = expense.customShares?.find((share) => share.partnerId === partner.id)?.amount;
        const lineAmount = expense.shareLines?.find((line) => line.partnerId === partner.id)?.shareAmount;
        return [partner.id, Number(customAmount ?? lineAmount ?? 0)];
      })
    );

    this.initializeShareType(expense);
    const sharePartners = this.sharePartners();
    const payments = expense.payments?.filter((payment) => Number(payment.amount) > 0.005) ?? [];
    const paymentMap = Object.fromEntries(
      sharePartners.map((partner) => {
        const payment = payments.find((item) => item.partnerId === partner.id)?.amount;
        const share = expense.shareLines?.find((line) => line.partnerId === partner.id)?.shareAmount;
        return [partner.id, Number(payment ?? share ?? 0)];
      })
    );
    this.payerState = {
      singlePayment: sharePartners.length <= 1,
      payerId: payments[0]?.partnerId ?? expense.paidByPartnerId ?? partners[0]?.id ?? '',
      payments: paymentMap,
    };
  }

  private initializeShareType(expense: ExpenseDto): void {
    const shareType = this.normalizeShareType(expense.shareType);
    const firstPartnerId = this.partners()[0]?.id ?? '';
    if (shareType !== 'Custom') {
      this.shareUi = shareType;
      this.solePartnerId = firstPartnerId;
      return;
    }

    const total = Number(expense.totalAmount);
    const sole = this.partners().find(
      (partner) => Math.abs(Number(this.customShares[partner.id]) - total) <= 0.01
    );
    const othersAreZero = this.partners().every(
      (partner) => partner.id === sole?.id || Math.abs(Number(this.customShares[partner.id])) <= 0.01
    );
    this.shareUi = sole && othersAreZero ? 'sole' : 'Custom';
    this.solePartnerId = sole?.id ?? firstPartnerId;
  }

  private distributeCustomShares(): void {
    this.customShares = this.distribute(Number(this.amount) || 0, this.partners());
  }

  private distribute(total: number, partners: ExpensePartnerOption[]): Record<string, number> {
    if (!partners.length) {
      return {};
    }
    const base = Math.floor((total / partners.length) * 100) / 100;
    let assigned = 0;
    return Object.fromEntries(
      partners.map((partner, index) => {
        const amount = index === partners.length - 1
          ? Math.round((total - assigned) * 100) / 100
          : base;
        assigned += amount;
        return [partner.id, amount];
      })
    );
  }

  private buildShares(total: number): {
    shareType: ShareType;
    customShares: { partnerId: string; amount: number }[];
  } | null {
    if (this.shareUi === 'sole') {
      if (!this.solePartnerId) {
        this.validationError.emit('Pay sahibi ortağını seçin.');
        return null;
      }
      return {
        shareType: 'Custom',
        customShares: this.partners().map((partner) => ({
          partnerId: partner.id,
          amount: partner.id === this.solePartnerId ? total : 0,
        })),
      };
    }
    if (this.shareUi === 'Custom') {
      if (!amountsMatchTotal(this.customShares, total)) {
        this.validationError.emit('Özel payların toplamı tutara eşit olmalı.');
        return null;
      }
      return {
        shareType: 'Custom',
        customShares: this.partners().map((partner) => ({
          partnerId: partner.id,
          amount: Number(this.customShares[partner.id]) || 0,
        })),
      };
    }
    return { shareType: this.shareUi, customShares: [] };
  }

  private buildPayments(total: number): {
    paidByPartnerId: string | null;
    payments: { partnerId: string; amount: number }[] | undefined;
  } | null {
    if (this.status !== 'Paid') {
      return { paidByPartnerId: null, payments: undefined };
    }
    const partners = this.sharePartners();
    if (this.payerState.singlePayment || partners.length <= 1) {
      const payerId = this.payerState.payerId || partners[0]?.id || this.partners()[0]?.id;
      if (!payerId) {
        this.validationError.emit('Ödeyen ortak seçin.');
        return null;
      }
      return { paidByPartnerId: payerId, payments: [{ partnerId: payerId, amount: total }] };
    }
    const payments = partners
      .map((partner) => ({ partnerId: partner.id, amount: Number(this.payerState.payments[partner.id]) || 0 }))
      .filter((payment) => payment.amount > 0);
    const paymentTotal = payments.reduce((sum, payment) => sum + payment.amount, 0);
    if (!payments.length || Math.abs(paymentTotal - total) > 0.01) {
      this.validationError.emit('Ödeyen tutarlarının toplamı gider tutarına eşit olmalı.');
      return null;
    }
    return {
      paidByPartnerId: payments.length === 1 ? payments[0]!.partnerId : null,
      payments,
    };
  }

  private normalizeShareType(value: ShareType | string | number): 'Default' | 'Equal' | 'Custom' {
    if (value === 0 || value === 'Default') return 'Default';
    if (value === 2 || value === 'Custom') return 'Custom';
    return 'Equal';
  }
}
