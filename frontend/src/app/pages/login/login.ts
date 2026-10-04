import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AuthService } from '../../core/services/auth-service';

@Component({
  selector: 'app-login',
  imports: [],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly authService = inject(AuthService);

  ngOnInit() {
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/';
    const absoluteReturnUrl = new URL(returnUrl, window.location.origin).toString();
    this.authService.login(absoluteReturnUrl);
  }
}
