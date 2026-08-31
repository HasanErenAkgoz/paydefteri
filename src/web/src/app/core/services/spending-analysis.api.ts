import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  SpendingStatementDetail,
  SpendingStatementListItem,
  SpendingBudget,
  SpendingBudgetStatus,
  SpendingDashboard,
  SpendingPatterns,
  SpendingCoachResponse,
} from '../../features/spending-analysis/spending-analysis.models';

@Injectable({ providedIn: 'root' })
export class SpendingAnalysisApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/spending-analysis`;

  listStatements(): Observable<SpendingStatementListItem[]> {
    return this.http.get<SpendingStatementListItem[]>(`${this.base}/statements`);
  }

  getStatement(statementId: string): Observable<SpendingStatementDetail> {
    return this.http.get<SpendingStatementDetail>(`${this.base}/statements/${statementId}`);
  }

  uploadStatement(file: File): Observable<SpendingStatementDetail> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<SpendingStatementDetail>(`${this.base}/statements`, form);
  }

  updateCategory(statementId: string, transactionId: string, category: string): Observable<void> {
    return this.http.patch<void>(`${this.base}/statements/${statementId}/transactions/${transactionId}/category`, {
      category,
    });
  }

  deleteStatement(statementId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/statements/${statementId}`);
  }

  getDashboard(statementId: string): Observable<SpendingDashboard> {
    return this.http.get<SpendingDashboard>(`${this.base}/statements/${statementId}/dashboard`);
  }

  getPatterns(statementId: string): Observable<SpendingPatterns> {
    return this.http.get<SpendingPatterns>(`${this.base}/statements/${statementId}/patterns`);
  }

  getBudgetStatus(statementId: string): Observable<SpendingBudgetStatus> {
    return this.http.get<SpendingBudgetStatus>(`${this.base}/statements/${statementId}/budget-status`);
  }

  getCoach(statementId: string): Observable<SpendingCoachResponse> {
    return this.http.post<SpendingCoachResponse>(`${this.base}/statements/${statementId}/coach`, null);
  }

  askCoach(statementId: string, question: string): Observable<SpendingCoachResponse> {
    return this.http.post<SpendingCoachResponse>(`${this.base}/statements/${statementId}/ask`, { question });
  }

  listBudgets(month: string): Observable<SpendingBudget[]> {
    return this.http.get<SpendingBudget[]>(`${this.base}/budgets/${month}`);
  }

  saveBudget(month: string, category: string, amount: number, currency: string): Observable<SpendingBudget> {
    return this.http.put<SpendingBudget>(`${this.base}/budgets/${month}/${encodeURIComponent(category)}`, {
      amount,
      currency,
    });
  }
}
