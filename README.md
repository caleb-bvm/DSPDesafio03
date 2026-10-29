# Sistema de préstamo de equipos de laboratorio

Solución de ASP.NET Core 10 con tres proyectos que comparten la base de datos
SQL Server `EquiposDb`:

| Proyecto | Qué hace | URL local |
| --- | --- | --- |
| `EquiposAPI` | Aplica las reglas de negocio, emite los tokens JWT y es el único que escribe en la base | `http://localhost:5101` |
| `EquiposMVC` | Interfaz de operación; todo lo hace llamando a la API | `http://localhost:5102` |
| `EquiposReportes` | Reportes de solo lectura para el rol Admin | `http://localhost:5103` |

EquiposMVC nunca se conecta a SQL Server. EquiposReportes lee la base con el
usuario SQL `reportes`, que solo pertenece a `db_datareader`.

Hay dos formas de arrancar el sistema. La opción A usa Docker y es la más
rápida. La opción B usa SQL Server Express instalado en el equipo.

## Opción A: arrancar con Docker (recomendada)

Solo necesita Docker Desktop. No hace falta instalar SQL Server, el SDK de .NET
ni configurar User Secrets.

1. Abra Docker Desktop y espere a que indique que está en ejecución.
2. Abra PowerShell en la carpeta de la solución, la que contiene
   `docker-compose.yml`.
3. Ejecute el script de arranque:

   ```powershell
   .\iniciar.ps1
   ```

   Si PowerShell responde que la ejecución de scripts está deshabilitada, use
   este comando en su lugar:

   ```powershell
   powershell -ExecutionPolicy Bypass -File .\iniciar.ps1
   ```

   La primera vez tarda varios minutos, porque descarga las imágenes de
   SQL Server y .NET y compila los tres proyectos.
4. Espere a que el script termine. Al final muestra las URL y la contraseña del
   usuario `admin`. Reportes puede tardar unos segundos más que los otros en
   responder.
5. Abra `http://localhost:5102` e inicie sesión con `admin` y esa contraseña.

El script hace lo siguiente:

- La primera vez crea el archivo `.env` con contraseñas aleatorias para `sa`,
  `reportes` y `admin`, y con la clave JWT. Git ignora ese archivo.
- Levanta SQL Server, la API, el MVC y Reportes en contenedores.
- Al arrancar, la API crea la base con las migraciones y el usuario `admin`.
  Después, el servicio `db-init` crea el login de solo lectura `reportes`.

Si después necesita la contraseña de `admin`, está en la línea
`ADMIN_PASSWORD` del archivo `.env`.

Direcciones del sistema en Docker:

| Qué | Dónde |
| --- | --- |
| MVC | `http://localhost:5102` |
| Reportes | `http://localhost:5103` |
| Swagger | `http://localhost:5101/swagger` |
| SSMS | Servidor `localhost,14330`, autenticación de SQL Server, usuario `sa`, contraseña en la línea `SA_PASSWORD` de `.env` |

Comandos útiles:

```powershell
docker compose stop        # Detiene todo y conserva los datos.
docker compose up -d       # Vuelve a arrancar lo que se detuvo.
docker compose down -v     # Borra todo, incluida la base, para empezar de cero.
docker compose logs api    # Muestra los mensajes de la API.
```

Las contraseñas de `.env` solo se aplican cuando la base se crea por primera
vez. Si borra o cambia `.env`, ejecute también `docker compose down -v` antes
de volver a ejecutar `.\iniciar.ps1`; de lo contrario, la base conserva las
contraseñas anteriores y la API no podrá conectarse.

## Opción B: arrancar sin Docker

### Requisitos

- SDK de .NET 10. Los tres proyectos usan la misma versión.
- SQL Server Express con la instancia `.\SQLEXPRESS` y SSMS.
- La herramienta `dotnet-ef` versión 10, para migraciones y scaffold.
- Postman, para la colección de pruebas.

Si la instancia de SQL Server tiene otro nombre, use ese nombre en las cadenas
de conexión de los pasos 2 y 5.

Ejecute todos los comandos desde la carpeta de la solución.

### Paso 1: activar la autenticación de SQL Server

El login `reportes` es un usuario de SQL Server, así que la instancia debe
aceptar ese tipo de inicio de sesión.

1. En SSMS, haga clic derecho sobre el servidor y elija **Propiedades**.
2. En **Seguridad**, marque **Modo de autenticación de SQL Server y de
   Windows**.
3. Reinicie la instancia.

La API puede seguir conectándose con autenticación de Windows.

### Paso 2: configurar los secretos de la API

