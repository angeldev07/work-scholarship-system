import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PlaceholderComponent } from '../../../shared/components/placeholder/placeholder.component';

@Component({
  selector: 'app-audit',
  standalone: true,
  imports: [PlaceholderComponent],
  template: `<app-placeholder title="Auditoría" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuditComponent {}
