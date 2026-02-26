import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PlaceholderComponent } from '../../../../shared/components/placeholder/placeholder.component';

@Component({
  selector: 'app-absences-history',
  standalone: true,
  imports: [PlaceholderComponent],
  template: `<app-placeholder title="Historial de Ausencias" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbsencesHistoryComponent {}
