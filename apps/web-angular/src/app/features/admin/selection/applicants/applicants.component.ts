import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PlaceholderComponent } from '../../../../shared/components/placeholder/placeholder.component';

@Component({
  selector: 'app-applicants',
  standalone: true,
  imports: [PlaceholderComponent],
  template: `<app-placeholder title="Postulantes" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ApplicantsComponent {}
