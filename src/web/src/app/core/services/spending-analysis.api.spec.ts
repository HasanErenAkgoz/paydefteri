import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SpendingAnalysisApi } from './spending-analysis.api';

describe('SpendingAnalysisApi', () => {
  let api: SpendingAnalysisApi;
  let http: HttpTestingController;
  const statementId = '71ece420-8aa7-4ef3-b4bb-45380f948843';

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(SpendingAnalysisApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('uploads only the selected statement file as multipart data', async () => {
    const file = new File(['date,description,amount'], 'agustos.csv', { type: 'text/csv' });
    const promise = firstValueFrom(api.uploadStatement(file));

    const request = http.expectOne(`${environment.apiUrl}/spending-analysis/statements`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body instanceof FormData).toBeTrue();
    const uploaded = (request.request.body as FormData).get('file') as File;
    expect(uploaded.name).toBe('agustos.csv');
    expect(uploaded.size).toBe(file.size);
    request.flush(statement());

    await expectAsync(promise).toBeResolved();
  });

  it('sends coach questions only in the request body', async () => {
    const promise = firstValueFrom(api.askCoach(statementId, 'En büyük 5 işlem hangisi?'));

    const request = http.expectOne(`${environment.apiUrl}/spending-analysis/statements/${statementId}/ask`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ question: 'En büyük 5 işlem hangisi?' });
    request.flush(coach());

    await expectAsync(promise).toBeResolved();
  });

  it('encodes category names in budget routes', async () => {
    const promise = firstValueFrom(api.saveBudget('2026-08', 'Restoran / Yemek', 4500, 'TRY'));

    const request = http.expectOne(`${environment.apiUrl}/spending-analysis/budgets/2026-08/Restoran%20%2F%20Yemek`);
    expect(request.request.body).toEqual({ amount: 4500, currency: 'TRY' });
    request.flush({ id: '1', month: '2026-08', category: 'Restoran / Yemek', currency: 'TRY', amount: 4500 });

    await expectAsync(promise).toBeResolved();
  });

  function statement() {
    return { id: statementId, sourceFileName: 'agustos.csv', sourceKind: 'CSV', periodStart: null, periodEnd: null, totals: [], transactions: [], warnings: [], createdAtUtc: '2026-08-30T00:00:00Z' };
  }

  function coach() {
    return { answer: 'Yanıt', insights: [], recommendations: [], evidence: {}, evidenceKeys: [], disclaimer: 'Bilgilendirme', dataAvailable: true, aiGenerated: false };
  }
});
