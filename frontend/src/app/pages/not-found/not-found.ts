import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { TpButton } from '@team-platform/ui';

@Component({
  selector: 'app-not-found',
  imports: [TpButton],
  templateUrl: './not-found.html',
  styleUrl: './not-found.scss',
})
export class NotFound {
  private readonly router = inject(Router);

  protected goHome(): void {
    void this.router.navigateByUrl('/');
  }
}
