-- =====================================================================
-- Usuario centinela 'login_error' (id = 0).
--
-- El registro de intentos de login (sec.users_login) inserta user_id = 0
-- cuando el login FALLA (usuario no encontrado o contraseña incorrecta).
-- Como users_login.user_id tiene FK a sec.users(id), debe existir un
-- usuario con id = 0; de lo contrario todo login fallido revienta con:
--   23503 users_login_user_fk violation
--
-- Este usuario está INACTIVO y con password 'ninguno', por lo que nunca
-- puede iniciar sesión: solo sirve para satisfacer la FK.
--
-- Idempotente. Ejecutar en la base que usa la API (ej. punto_venta).
-- =====================================================================
INSERT INTO sec.users
    (id, user_name,    password,  email,     full_name, last_access, change_password, is_active, created_by, created, modified_by, modified, uuid)
VALUES
    (0,  'login_error','ninguno', 'ninguno', 'ninguno', now(),       false,           false,     1,          now(),   1,           now(),    gen_random_uuid())
ON CONFLICT (id) DO NOTHING;
