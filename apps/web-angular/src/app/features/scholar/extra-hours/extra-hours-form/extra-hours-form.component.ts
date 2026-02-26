import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PlaceholderComponent } from '../../../../shared/components/placeholder/placeholder.component';

@Component({
  selector: 'app-extra-hours-form',
  standalone: true,
  imports: [PlaceholderComponent],
  template: `<app-placeholder title="Solicitar Adelanto de Horas" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExtraHoursFormComponent {}
