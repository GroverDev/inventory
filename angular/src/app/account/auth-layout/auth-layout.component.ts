import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

/** Marco de las pantallas de acceso (login, TOTP): fondo, logo y pie del template. */
@Component({
  selector: 'app-auth-layout',
  imports: [RouterOutlet],
  template: `
    <div class="auth-page-wrapper pt-5">
      <div class="auth-one-bg-position auth-one-bg" id="auth-particles">
        <div class="bg-overlay"></div>
        <div class="shape">
          <svg xmlns="http://www.w3.org/2000/svg" version="1.1" viewBox="0 0 1440 120">
            <path d="M 0,36 C 144,53.6 432,123.2 720,124 C 1008,124.8 1296,56.8 1440,40L1440 140L0 140z"></path>
          </svg>
        </div>
      </div>

      <div class="auth-page-content">
        <div class="container">
          <div class="row">
            <div class="col-lg-12">
              <div class="text-center mt-sm-5 mb-4 text-white-50">
                <a href="javascript:void(0);" class="d-inline-block auth-logo">
                  <img src="assets/images/logo-light.png" alt="Punto de Venta" height="20">
                </a>
                <p class="mt-3 fs-15 fw-medium">Tus ventas e inventario, siempre bajo control</p>
              </div>
            </div>
          </div>

          <div class="row justify-content-center">
            <div class="col-md-8 col-lg-6 col-xl-5">
              <router-outlet />
            </div>
          </div>
        </div>
      </div>

      <footer class="footer">
        <div class="container">
          <div class="row">
            <div class="col-lg-12">
              <div class="text-center">
                <p class="mb-0 text-muted">&copy; {{ year }} Punto de Venta</p>
              </div>
            </div>
          </div>
        </div>
      </footer>
    </div>
  `,
})
export class AuthLayoutComponent {
  readonly year = new Date().getFullYear();
}
