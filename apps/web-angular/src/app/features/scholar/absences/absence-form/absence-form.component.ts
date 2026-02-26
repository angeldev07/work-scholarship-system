import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PlaceholderComponent } from '../../../../shared/components/placeholder/placeholder.component';

@Component({
  selector: 'app-absence-form',
  standalone: true,
  imports: [PlaceholderComponent],
  template: `<app-placeholder title="Reportar Ausencia" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbsenceFormComponent {}
