export class Session {
  Id: number = 0;
  Device: string = '';
  LoginFrom: string = '';
  CreatedAt: string = '';
  ExpiresAt: string = '';
}

/**
 * True si la sesión es de las que el usuario pidió mantener 30 días. No hay un
 * campo para eso: el backend emite el refresh token web con ~12 h de vida o con
 * 30 días, y la rotación conserva esa vida, así que basta comparar las fechas.
 * Cualquier valor por encima de un día es "larga".
 */
export const isLongSession = (session: Pick<Session, 'CreatedAt' | 'ExpiresAt'>): boolean => {
  const created = new Date(session.CreatedAt).getTime();
  const expires = new Date(session.ExpiresAt).getTime();
  if (Number.isNaN(created) || Number.isNaN(expires)) return false;
  return expires - created > 24 * 60 * 60 * 1000;
};

export class ConnectedUser extends Session {
  Uuid: string = '';
  FullName: string = '';
  Email: string = '';
}
