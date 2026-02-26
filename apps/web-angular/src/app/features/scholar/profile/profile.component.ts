import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PlaceholderComponent } from '../../../shared/components/placeholder/placeholder.component';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [PlaceholderComponent],
  template: `<app-placeholder title="Mi Perfil" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfileComponent {}