La cadena de conexión, la clave JWT y la contraseña inicial de `admin` se
guardan en User Secrets, fuera del repositorio. Sustituya cada valor entre
`< >` por el suyo:

```powershell
dotnet user-secrets set "ConnectionStrings:EquiposDb" "Server=.\SQLEXPRESS;Database=EquiposDb;Trusted_Connection=True;TrustServerCertificate=True" --project EquiposAPI
dotnet user-secrets set "Jwt:Key" "<clave-aleatoria-de-al-menos-32-caracteres>" --project EquiposAPI
dotnet user-secrets set "SeedAdmin:Password" "<contraseña-de-al-menos-8-caracteres>" --project EquiposAPI
```

La API no arranca si falta la cadena de conexión o si la clave JWT tiene menos
de 32 caracteres.

### Paso 3: crear la base de datos

Si no tiene `dotnet-ef`, instálelo. Luego aplique las migraciones:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.13
dotnet ef database update --project EquiposAPI
```

Este paso es opcional, porque la API también aplica las migraciones pendientes
al arrancar. En cualquiera de los dos casos, si la tabla `Usuarios` está vacía,
la API crea el usuario `admin` con la contraseña del paso 2.

### Paso 4: crear el login de solo lectura `reportes`

La base `EquiposDb` debe existir antes de este paso.

1. En SSMS, abra `EquiposReportes/sql/crear_usuario_reportes.sql`.
2. Active **Consulta > Modo SQLCMD**.
3. Agregue esta línea al principio de la pestaña, con una contraseña suya:

   ```sql
   :setvar ReportesPassword "contraseña-local-fuerte"
   ```

4. Ejecute el script.
5. Cierre la pestaña sin guardar, para que la contraseña no quede en el
   archivo ni en Git.

El script crea el login, el usuario de la base y lo agrega únicamente a
`db_datareader`.

### Paso 5: configurar la conexión de Reportes

Use la contraseña que eligió en el paso 4:

```powershell
dotnet user-secrets set "ConnectionStrings:EquiposReportes" "Server=.\SQLEXPRESS;Database=EquiposDb;User Id=reportes;Password=<contraseña-del-paso-4>;TrustServerCertificate=True" --project EquiposReportes
```

Las URL de la API sí están en los `appsettings.json` de los clientes, porque no
son secretos. La cadena de conexión, la clave JWT y las contraseñas nunca se
guardan en archivos versionados.

### Paso 6: arrancar los tres proyectos

Abra tres terminales en la carpeta de la solución y ejecute un comando en cada
una, en este orden. Arranque primero la API, porque es la que crea la base y el
usuario `admin`.

```powershell
dotnet run --project EquiposAPI --urls http://localhost:5101
```

```powershell
dotnet run --project EquiposMVC --urls http://localhost:5102
```

```powershell
dotnet run --project EquiposReportes --urls http://localhost:5103
```

### Paso 7: entrar al sistema

1. Abra `http://localhost:5102` e inicie sesión con `admin` y la contraseña del
   paso 2.
2. Para ver los reportes, elija **Reportes** en el menú. Reportes vuelve a pedir
   usuario y contraseña, y los valida contra la API.
3. Swagger está en `http://localhost:5101/swagger` mientras el entorno sea
   Development.

Si olvidó la contraseña de `admin`, consúltela así:

```powershell
dotnet user-secrets list --project EquiposAPI | Select-String 'SeedAdmin:Password'
```

Cambiar ese secreto después no cambia la contraseña de `admin`, porque la API
solo crea el usuario cuando la tabla `Usuarios` está vacía.

## Cargar datos de demostración

Una base recién creada solo tiene las 4 categorías y el usuario `admin`, así
que las listas y los reportes salen vacíos. `scripts/CargarDatosDemo.ps1` carga
datos de ejemplo llamando a la API, igual que lo haría el MVC:

- 2 docentes (`mlopez`, `jhernandez`) y 4 estudiantes (`acastro`, `dmartinez`,
  `sramirez`, `kflores`).
- 15 equipos repartidos en las 4 categorías, 2 de ellos en Mantenimiento.
- 15 préstamos ya devueltos y 6 activos, para el historial y el Top 5.

Con la API encendida, ejecute el comando que corresponda a su forma de arranque:

```powershell
.\scripts\CargarDatosDemo.ps1           # Opción A (Docker)
.\scripts\CargarDatosDemo.ps1 -Local    # Opción B (sin Docker)
```

Con Docker, la contraseña de los usuarios de demostración se genera la primera
vez y queda en la línea `DEMO_PASSWORD` de `.env`. Con `-Local`, el script la
pide por teclado. Si ejecuta el script otra vez, no duplica usuarios, equipos ni
préstamos.

