import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PlaceholderComponent } from '../../../shared/components/placeholder/placeholder.component';

@Component({
  selector: 'app-interviews',
  standalone: true,
  imports: [PlaceholderComponent],
  template: `<app-placeholder title="Entrevistas Programadas" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InterviewsComponent {}
