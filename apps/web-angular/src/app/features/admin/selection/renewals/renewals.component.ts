import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PlaceholderComponent } from '../../../../shared/components/placeholder/placeholder.component';

@Component({
  selector: 'app-renewals',
  standalone: true,
  imports: [PlaceholderComponent],
  template: `<app-placeholder title="Renovaciones" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RenewalsComponent {}
