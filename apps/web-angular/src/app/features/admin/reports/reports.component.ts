import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PlaceholderComponent } from '../../../shared/components/placeholder/placeholder.component';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [PlaceholderComponent],
  template: `<app-placeholder title="Reportes" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportsComponent {}
