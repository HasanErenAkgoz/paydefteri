import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreatePlanRequest,
  DashboardDto,
  PlanActivityItemDto,
  PlanDocumentPreviewDto,
  PlanDto,
  PlanExportDto,
  ReminderHistoryItemDto,
  ReportSummaryDto,
  UpdatePlanRequest,
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class PlansApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/plans`;

  list(includeArchived = false): Observable<PlanDto[]> {
    let params = new HttpParams();
    if (includeArchived) {
      params = params.set('includeArchived', 'true');
    }
    return this.http.get<PlanDto[]>(this.base, { params });
  }

  get(planId: string): Observable<PlanDto> {
    return this.http.get<PlanDto>(`${this.base}/${planId}`);
  }

  create(body: CreatePlanRequest): Observable<PlanDto> {
    return this.http.post<PlanDto>(this.base, body);
  }

  update(planId: string, body: UpdatePlanRequest): Observable<PlanDto> {
    return this.http.put<PlanDto>(`${this.base}/${planId}`, body);
  }

  delete(planId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${planId}`);
  }

  archive(planId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${planId}/archive`, {});
  }

  restore(planId: string): Observable<PlanDto> {
    return this.http.post<PlanDto>(`${this.base}/${planId}/restore`, {});
  }

  copy(planId: string): Observable<PlanDto> {
    return this.http.post<PlanDto>(`${this.base}/${planId}/copy`, {});
  }

  seedFuzul(planId: string): Observable<PlanDto> {
    return this.seed(planId, 'fuzul');
  }

  seed(
    planId: string,
    templateKey: string,
    body?: {
      title?: string;
      description?: string;
      partners?: { name: string; color: string; defaultPct: number }[];
      expenses?: { name: string; occurredOn: string; totalAmount: number }[];
    } | null
  ): Observable<PlanDto> {
    return this.http.post<PlanDto>(
      `${this.base}/${planId}/seed/${encodeURIComponent(templateKey)}`,
      body ?? {}
    );
  }

  settleUp(planId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${planId}/settle-up`, {});
  }

  dashboard(planId: string): Observable<DashboardDto> {
    return this.http.get<DashboardDto>(`${this.base}/${planId}/dashboard`);
  }

  reportSummary(planId: string): Observable<ReportSummaryDto> {
    return this.http.get<ReportSummaryDto>(`${this.base}/${planId}/report-summary`);
  }

  reminders(planId: string): Observable<ReminderHistoryItemDto[]> {
    return this.http.get<ReminderHistoryItemDto[]>(`${this.base}/${planId}/reminders`);
  }

  activity(planId: string): Observable<PlanActivityItemDto[]> {
    return this.http.get<PlanActivityItemDto[]>(`${this.base}/${planId}/activity`);
  }

  parseDocument(planId: string, file: File): Observable<PlanDocumentPreviewDto> {
    const form = new FormData();
    form.append('file', file, file.name);
    return this.http.post<PlanDocumentPreviewDto>(`${this.base}/${planId}/parse-document`, form);
  }

  export(planId: string): Observable<PlanExportDto> {
    return this.http.get<PlanExportDto>(`${this.base}/${planId}/export`);
  }

  import(planId: string, data: PlanExportDto): Observable<PlanDto> {
    return this.http.post<PlanDto>(`${this.base}/${planId}/import`, data);
  }
}
