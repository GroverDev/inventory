import { Component, inject } from '@angular/core';

import { IdleLogoutService } from './core/services/idle-logout.service';
import { LoadingService } from './core/services/loading.service';

@Component({
    selector: 'app-root',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.scss'],
    standalone: false
})
export class AppComponent {
  readonly loading = inject(LoadingService);

  constructor() {
    // Cierra la sesión no recordada tras un rato sin actividad.
    inject(IdleLogoutService).start();
  }
}
