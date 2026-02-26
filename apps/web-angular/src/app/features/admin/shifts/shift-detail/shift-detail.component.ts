import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PlaceholderComponent } from '../../../../shared/components/placeholder/placeholder.component';

@Component({
  selector: 'app-shift-detail',
  standalone: true,
  imports: [PlaceholderComponent],
  template: `<app-placeholder title="Detalle de Jornada" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShiftDetailComponent {}
