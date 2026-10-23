import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { AccessMenu } from '../models/access-menu.model';
import { ResponseArray, ResponseObject } from '../models/api.models';
import { User } from '../models/user.model';
import { apiUrl } from './api-url';
import { ApiService } from './api.service';

/** InicioSesionDesde.ReconexionWeb en el backend. */
const LOGIN_FROM_RECONEXION_WEB = 3;

const KEY_REMEMBER = 'auth_remember';
const KEY_TOKEN = 'auth_token';
const KEY_USER = 'auth_user';
const KEY_MENU = 'auth_access_menu';
const KEY_PENDING_USER = 'auth_pending_user';
const KEY_PENDING_REMEMBER = 'auth_pending_remember';

export interface LoginResult {
  success: boolean;
  requireTotp?: boolean;
  totpSetupRequired?: boolean;
  user?: User;
}

const read = <T>(storage: Storage, key: string, fallback: T): T => {
  try {
    const raw = storage.getItem(key);
    return raw === null ? fallback : (JSON.parse(raw) as T);
  } catch {
    return fallback;
  }
};

const write = (storage: Storage, key: string, value: unknown): void => {
  try {
    if (value === null || value === undefined) storage.removeItem(key);
    else storage.setItem(key, JSON.stringify(value));
  } catch { /* almacenamiento bloqueado */ }
};

