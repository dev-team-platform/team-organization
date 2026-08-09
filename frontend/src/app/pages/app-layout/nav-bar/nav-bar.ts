import { Component, input, output } from '@angular/core';
import {
  TpAvatar,
  TpBellNotification,
  TpButton,
  TpSearchBar,
  TpTextButton,
} from '@team-platform/ui';

@Component({
  selector: 'app-nav-bar',
  imports: [TpAvatar, TpBellNotification, TpButton, TpSearchBar, TpTextButton],
  templateUrl: './nav-bar.html',
  styleUrl: './nav-bar.scss',
})
export class NavBar {
  readonly sideBarCollapsed = input(false);
  readonly sideBarToggle = output<void>();
}
