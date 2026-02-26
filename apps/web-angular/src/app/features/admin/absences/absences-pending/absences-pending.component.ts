import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PlaceholderComponent } from '../../../../shared/components/placeholder/placeholder.component';

@Component({
  selector: 'app-absences-pending',
  standalone: true,
  imports: [PlaceholderComponent],
  template: `<app-placeholder title="Ausencias Pendientes" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbsencesPendingComponent {}
