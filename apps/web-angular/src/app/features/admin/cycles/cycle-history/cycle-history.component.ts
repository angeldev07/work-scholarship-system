import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PlaceholderComponent } from '../../../../shared/components/placeholder/placeholder.component';

@Component({
  selector: 'app-cycle-history',
  standalone: true,
  imports: [PlaceholderComponent],
  template: `<app-placeholder title="Historial de Ciclos" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CycleHistoryComponent {}
