import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PlaceholderComponent } from '../../../../shared/components/placeholder/placeholder.component';

@Component({
  selector: 'app-applicant-detail',
  standalone: true,
  imports: [PlaceholderComponent],
  template: `<app-placeholder title="Detalle de Postulante" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ApplicantDetailComponent {}
