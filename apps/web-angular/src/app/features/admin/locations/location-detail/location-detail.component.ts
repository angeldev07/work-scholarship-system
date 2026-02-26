import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PlaceholderComponent } from '../../../../shared/components/placeholder/placeholder.component';

@Component({
  selector: 'app-location-detail',
  standalone: true,
  imports: [PlaceholderComponent],
  template: `<app-placeholder title="Detalle de Ubicación" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LocationDetailComponent {}
