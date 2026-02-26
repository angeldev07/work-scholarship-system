import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PlaceholderComponent } from '../../../../shared/components/placeholder/placeholder.component';

@Component({
  selector: 'app-user-detail',
  standalone: true,
  imports: [PlaceholderComponent],
  template: `<app-placeholder title="Detalle de Usuario" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserDetailComponent {}
