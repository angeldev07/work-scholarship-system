import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Toast } from 'primeng/toast';
import { ConfirmDialog } from 'primeng/confirmdialog';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, Toast, ConfirmDialog],
  template: `
    <router-outlet />
    <p-toast position="top-right" />
    <p-confirmDialog />
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {}
