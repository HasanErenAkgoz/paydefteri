import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreatePlanRequest,
  DashboardDto,
  PlanDto,
  PlanExportDto,
  UpdatePlanRequest,
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class PlansApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/plans`;

  list(): Observable<PlanDto[]> {
    return this.http.get<PlanDto[]>(this.base);
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

  seedFuzul(planId: string): Observable<PlanDto> {
    return this.http.post<PlanDto>(`${this.base}/${planId}/seed/fuzul`, {});
  }

  dashboard(planId: string): Observable<DashboardDto> {
    return this.http.get<DashboardDto>(`${this.base}/${planId}/dashboard`);
  }

  export(planId: string): Observable<PlanExportDto> {
    return this.http.get<PlanExportDto>(`${this.base}/${planId}/export`);
  }

  import(planId: string, data: PlanExportDto): Observable<PlanDto> {
    return this.http.post<PlanDto>(`${this.base}/${planId}/import`, data);
  }
}
