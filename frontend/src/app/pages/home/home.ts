import { JsonPipe } from '@angular/common';
import { Component, inject } from '@angular/core';
import { AuthService } from '../../core/services/auth-service';

@Component({
  selector: 'app-home',
  imports: [JsonPipe],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home {
  private readonly authService = inject(AuthService);
  readonly currentUser = this.authService.currentUser;
}
