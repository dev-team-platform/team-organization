import { Component, input, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TpTextButton } from '@team-platform/ui';

interface SideBarNavigationItem {
  label: string;
  icon: string;
  route: string | null;
  type: 'navigate' | 'dropdown';
  children: SideBarNavigationItem[];
}

@Component({
  selector: 'app-side-bar',
  imports: [RouterLink, RouterLinkActive, TpTextButton],
  host: {
    '[class.tt-side-bar-host--collapsed]': 'collapsed()',
  },
  templateUrl: './side-bar.html',
  styleUrl: './side-bar.scss',
})
export class SideBar {
  readonly collapsed = input(false);
  protected readonly expandedDropdownLabels = signal<ReadonlySet<string>>(new Set());

  protected readonly workspaceItems: SideBarNavigationItem[] = [
    { label: 'Home', icon: 'home', route: '/', type: 'navigate', children: [] },
    {
      label: 'My tickets',
      icon: 'assignment',
      route: '/my-tickets',
      type: 'navigate',
      children: [],
    },
    {
      label: 'Projects',
      icon: 'folder_open',
      route: null,
      type: 'dropdown',
      children: [
        {
          label: 'All projects',
          icon: 'folder_open',
          route: '/projects',
          type: 'navigate',
          children: [],
        },
        {
          label: 'Active projects',
          icon: 'folder_open',
          route: '/projects/active',
          type: 'navigate',
          children: [],
        },
        {
          label: 'Archived projects',
          icon: 'inventory_2',
          route: '/projects/archived',
          type: 'navigate',
          children: [],
        },
      ],
    },
  ];

  protected readonly planningItems: SideBarNavigationItem[] = [
    { label: 'Boards', icon: 'view_kanban', route: '/boards', type: 'navigate', children: [] },
    { label: 'Reports', icon: 'bar_chart', route: '/reports', type: 'navigate', children: [] },
  ];

  protected isDropdownExpanded(item: SideBarNavigationItem): boolean {
    return this.expandedDropdownLabels().has(item.label);
  }

  protected toggleDropdown(item: SideBarNavigationItem): void {
    this.expandedDropdownLabels.update((expandedLabels) => {
      const nextExpandedLabels = new Set(expandedLabels);

      if (nextExpandedLabels.has(item.label)) {
        nextExpandedLabels.delete(item.label);
      } else {
        nextExpandedLabels.add(item.label);
      }

      return nextExpandedLabels;
    });
  }
}
