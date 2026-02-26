import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PlaceholderComponent } from '../../../shared/components/placeholder/placeholder.component';

@Component({
  selector: 'app-application',
  standalone: true,
  imports: [PlaceholderComponent],
  template: `<app-placeholder title="Estado de mi Postulación" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ApplicationComponent {}
