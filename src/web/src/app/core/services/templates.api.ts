import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PlanTemplatePreviewDto, TemplateListItemDto } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class TemplatesApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/templates`;

  list(): Observable<TemplateListItemDto[]> {
    return this.http.get<TemplateListItemDto[]>(this.base);
  }

  preview(templateKey: string): Observable<PlanTemplatePreviewDto> {
    return this.http.get<PlanTemplatePreviewDto>(`${this.base}/${templateKey}`);
  }
}
