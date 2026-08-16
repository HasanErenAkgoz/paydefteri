import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PartnerDto, PartnerRequest } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class PartnersApi {
  private readonly http = inject(HttpClient);

  private base(planId: string): string {
    return `${environment.apiUrl}/plans/${planId}/partners`;
  }

  list(planId: string): Observable<PartnerDto[]> {
    return this.http.get<PartnerDto[]>(this.base(planId));
  }

  create(planId: string, body: PartnerRequest): Observable<PartnerDto> {
    return this.http.post<PartnerDto>(this.base(planId), body);
  }

  update(planId: string, partnerId: string, body: PartnerRequest): Observable<PartnerDto> {
    return this.http.put<PartnerDto>(`${this.base(planId)}/${partnerId}`, body);
  }

  delete(planId: string, partnerId: string): Observable<void> {
    return this.http.delete<void>(`${this.base(planId)}/${partnerId}`);
  }
}
