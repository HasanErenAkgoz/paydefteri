import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  InvitePreviewDto,
  PlanDto,
  PlanInviteDto,
  PlanMemberDto,
  PartnerDto,
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class MembershipApi {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  members(planId: string): Observable<PlanMemberDto[]> {
    return this.http.get<PlanMemberDto[]>(`${this.api}/plans/${planId}/members`);
  }

  invites(planId: string): Observable<PlanInviteDto[]> {
    return this.http.get<PlanInviteDto[]>(`${this.api}/plans/${planId}/invites`);
  }

  invite(planId: string, email: string, partnerId: string): Observable<PlanInviteDto> {
    return this.http.post<PlanInviteDto>(`${this.api}/plans/${planId}/invites`, { email, partnerId });
  }

  revoke(planId: string, inviteId: string): Observable<void> {
    return this.http.delete<void>(`${this.api}/plans/${planId}/invites/${inviteId}`);
  }

  resendInvite(planId: string, inviteId: string): Observable<PlanInviteDto> {
    return this.http.post<PlanInviteDto>(`${this.api}/plans/${planId}/invites/${inviteId}/resend`, {});
  }

  linkSelf(planId: string, partnerId: string): Observable<PartnerDto> {
    return this.http.post<PartnerDto>(`${this.api}/plans/${planId}/link-self`, { partnerId });
  }

  myInvites(): Observable<PlanInviteDto[]> {
    return this.http.get<PlanInviteDto[]>(`${this.api}/invites/mine`);
  }

  preview(token: string): Observable<InvitePreviewDto> {
    return this.http.get<InvitePreviewDto>(`${this.api}/invites/${encodeURIComponent(token)}/preview`);
  }

  accept(token: string): Observable<PlanDto> {
    return this.http.post<PlanDto>(`${this.api}/invites/${encodeURIComponent(token)}/accept`, {});
  }
}
