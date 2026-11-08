import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ExportResponse } from '../models/export';
import { GetUsersRequest } from '../models/users';
import { AppHttpContext } from './http-context';

@Injectable({
  providedIn: 'root',
})
export class ExportService {
  private readonly httpClient = inject(HttpClient);
  private readonly appHttpContext = inject(AppHttpContext);

  exportUsers(request: GetUsersRequest): Observable<ExportResponse> {
    return this.httpClient.post<ExportResponse>('export/users', request, {
      context: this.appHttpContext.baseContext,
    });
  }
}
