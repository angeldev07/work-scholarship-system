import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PlaceholderComponent } from '../../../../shared/components/placeholder/placeholder.component';

@Component({
  selector: 'app-extra-hours-list',
  standalone: true,
  imports: [PlaceholderComponent],
  template: `<app-placeholder title="Mis Solicitudes de Adelanto de Horas" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExtraHoursListComponent {}