### Préstamos vencidos para el reporte

La API registra cada préstamo con la fecha de hoy y no acepta vencimientos en el
pasado, así que los préstamos recién cargados todavía no están vencidos. Para
mostrar el reporte de vencidos en la demostración, abra
`sql/DemoPrestamosVencidos.sql` en SSMS y ejecútelo con el usuario `sa`. El
script mueve al pasado las fechas de 3 préstamos activos (equipos `IOT-002`,
`LAP-003` y `ARD-003`) y muestra el resultado. Es un ajuste de datos de prueba
hecho por el administrador de la base; la aplicación sigue sin escribir directo
en SQL Server.

## Roles

| Rol | Qué puede hacer |
| --- | --- |
| Admin | Mantener categorías, equipos y usuarios; registrar préstamos y devoluciones; ver todos los préstamos y los reportes |
| Docente | Registrar préstamos y devoluciones, y ver todos los préstamos |
| Estudiante | Ver los equipos disponibles y solo sus propios préstamos |

Solo el Admin registra usuarios nuevos, desde **Usuarios** en el menú del MVC.

## Cómo se comportan préstamos, ediciones y reportes

Al prestar un equipo, la API guarda el préstamo y cambia el equipo a `Prestado`
en una sola operación. Si el equipo no está disponible, responde 409.

La edición de equipos envía la `rowVersion` que se leyó al abrir el formulario.
Si otra persona guardó cambios en ese tiempo, la API responde 409 y el MVC
muestra el mensaje sin borrar lo que se escribió.

Los cuatro reportes se filtran por categoría y por rango de fechas. La fecha
que se filtra cambia según el reporte:

| Reporte | Fecha que se filtra |
| --- | --- |
| Inventario por categoría | Fecha de registro del equipo; las cantidades muestran el estado actual de esos equipos |
| Préstamos vencidos | Fecha de vencimiento |
| Top 5 equipos más prestados | Fecha del préstamo |
| Historial por usuario | Fecha del préstamo |

## Pruebas

### Colección de Postman (casos 1 a 10)

Funciona con las dos opciones de arranque, porque usa `http://localhost:5101`.

1. Importe `postman/EquiposLaboratorio.postman_collection.json`.
2. En las variables de la colección, escriba en **Current value** la contraseña
   de `admin` en `adminPassword` y una contraseña nueva de al menos 8
   caracteres en `studentPassword`. No las escriba en **Initial value**, para
   que no queden en el archivo si vuelve a exportarlo.
3. Ejecute la carpeta **API · ejecución ordenada** completa y en orden, por
   ejemplo con **Run collection**.

La petición 05 crea el usuario `estudiante_prueba`. En una segunda ejecución
esa petición falla porque el usuario ya existe. Antes de repetir, cambie la
variable `studentUsername` o empiece con una base nueva.

### Script de PowerShell (casos 1 a 10)

`scripts/VerificarApi.ps1` ejecuta los mismos casos y muestra `OK` o `FALLÓ` en
cada uno. Solo sirve con la opción B, porque lee la contraseña de `admin` de
los User Secrets de la API. Necesita PowerShell 7:

```powershell
pwsh -File scripts/VerificarApi.ps1
```

### Casos 11 y 12

Se verifican a mano: el caso 11 en el MVC y el caso 12 comparando los reportes
con las consultas de `sql/VerificarReportes.sql` en SSMS. Los resultados de los
12 casos están en `docs/PlanDePruebas.md`.

## Archivos entregados

| Archivo | Contenido |
| --- | --- |
| `sql/EquiposDb.sql` | Script generado a partir de las migraciones |
| `sql/VerificarReportes.sql` | Consultas para comparar los reportes en SSMS |
| `sql/DemoPrestamosVencidos.sql` | Ajuste opcional de fechas para la demostración del reporte de vencidos |
| `scripts/CargarDatosDemo.ps1` | Carga de datos de demostración por la API |
| `postman/EquiposLaboratorio.postman_collection.json` | Colección con los casos de la API |
| `docs/PlanDePruebas.md` | Los 12 casos de la defensa con resultado esperado, obtenido y evidencia |
| `docs/evidencias/` | Salidas y notas de cada prueba |
| `docs/Contratos.md` | Rutas y datos que intercambian los proyectos |

Para volver a generar `sql/EquiposDb.sql` después de una migración nueva:

```powershell
dotnet ef migrations script --idempotent --project EquiposAPI --output sql/EquiposDb.sql
```
