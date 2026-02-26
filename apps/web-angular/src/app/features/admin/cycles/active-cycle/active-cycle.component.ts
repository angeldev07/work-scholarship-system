import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PlaceholderComponent } from '../../../../shared/components/placeholder/placeholder.component';

@Component({
  selector: 'app-active-cycle',
  standalone: true,
  imports: [PlaceholderComponent],
  template: `<app-placeholder title="Ciclo Activo" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ActiveCycleComponent {}
