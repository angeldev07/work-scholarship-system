import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PlaceholderComponent } from '../../../../shared/components/placeholder/placeholder.component';

@Component({
  selector: 'app-shift-review',
  standalone: true,
  imports: [PlaceholderComponent],
  template: `<app-placeholder title="Revisar Jornada" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShiftReviewComponent {}
