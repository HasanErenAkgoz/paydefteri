import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  BulkIncreaseRequest,
  InstallmentDto,
  InstallmentRequest,
  PaymentDto,
  PaymentRequest,
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class InstallmentsApi {
  private readonly http = inject(HttpClient);

  private base(planId: string): string {
    return `${environment.apiUrl}/plans/${planId}/installments`;
  }

  list(planId: string): Observable<InstallmentDto[]> {
    return this.http.get<InstallmentDto[]>(this.base(planId));
  }

  create(planId: string, body: InstallmentRequest): Observable<InstallmentDto> {
    return this.http.post<InstallmentDto>(this.base(planId), body);
  }

  update(planId: string, installmentId: string, body: InstallmentRequest): Observable<InstallmentDto> {
    return this.http.put<InstallmentDto>(`${this.base(planId)}/${installmentId}`, body);
  }

  delete(planId: string, installmentId: string): Observable<void> {
    return this.http.delete<void>(`${this.base(planId)}/${installmentId}`);
  }

  upsertPayment(
    planId: string,
    installmentId: string,
    partnerId: string,
    body: PaymentRequest
  ): Observable<PaymentDto> {
    return this.http.put<PaymentDto>(
      `${this.base(planId)}/${installmentId}/payments/${partnerId}`,
      body
    );
  }

  bulkIncrease(planId: string, body: BulkIncreaseRequest): Observable<InstallmentDto[]> {
    return this.http.post<InstallmentDto[]>(`${this.base(planId)}/bulk-increase`, body);
  }
}
