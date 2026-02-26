import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PlaceholderComponent } from '../../../../shared/components/placeholder/placeholder.component';

@Component({
  selector: 'app-location-form',
  standalone: true,
  imports: [PlaceholderComponent],
  template: `<app-placeholder title="Formulario de Ubicación" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LocationFormComponent {}
