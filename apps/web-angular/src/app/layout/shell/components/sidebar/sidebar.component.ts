import {
  ChangeDetectionStrategy,
  Component,
  InputSignal,
  OutputEmitterRef,
  Signal,
  input,
  output,
  signal,
} from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { animate, style, transition, trigger } from '@angular/animations';
import { TooltipModule } from 'primeng/tooltip';
import { BadgeModule } from 'primeng/badge';
import { RippleModule } from 'primeng/ripple';
import { DividerModule } from 'primeng/divider';
import { NavConfig, NavItem, PendingCounts } from '../../models/navigation.models';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, TooltipModule, BadgeModule, RippleModule, DividerModule],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  animations: [
    trigger('submenuExpand', [
      transition(':enter', [
        style({ height: 0, opacity: 0, overflow: 'hidden' }),
        animate('200ms ease-out', style({ height: '*', opacity: 1 })),
      ]),
      transition(':leave', [
        style({ overflow: 'hidden' }),
        animate('150ms ease-in', style({ height: 0, opacity: 0 })),
      ]),
    ]),
    trigger('labelFade', [
      transition(':enter', [
        style({ opacity: 0, transform: 'translateX(-4px)' }),
        animate('200ms ease-out', style({ opacity: 1, transform: 'translateX(0)' })),
      ]),
      transition(':leave', [
        animate('150ms ease-in', style({ opacity: 0, transform: 'translateX(-4px)' })),
      ]),
    ]),
  ],
})
export class SidebarComponent {
  readonly collapsed: InputSignal<boolean> = input<boolean>(false);
  readonly navConfig: InputSignal<NavConfig> = input<NavConfig>([]);
  readonly pendingCounts: InputSignal<PendingCounts> = input<PendingCounts>({
    shifts: 0,
    absences: 0,
    applicants: 0,
  });

  /** Emitted when a nav item is clicked (used by mobile drawer to close itself) */
  readonly itemClicked: OutputEmitterRef<void> = output<void>();

  /** Track which parent items have their sub-menu expanded */
  private readonly expandedItems = signal<Set<string>>(new Set());

  isExpanded(itemId: string): boolean {
    return this.expandedItems().has(itemId);
  }

  toggleExpand(itemId: string): void {
    this.expandedItems.update((set) => {
      const next = new Set(set);
      if (next.has(itemId)) {
        next.delete(itemId);
      } else {
        next.add(itemId);
      }
      return next;
    });
  }

  getBadgeCount(item: NavItem): number {
    if (!item.badgeKey) return 0;
    const counts = this.pendingCounts();
    return counts[item.badgeKey as keyof PendingCounts] ?? 0;
  }

  onItemClick(item: NavItem): void {
    if (item.children?.length) {
      if (!this.collapsed()) {
        this.toggleExpand(item.id);
      }
    } else {
      this.itemClicked.emit();
    }
  }

  getTooltipText(item: NavItem): string {
    return this.collapsed() ? item.label : '';
  }

  trackByItemId(_index: number, item: NavItem): string {
    return item.id;
  }
}
