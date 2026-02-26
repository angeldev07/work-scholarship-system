import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PlaceholderComponent } from '../../../shared/components/placeholder/placeholder.component';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [PlaceholderComponent],
  template: `<app-placeholder title="Notificaciones" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotificationsComponent {}
