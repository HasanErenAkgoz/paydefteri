import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ExpenseBoardDto,
  ExpenseCategoryDto,
  ExpenseDto,
  PagedExpenseDto,
  ExpenseRecurrenceDto,
  ExpenseRecurrenceRequest,
  ExpenseRequest,
  ExpenseReceiptDraftDto,
  SettlementTransferDto,
  SettlementTransferRequest,
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class ExpensesApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/plans`;

  board(planId: string): Observable<ExpenseBoardDto> {
    return this.http.get<ExpenseBoardDto>(`${this.base}/${planId}/expenses/board`);
  }

  list(planId: string, page = 1, pageSize = 50): Observable<PagedExpenseDto> {
    return this.http.get<PagedExpenseDto>(`${this.base}/${planId}/expenses`, {
      params: { page, pageSize },
    });
  }

  create(planId: string, body: ExpenseRequest): Observable<ExpenseDto> {
    return this.http.post<ExpenseDto>(`${this.base}/${planId}/expenses`, body);
  }

  analyzeReceipt(planId: string, file: File): Observable<ExpenseReceiptDraftDto> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<ExpenseReceiptDraftDto>(
      `${this.base}/${planId}/expenses/analyze-receipt`,
      formData
    );
  }

  update(planId: string, expenseId: string, body: ExpenseRequest): Observable<ExpenseDto> {
    return this.http.put<ExpenseDto>(`${this.base}/${planId}/expenses/${expenseId}`, body);
  }

  delete(planId: string, expenseId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${planId}/expenses/${expenseId}`);
  }

  createCategory(planId: string, name: string, color = '#94a3b8'): Observable<ExpenseCategoryDto> {
    return this.http.post<ExpenseCategoryDto>(`${this.base}/${planId}/expenses/categories`, {
      name,
      color,
    });
  }

  createTransfer(planId: string, body: SettlementTransferRequest): Observable<SettlementTransferDto> {
    return this.http.post<SettlementTransferDto>(`${this.base}/${planId}/expenses/transfers`, body);
  }

  deleteTransfer(planId: string, transferId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${planId}/expenses/transfers/${transferId}`);
  }

  createRecurrence(planId: string, body: ExpenseRecurrenceRequest): Observable<ExpenseRecurrenceDto> {
    return this.http.post<ExpenseRecurrenceDto>(`${this.base}/${planId}/expenses/recurrences`, body);
  }

  deleteRecurrence(planId: string, recurrenceId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${planId}/expenses/recurrences/${recurrenceId}`);
  }
}
