-- Índice de apoyo para el freno de fuerza bruta del login.
--
-- AuthenticationRepository.RecentFailedAttempts() consulta sec.users_login
-- filtrando por login_value y date en cada intento de login. Esa tabla crece
-- con cada acceso, así que sin índice la consulta degrada a scan secuencial.
--
-- Ejecutar una vez sobre la base punto_venta_pg.

CREATE INDEX IF NOT EXISTS ix_users_login_value_date
    ON sec.users_login (login_value, date DESC);
