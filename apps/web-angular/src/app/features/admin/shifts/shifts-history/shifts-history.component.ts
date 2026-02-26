import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PlaceholderComponent } from '../../../../shared/components/placeholder/placeholder.component';

@Component({
  selector: 'app-shifts-history',
  standalone: true,
  imports: [PlaceholderComponent],
  template: `<app-placeholder title="Historial de Jornadas" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShiftsHistoryComponent {}
