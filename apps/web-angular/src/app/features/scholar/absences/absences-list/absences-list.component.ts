import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PlaceholderComponent } from '../../../../shared/components/placeholder/placeholder.component';

@Component({
  selector: 'app-absences-list',
  standalone: true,
  imports: [PlaceholderComponent],
  template: `<app-placeholder title="Mis Solicitudes de Ausencia" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AbsencesListComponent {}
