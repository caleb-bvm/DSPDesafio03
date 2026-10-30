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

Hay dos formas de arrancar el sistema, y las dos usan el mismo script,
`iniciar.ps1`. La opción A usa Docker y es la más rápida. La opción B usa el
SQL Server Express del equipo y guarda los secretos en User Secrets; úsela en
un equipo sin Docker.

En las dos opciones el script deja todo en marcha: crea las contraseñas que
falten, arranca los tres proyectos, espera a que respondan y carga los datos de
demostración. Al terminar muestra las URL y las contraseñas.

Si PowerShell responde que la ejecución de scripts está deshabilitada,
anteponga `powershell -ExecutionPolicy Bypass -File` al comando, por ejemplo:

```powershell
powershell -ExecutionPolicy Bypass -File .\iniciar.ps1
```

## Opción A: con Docker

Solo necesita Docker Desktop. No hace falta instalar SQL Server ni el SDK de
.NET.

1. Abra Docker Desktop y espere a que indique que está en ejecución.
2. Abra PowerShell en la carpeta de la solución, la que contiene
   `docker-compose.yml`.
3. Ejecute:

   ```powershell
   .\iniciar.ps1
   ```

   La primera vez tarda varios minutos, porque descarga las imágenes de
   SQL Server y .NET y compila los tres proyectos.
4. Cuando el script diga `Listo`, abra `http://localhost:5102` e inicie sesión
   con `admin` y la contraseña que muestra.

La primera vez, el script crea el archivo `.env` con contraseñas aleatorias para
`sa`, `reportes`, `admin` y los usuarios de demostración, y con la clave JWT.
Git ignora ese archivo. Si después necesita alguna contraseña, búsquela ahí.

| Qué | Dónde |
| --- | --- |
| MVC | `http://localhost:5102` |
| Reportes | `http://localhost:5103` |
| Swagger | `http://localhost:5101/swagger` |
| SSMS | Servidor `localhost,14330`, autenticación de SQL Server, usuario `sa`, contraseña en la línea `SA_PASSWORD` de `.env` |

Comandos útiles:

```powershell
docker compose stop                # Detiene todo y conserva los datos.
.\iniciar.ps1                      # Vuelve a arrancar; no duplica los datos.
.\iniciar.ps1 -ReiniciarBase       # Borra la base y empieza de cero.
docker compose logs api            # Muestra los mensajes de la API.
```

Las contraseñas de `.env` solo se aplican cuando la base se crea. Si borra o
cambia `.env`, arranque con `.\iniciar.ps1 -ReiniciarBase`; de lo contrario, la
base conserva las contraseñas anteriores y la API no podrá conectarse.

## Opción B: sin Docker

### Requisitos

- SDK de .NET 10. Los tres proyectos usan la misma versión.
- SQL Server Express con la instancia `.\SQLEXPRESS`, y SSMS.
- `sqlcmd`, que se instala con SQL Server. Para comprobarlo, ejecute
  `sqlcmd -?`.

### Paso 1: activar la autenticación de SQL Server (una sola vez por equipo)

El login `reportes` es un usuario de SQL Server, así que la instancia debe
aceptar ese tipo de inicio de sesión. Si no lo acepta, el script se detiene y
muestra estas mismas instrucciones.

1. En SSMS, haga clic derecho sobre el servidor y elija **Propiedades**.
2. En **Seguridad**, marque **Modo de autenticación de SQL Server y de
   Windows** y acepte.
3. Haga clic derecho sobre el servidor y elija **Reiniciar**.

La API sigue conectándose con autenticación de Windows.

### Paso 2: arrancar

Abra PowerShell en la carpeta de la solución y ejecute:

```powershell
.\iniciar.ps1 -Local
```

Si la instancia tiene otro nombre, indíquelo con `-Instancia`, por ejemplo
`.\iniciar.ps1 -Local -Instancia "EQUIPO\SQLEXPRESS"`.

El script hace lo siguiente, en este orden:

1. Revisa que estén el SDK, `sqlcmd`, la instancia y la autenticación de SQL
   Server, y que los puertos 5101 a 5103 estén libres.
2. Crea en User Secrets los secretos que falten. Los que ya existen no se
   modifican.
3. Compila la solución y abre una ventana de PowerShell con la API. Al arrancar,
   la API crea la base con las migraciones y el usuario `admin`.
4. Crea el login `reportes` con `EquiposReportes/sql/crear_usuario_reportes.sql`,
   solo con el rol `db_datareader`.
