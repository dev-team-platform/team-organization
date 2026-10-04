import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { GetUsersRequest, GetUsersResponse } from '../models/users/get-users';

@Injectable({
  providedIn: 'root',
})
export class UserService {
  private readonly httpClient = inject(HttpClient);

  getUsers(request: GetUsersRequest): Observable<GetUsersResponse> {
    return this.httpClient.post<GetUsersResponse>('users/all', request);
  }
}