/**
 * Sesión del usuario. Equivale a auth.store.ts del frontend Vue:
 * - El JWT vive en sessionStorage y nunca va a localStorage.
 * - "Mantener sesión" (`remember`) es la única marca que sobrevive al cierre
 *   del navegador; con ella, usuario y menú van a localStorage y el token se
 *   renueva en silencio con la cookie de refresh (`restoreSession`).
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(ApiService);
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  readonly remember = signal<boolean>(read(localStorage, KEY_REMEMBER, false));
  readonly token = signal<string | null>(read<string | null>(sessionStorage, KEY_TOKEN, null));
  readonly user = signal<User | null>(this.readPersisted<User | null>(KEY_USER, null));
  readonly accessMenu = signal<AccessMenu[]>(this.readPersisted<AccessMenu[]>(KEY_MENU, []));
  readonly pendingUser = signal<User | null>(read<User | null>(sessionStorage, KEY_PENDING_USER, null));
  readonly pendingRemember = signal<boolean>(read(sessionStorage, KEY_PENDING_REMEMBER, false));

  readonly isAuthenticated = computed(() => !!this.token());

  /** Refresco en curso: si varias llamadas caducan a la vez comparten uno solo. */
  private refreshing: Promise<boolean> | null = null;

  constructor() {
    // Datos de sesiones anteriores no deben quedar en equipos sin sesión recordada.
    try {
      localStorage.removeItem(KEY_TOKEN);
      if (!this.remember()) {
        localStorage.removeItem(KEY_USER);
        localStorage.removeItem(KEY_MENU);
      }
    } catch { /* almacenamiento bloqueado */ }
  }

  async login(email: string, password: string, turnstileToken = '', rememberMe = false): Promise<LoginResult> {
    const resp = await this.api.post<ResponseObject<User>>('Login', {
      UserName: '',
      Email: email,
      Password: password,
      Device: '',
      WithEmail: true,
      // InicioSesionDesde.Web
      LoginFrom: 1,
      LoginWith: 1,
      // El backend exige el captcha según la cabecera Origin, no este cuerpo.
      TurnstileToken: turnstileToken,
      // Solo la web lo usa (vida de la cookie de refresh).
      RememberMe: rememberMe,
    });

    if (!resp.ok) return { success: false };
    const newUser = resp.Data;

    // TOTP ya configurado: se verifica el código (aún no hay JWT real).
    if (newUser.RequireTotp) {
      this.setPending(newUser, rememberMe);
      return { success: false, requireTotp: true, totpSetupRequired: false };
    }

    // TOTP sin configurar: el JWT real ya vino, pero debe configurarlo ahora.
    this.setAuth(newUser.Token, newUser, rememberMe);
    if (newUser.TotpSetupRequired) {
      return { success: false, requireTotp: true, totpSetupRequired: true };
    }
    return { success: true, user: newUser };
  }

  async getAccessMenuApi(): Promise<boolean> {
    const resp = await this.api.get<ResponseArray<AccessMenu>>('AccessMenu');
    if (resp.ok) this.setAccessMenu(resp.Data);
    return resp.ok;
  }

  setAccessMenu(menu: AccessMenu[]): void {
    this.accessMenu.set(menu);
    this.persist(KEY_MENU, menu);
  }

  setAuth(newToken: string, newUser: User, rememberMe = false): void {
    // Primero la marca: decide en qué storage se escriben usuario y menú.
    this.remember.set(rememberMe);
    write(localStorage, KEY_REMEMBER, rememberMe);
    this.setToken(newToken);
    this.user.set(newUser);
    this.persist(KEY_USER, newUser);
  }

  /** Renueva solo el access token, conservando el usuario. */
  setToken(newToken: string): void {
    this.token.set(newToken);
    write(sessionStorage, KEY_TOKEN, newToken);
  }

  /**
   * Actualiza la sucursal activa y las habilitadas. Lo usan el cambio de
   * sucursal y el refresh, que puede devolver otra si al usuario le quitaron la suya.
   */
  setBranchInfo(info: Pick<User, 'BranchId' | 'BranchName' | 'Branches'>): void {
    const current = this.user();
    if (!current || !info?.BranchId) return;
    const updated = { ...current, BranchId: info.BranchId, BranchName: info.BranchName, Branches: info.Branches ?? [] };
    this.user.set(updated);
    this.persist(KEY_USER, updated);
  }

  /** El backend valida que el usuario esté habilitado y devuelve un token con esa sucursal. */
  async switchBranch(branchId: string): Promise<boolean> {
    const resp = await this.api.post<ResponseObject<User>>('Login/switch-branch', { BranchId: branchId });
    if (!resp.ok) return false;
    this.setToken(resp.Data.Token);
    this.setBranchInfo(resp.Data);
    return true;
  }

  /** Vuelve a pedir las sucursales habilitadas; renovar la sesión las trae. */
  refreshBranches(): Promise<boolean> {
    return this.tryRefreshSession();
  }

  completarTotp(newUser: User): void {
    this.setAuth(newUser.Token, newUser, this.pendingRemember());
    this.setPending(null, false);
  }

  /**
   * Al abrir la web sin token en esta pestaña: si la sesión fue recordada,
   * la renueva en silencio con la cookie de refresh. Si falla, limpia todo.
   */
  async restoreSession(): Promise<boolean> {
    if (this.token()) return true;
    if (!this.remember() || !this.user()?.Email) return false;

    if (await this.tryRefreshSession()) return true;
    this.clearSession();
    return false;
  }

  tryRefreshSession(): Promise<boolean> {
    if (!this.refreshing) {
      this.refreshing = this.doRefresh().finally(() => (this.refreshing = null));
    }
    return this.refreshing;
  }

  /**
   * Sesión irrecuperable: limpia el estado local y lleva al login. No llama a
   * `logout()` porque revocaría contra el servidor, que ya borró la cookie.
   */
  async handleSessionExpired(): Promise<void> {
    this.clearSession();
    if (!this.router.url.startsWith('/auth/login')) {
      await this.router.navigate(['/auth/login']);
    }
  }

  /** Limpia la sesión solo en el navegador, sin llamar al servidor. */
  clearSession(): void {
    this.token.set(null);
    write(sessionStorage, KEY_TOKEN, null);
    this.user.set(null);
    this.accessMenu.set([]);
    this.setPending(null, false);
    // Después de vaciar user/menú: mientras la marca siga puesta, esos valores
    // se escriben en localStorage, que es donde hay que borrarlos.
    this.remember.set(false);
    write(localStorage, KEY_REMEMBER, false);
    for (const key of [KEY_USER, KEY_MENU]) {
      write(localStorage, key, null);
      write(sessionStorage, key, null);
    }
  }

  /**
   * `keepDevice` es para el cierre automático (inactividad): revoca solo la
   * sesión y deja el equipo como de confianza. El botón "Cerrar sesión" no lo
   * usa, y ahí el servidor también olvida el equipo.
   */
  async logout(keepDevice = false): Promise<void> {
    // Revoca el refresh token en el servidor y borra la cookie.
    try {
      await this.api.post('Login/revoke', { RefreshToken: '', KeepDevice: keepDevice });
    } catch { /* sin red o sesión vencida: igual se limpia el navegador */ }
    this.clearSession();
  }

  private async doRefresh(): Promise<boolean> {
    try {
      // El cuerpo va sin token a propósito: el backend lo toma de la cookie.
      const resp = await firstValueFrom(this.http.post<ResponseObject<User>>(apiUrl('Login/refresh'), {
        RefreshToken: '',
        Device: '',
        LoginFrom: LOGIN_FROM_RECONEXION_WEB,
      }));

      if (resp?.ok && resp.Data?.Token) {
        this.setToken(resp.Data.Token);
        // Conserva la sucursal, salvo que al usuario se la hayan quitado.
        this.setBranchInfo(resp.Data);
        return true;
      }
    } catch { /* sin red o refresh inválido: sesión no recuperable */ }
    return false;
  }

  private setPending(user: User | null, remember: boolean): void {
    this.pendingUser.set(user);
    this.pendingRemember.set(remember);
    write(sessionStorage, KEY_PENDING_USER, user);
    write(sessionStorage, KEY_PENDING_REMEMBER, remember);
  }

  private persist(key: string, value: unknown): void {
    write(this.remember() ? localStorage : sessionStorage, key, value);
  }

  private readPersisted<T>(key: string, fallback: T): T {
    return read(read(localStorage, KEY_REMEMBER, false) ? localStorage : sessionStorage, key, fallback);
  }
}
