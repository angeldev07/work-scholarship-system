import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PlaceholderComponent } from '../../../../shared/components/placeholder/placeholder.component';

@Component({
  selector: 'app-cycle-detail',
  standalone: true,
  imports: [PlaceholderComponent],
  template: `<app-placeholder title="Detalle de Ciclo" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CycleDetailComponent {}
