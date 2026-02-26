import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PlaceholderComponent } from '../../../shared/components/placeholder/placeholder.component';

@Component({
  selector: 'app-shift',
  standalone: true,
  imports: [PlaceholderComponent],
  template: `<app-placeholder title="Mi Jornada" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ShiftComponent {}
