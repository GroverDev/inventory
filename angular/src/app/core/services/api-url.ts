import { environment } from '../../../environments/environment';

/** Une la URL base de la API con un endpoint, sin dobles barras. */
export const apiUrl = (endpoint: string): string =>
  `${environment.apiUrl.replace(/\/+$/, '')}/${endpoint.replace(/^\/+/, '')}`;