5. Abre una ventana para el MVC y otra para Reportes.
6. Carga los datos de demostración y muestra las URL y las contraseñas.

Para detener el sistema, cierre las tres ventanas de PowerShell. Para empezar
con una base vacía, use `.\iniciar.ps1 -Local -ReiniciarBase`.

Los secretos que usa el sistema son estos:

| Proyecto | Secreto | Contenido |
| --- | --- | --- |
| EquiposAPI | `ConnectionStrings:EquiposDb` | Conexión a `EquiposDb` con autenticación de Windows |
| EquiposAPI | `Jwt:Key` | Clave del JWT, de al menos 32 caracteres |
| EquiposAPI | `SeedAdmin:Password` | Contraseña con la que se crea el usuario `admin` |
| EquiposAPI | `SeedDemo:Password` | Contraseña de los usuarios de demostración; solo la usa `scripts/CargarDatosDemo.ps1` |
| EquiposReportes | `ConnectionStrings:EquiposReportes` | Conexión a `EquiposDb` con el usuario `reportes` |

Para verlos:

```powershell
dotnet user-secrets list --project EquiposAPI
dotnet user-secrets list --project EquiposReportes
```

La API solo crea el usuario `admin` cuando la tabla `Usuarios` está vacía. Si
cambia `SeedAdmin:Password` en una base que ya tiene usuarios, la contraseña de
`admin` no cambia; el script lo detecta al cargar los datos y le indica usar
`-ReiniciarBase`.

Las URL de la API están en los `appsettings.json` de los clientes porque no son
secretos. Las cadenas de conexión, la clave JWT y las contraseñas nunca se
guardan en archivos versionados.

## Datos de demostración

Una base nueva solo tiene las 4 categorías y el usuario `admin`. Por eso
`iniciar.ps1` ejecuta `scripts/CargarDatosDemo.ps1`, que carga estos datos
llamando a la API, igual que lo haría el MVC:

- 2 docentes (`mlopez`, `jhernandez`) y 4 estudiantes (`acastro`, `dmartinez`,
  `sramirez`, `kflores`), todos con la misma contraseña de demostración.
- 15 equipos repartidos en las 4 categorías, 2 de ellos en Mantenimiento.
- 15 préstamos ya devueltos y 6 activos, para el historial y el Top 5.

El script no duplica nada si se ejecuta otra vez. Para arrancar sin estos datos,
agregue `-SinDatos` a `iniciar.ps1`.

### Préstamos vencidos para el reporte

La API registra cada préstamo con la fecha de hoy y no acepta vencimientos en el
pasado, así que los préstamos recién cargados todavía no están vencidos. Para
mostrar el reporte de vencidos en la demostración, abra
`sql/DemoPrestamosVencidos.sql` en SSMS y ejecútelo con `sa` (opción A) o con
su usuario de Windows (opción B). El script mueve al pasado las fechas de 3
préstamos activos (equipos `IOT-002`, `LAP-003` y `ARD-003`) y muestra el
resultado. Es un ajuste de datos de prueba hecho por el administrador de la
base; la aplicación sigue sin escribir directo en SQL Server.

## Roles

| Rol | Qué puede hacer |
| --- | --- |
| Admin | Mantener categorías, equipos y usuarios; registrar préstamos y devoluciones; ver todos los préstamos y los reportes |
| Docente | Registrar préstamos y devoluciones, y ver todos los préstamos |
| Estudiante | Ver los equipos disponibles y solo sus propios préstamos |

Solo el Admin ve la lista de usuarios, desde **Usuarios** en el menú del MVC, y
desde ahí registra usuarios nuevos con el botón **Registrar usuario**. El
Docente ve la lista de estudiantes en **Alumnos**, solo para consulta: no puede
registrar ni modificar cuentas.
Para ver los reportes, el Admin elige **Reportes** en el menú; Reportes vuelve a
pedir usuario y contraseña y los valida contra la API.

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

La petición 05 crea un usuario nuevo en cada ejecución, con el nombre
`estudiante_prueba_` más un número, así que la carpeta se puede repetir sin
cambiar variables. Cada ejecución deja en la base ese usuario, dos equipos y un
préstamo, y esos datos aparecen en los reportes.

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

Para volver a generar `sql/EquiposDb.sql` después de una migración nueva
(necesita la herramienta `dotnet-ef`):

```powershell
dotnet tool install --global dotnet-ef --version 10.0.13
dotnet ef migrations script --idempotent --project EquiposAPI --output sql/EquiposDb.sql
```
