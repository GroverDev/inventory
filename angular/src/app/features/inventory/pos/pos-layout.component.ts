import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Store } from '@ngrx/store';

import { RootReducerState } from '../../../store';

/**
 * Marco a pantalla completa del punto de venta (sin menú lateral ni cabecera, como
 * PointOfSaleLayout del Vue). Aplica los atributos de tema del template, que de otro
 * modo solo pondría el layout principal y se perderían al recargar directo en el POS.
 */
@Component({
  selector: 'app-pos-layout',
  imports: [RouterOutlet],
  template: `<router-outlet />`,
})
export class PosLayoutComponent implements OnInit {
  private readonly store = inject(Store<RootReducerState>);

  ngOnInit() {
    this.store.select('layout').subscribe(data => {
      const html = document.documentElement;
      html.setAttribute('data-layout', data.LAYOUT);
      html.setAttribute('data-theme', data.LAYOUT_THEME);
      html.setAttribute('data-theme-colors', data.LAYOUT_THEME_COLOR);
      html.setAttribute('data-bs-theme', data.LAYOUT_MODE);
      try { localStorage.setItem('theme', data.LAYOUT_MODE); } catch { /* almacenamiento bloqueado */ }
      html.setAttribute('data-layout-width', data.LAYOUT_WIDTH);
      html.setAttribute('data-layout-position', data.LAYOUT_POSITION);
      html.setAttribute('data-topbar', data.TOPBAR);
    });
  }
}
