import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PlaceholderComponent } from '../../../shared/components/placeholder/placeholder.component';

@Component({
  selector: 'app-hours',
  standalone: true,
  imports: [PlaceholderComponent],
  template: `<app-placeholder title="Mis Horas Acumuladas" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HoursComponent {}
