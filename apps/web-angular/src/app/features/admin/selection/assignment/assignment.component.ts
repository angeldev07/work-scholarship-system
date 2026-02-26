import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PlaceholderComponent } from '../../../../shared/components/placeholder/placeholder.component';

@Component({
  selector: 'app-assignment',
  standalone: true,
  imports: [PlaceholderComponent],
  template: `<app-placeholder title="Asignación" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AssignmentComponent {}
