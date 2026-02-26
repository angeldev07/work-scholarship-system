import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PlaceholderComponent } from '../../../../shared/components/placeholder/placeholder.component';

@Component({
  selector: 'app-scholar-detail',
  standalone: true,
  imports: [PlaceholderComponent],
  template: `<app-placeholder title="Detalle de Beca" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ScholarDetailComponent {}
