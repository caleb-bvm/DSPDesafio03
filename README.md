# Sistema de préstamo de equipos de laboratorio

Solución de ASP.NET Core 10 con tres proyectos y una base de datos SQL Server:

| Proyecto | Función | URL local |
| --- | --- | --- |
| `EquiposAPI` | Reglas, seguridad JWT y escritura de datos | `http://localhost:5101` |
| `EquiposMVC` | Interfaz de operación que consume la API | `http://localhost:5102` |
| `EquiposReportes` | Consultas de solo lectura para Admin | `http://localhost:5103` |

El cliente de operación nunca accede directamente a SQL Server. Reportes lee
con el usuario SQL `reportes`, limitado a `db_datareader`. Toda escritura de la
aplicación pasa por la API.

## Arranque rápido con Docker (recomendado)

Solo necesita **Docker Desktop** abierto. Desde la carpeta de la solución:

```powershell
.\iniciar.ps1
```

El script hace todo:

1. La primera vez crea el archivo `.env` con contraseñas aleatorias para `sa`,
   `reportes` y `admin`, y con la clave JWT. Ese archivo no se sube a Git.
2. Levanta SQL Server, la API, el MVC y Reportes en contenedores.
3. La API crea la base y el usuario `admin`. Luego el servicio `db-init`
   crea el login de solo lectura `reportes`.
4. Muestra las URL y la contraseña de `admin`.

| Qué | Dónde |
| --- | --- |
| MVC | `http://localhost:5102` |
| Reportes | `http://localhost:5103` |
| Swagger | `http://localhost:5101/swagger` |
| SSMS | Servidor `localhost,14330`, autenticación SQL Server, usuario `sa` (la contraseña está en `.env`) |

Comandos útiles:

```powershell
docker compose stop        # Detiene todo y conserva los datos.
docker compose down -v     # Borra todo, incluida la base, para empezar de cero.
docker compose logs api    # Muestra los mensajes de la API.
```

Con Docker no hace falta instalar SQL Server, cambiar el modo de autenticación
ni configurar User Secrets. Las secciones siguientes describen el arranque sin
Docker.

## Requisitos (sin Docker)

- SDK de .NET 10.
- SQL Server Express (`.\SQLEXPRESS`) y SSMS para revisar el esquema.
- Postman para importar `postman/EquiposLaboratorio.postman_collection.json`.
- Para las migraciones y el scaffold, herramienta `dotnet-ef` de versión 10.

Los tres proyectos deben usar la misma versión de .NET. En otro equipo, cambie
el nombre de la instancia SQL en las cadenas de conexión locales.

## Configuración inicial (sin Docker)

1. Configure SQL Server en modo de autenticación **SQL Server y Windows** y
   reinicie la instancia. Es necesario para el login SQL `reportes`. La API
   puede seguir usando autenticación de Windows.
2. Desde la carpeta de la solución, configure los secretos de la API con sus
   propios valores:

   ```powershell
   dotnet user-secrets set "ConnectionStrings:EquiposDb" "<cadena-Windows-para-EquiposDb>" --project EquiposAPI
   dotnet user-secrets set "Jwt:Key" "<clave-aleatoria-de-al-menos-32-caracteres>" --project EquiposAPI
   dotnet user-secrets set "SeedAdmin:Password" "<contraseña-inicial-de-al-menos-8-caracteres>" --project EquiposAPI
   ```

3. Instale `dotnet-ef` si no está disponible y aplique las migraciones:

   ```powershell
   dotnet tool install --global dotnet-ef --version 10.0.13
   dotnet ef database update --project EquiposAPI
   ```

   La API también aplica las migraciones pendientes al arrancar. Si la tabla
   `Usuarios` está vacía, crea el usuario `admin` con la contraseña del secreto.
4. Cree el login SQL `reportes` y su usuario de la base mediante
   `EquiposReportes/sql/crear_usuario_reportes.sql`. En SSMS abra el script,
   active **Consulta > Modo SQLCMD** y agregue temporalmente esta línea al
   principio de la pestaña antes de ejecutarlo:

   ```sql
   :setvar ReportesPassword "contraseña-local-fuerte"
   ```

   Sustituya el ejemplo por su propia contraseña. Cierre la pestaña sin
   guardar el cambio; no guarde contraseñas en Git. El login solo debe
   pertenecer a `db_datareader`.
5. Configure la conexión de Reportes en User Secrets:

   ```powershell
   dotnet user-secrets set "ConnectionStrings:EquiposReportes" "<cadena-SQL-de-reportes-para-EquiposDb>" --project EquiposReportes
   ```

   La cadena de la API usa autenticación integrada de Windows; la de Reportes
   usa `User Id=reportes` y la contraseña creada en el paso anterior. Ambas
   apuntan a la base `EquiposDb` de la instancia local.

Las URL de la API se encuentran en `appsettings.json` de los clientes porque no
son secretos. La cadena de conexión, la clave JWT y las contraseñas no se
guardan en archivos versionados.

## Arranque (sin Docker)

Abra tres terminales en la carpeta de esta solución, en este orden:

```powershell
dotnet run --project EquiposAPI --urls http://localhost:5101
dotnet run --project EquiposMVC --urls http://localhost:5102
dotnet run --project EquiposReportes --urls http://localhost:5103
```

Entre en `http://localhost:5102` con `admin` y la contraseña configurada. El
Admin puede abrir Reportes desde el menú; Reportes pide iniciar sesión de nuevo
contra la API. Swagger de la API está disponible en
`http://localhost:5101/swagger` cuando el entorno es Development.

La contraseña inicial del Admin es la que se configuró en el paso 2 de la
configuración inicial. Para consultarla localmente sin incluirla en Git:

```powershell
dotnet user-secrets list --project EquiposAPI | Select-String 'SeedAdmin:Password'
```

Cambiar ese secreto después de crear el Admin no cambia el hash de la cuenta
existente: el seeder solo actúa cuando `Usuarios` está vacía.

## Roles y flujo principal

- **Admin:** mantiene categorías, equipos y usuarios; registra préstamos y
  devoluciones; consulta todos los préstamos y los reportes.
- **Docente:** registra préstamos y devoluciones, y ve los préstamos.
- **Estudiante:** ve equipos disponibles y solo sus propios préstamos.

Los cuatro reportes aceptan filtros de categoría y fechas. En **Inventario**,
las fechas seleccionan equipos según su fecha de registro; las cantidades
reflejan el estado actual de esos equipos. En **Vencidos** se filtra la fecha
de vencimiento y en **Top** e **Historial**, la fecha del préstamo.

Al prestar un equipo, la API registra el préstamo y cambia el equipo a
`Prestado` en una sola operación de guardado. Si ya no está disponible, devuelve
409. La edición de equipos envía `rowVersion`; una versión antigua produce 409
para evitar sobrescribir cambios ajenos.

## Evidencia y archivos entregados

- `sql/EquiposDb.sql`: script generado a partir de las migraciones.
- `sql/VerificarReportes.sql`: consultas de referencia para comparar reportes
  con SSMS.
- `postman/EquiposLaboratorio.postman_collection.json`: colección para los casos
  de la API. Complete las contraseñas como variables locales de Postman.
- `docs/PlanDePruebas.md`: los 12 casos de la defensa y sus resultados.
- `docs/Contratos.md`: rutas y datos intercambiados entre proyectos.

Los casos 11 (expiración en MVC) y 12 (comparación de reportes con SSMS) se
verifican desde las aplicaciones y SSMS, además de documentarse en el plan.
