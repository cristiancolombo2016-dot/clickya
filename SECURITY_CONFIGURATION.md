# Configuración de seguridad

No guardes valores reales en Git. Configurá estas variables en el servicio de la API:

- `ConnectionStrings__DefaultConnection`: conexión PostgreSQL rotada.
- `Security__SigningKey`: secreto aleatorio nuevo de al menos 32 caracteres.
- `Security__SensitiveDataKey`: clave aleatoria de exactamente 32 bytes codificada en Base64. Cifra el contacto y la dirección de las urgencias; debe conservarse estable y respaldarse de forma segura para poder descifrar solicitudes existentes.
- `Security__AdminUsername`: usuario administrativo.
- `Security__AdminPasswordHash`: salida de `python3 scripts/generate_password_hash.py`.
- `Security__AllowedOrigins`: orígenes web permitidos, separados por coma y sin rutas.
- `Security__Issuer` y `Security__Audience`: opcionales; deben permanecer estables.
- `Security__AccessTokenMinutes`: opcional, entre 5 y 480.
- `Security__AllowLegacyTokens`: mantener en `false`; sólo existe para una transición controlada y volver a deshabilitar inmediatamente.
- `Database__ApplyMigrations`: mantener en `false`; habilitar sólo durante una migración aprobada y controlada.

En el servicio de ClickYa.Comercios configurá:

- `Api__BaseUrl`: URL HTTPS de ClickYa.Api.

Después de desplegar ambos servicios, invalidá sesiones previas cambiando `Security__SigningKey` y verificá los tres roles antes de habilitar tráfico.

Generá `Security__SensitiveDataKey` fuera del repositorio con un generador criptográfico seguro (32 bytes y salida Base64). No reutilices `Security__SigningKey`. Si se pierde o cambia sin una rotación de datos planificada, las direcciones y contactos ya cifrados no podrán recuperarse.

Las contraseñas antiguas en texto plano se rechazan deliberadamente. Para cada comercio existente, acordá una contraseña nueva, generá su hash con el script y actualizá únicamente `Solicitudes.Password` mediante una operación controlada con respaldo. No reutilices la contraseña que estuvo expuesta ni habilites temporalmente comparaciones en texto plano.

## Activación de Servicios y Urgencias

La migración aditiva preparada es `20260916000100_CompletarServiciosUrgencias`. Antes de producción:

1. Creá un respaldo verificable de PostgreSQL y probá la migración en una copia/staging.
2. Configurá y respaldá `Security__SensitiveDataKey` antes de aceptar la primera urgencia.
3. Aplicá la migración mediante un trabajo manual y controlado; mantené `Database__ApplyMigrations=false` en la ejecución normal de Railway.
4. Verificá categorías de la sección `servicios`: los técnicos cuyo texto histórico de rubro coincida se vinculan automáticamente; los no coincidentes quedan sin `CategoriaId` y deben reasignarse manualmente.
5. Coordiná la publicación de API, ClickYa.Comercios y la nueva versión MAUI. El flujo antiguo de urgencias queda deliberadamente obsoleto y recibe HTTP 410/415; no habilites el nuevo backend para clientes antiguos sin un plan de actualización.
6. Probá con cuentas Admin y Técnico, un técnico común, uno Premium vigente y uno vencido antes de habilitar tráfico.
