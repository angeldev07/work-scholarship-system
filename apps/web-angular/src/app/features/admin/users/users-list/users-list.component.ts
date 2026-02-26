import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PlaceholderComponent } from '../../../../shared/components/placeholder/placeholder.component';

@Component({
  selector: 'app-users-list',
  standalone: true,
  imports: [PlaceholderComponent],
  template: `<app-placeholder title="Gestión de Usuarios" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UsersListComponent {}
