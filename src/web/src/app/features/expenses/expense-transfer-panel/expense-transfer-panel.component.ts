import { Component, OnChanges, SimpleChanges, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SettlementTransferDto, SettlementTransferRequest } from '../../../core/models/api.models';
import { ExpensePartnerOption } from '../expense-partner-option';
import { CurrencyTryPipe } from '../../../shared/pipes/currency-try.pipe';
import { formatDateTr } from '../../../shared/utils/format';

@Component({
  selector: 'app-expense-transfer-panel',
  standalone: true,
  imports: [FormsModule, CurrencyTryPipe],
  templateUrl: './expense-transfer-panel.component.html',
  styleUrl: './expense-transfer-panel.component.scss',
})
export class ExpenseTransferPanelComponent implements OnChanges {
  readonly partners = input.required<ExpensePartnerOption[]>();
  readonly transfers = input.required<SettlementTransferDto[]>();
  readonly isOwner = input.required<boolean>();
  readonly hasOpenBalance = input.required<boolean>();
  readonly saving = input.required<boolean>();
  readonly initialFromPartnerId = input('');
  readonly initialToPartnerId = input('');
  readonly initialAmount = input<number | null>(null);
  readonly add = output<SettlementTransferRequest>();
  readonly remove = output<string>();

  visible = false;
  fromPartnerId = '';
  toPartnerId = '';
  amount: number | null = null;
  transferredOn = new Date().toISOString().slice(0, 10);
  formatDateTr = formatDateTr;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['initialFromPartnerId']) this.fromPartnerId = this.initialFromPartnerId();
    if (changes['initialToPartnerId']) this.toPartnerId = this.initialToPartnerId();
    if (changes['initialAmount'] && this.amount == null) this.amount = this.initialAmount();
    if ((changes['hasOpenBalance'] || changes['isOwner']) && this.hasOpenBalance() && this.isOwner()) {
      this.visible = true;
    }
  }

  submit(): void {
    const amount = Number(this.amount);
    if (!(amount > 0) || !this.fromPartnerId || !this.toPartnerId || this.fromPartnerId === this.toPartnerId) {
      return;
    }
    this.add.emit({ fromPartnerId: this.fromPartnerId, toPartnerId: this.toPartnerId, amount, transferredOn: this.transferredOn });
    this.amount = null;
  }
}
