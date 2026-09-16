# Configuración de seguridad

No guardes valores reales en Git. Configurá estas variables en el servicio de la API:

- `ConnectionStrings__DefaultConnection`: conexión PostgreSQL rotada.
- `Security__SigningKey`: secreto aleatorio nuevo de al menos 32 caracteres.
- `Security__AdminUsername`: usuario administrativo.
- `Security__AdminPasswordHash`: salida de `python3 scripts/generate_password_hash.py`.
- `Security__AllowedOrigins`: orígenes web permitidos, separados por coma y sin rutas.
- `Security__Issuer` y `Security__Audience`: opcionales; deben permanecer estables.
- `Security__AccessTokenMinutes`: opcional, entre 5 y 480.
- `Database__ApplyMigrations`: mantener en `false`; habilitar sólo durante una migración aprobada y controlada.

En el servicio de ClickYa.Comercios configurá:

- `Api__BaseUrl`: URL HTTPS de ClickYa.Api.

Después de desplegar ambos servicios, invalidá sesiones previas cambiando `Security__SigningKey` y verificá los tres roles antes de habilitar tráfico.
