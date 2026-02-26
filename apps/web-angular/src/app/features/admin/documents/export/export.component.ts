import { ChangeDetectionStrategy, Component } from '@angular/core';
import { PlaceholderComponent } from '../../../../shared/components/placeholder/placeholder.component';

@Component({
  selector: 'app-export',
  standalone: true,
  imports: [PlaceholderComponent],
  template: `<app-placeholder title="Exportar Datos" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExportComponent {}
