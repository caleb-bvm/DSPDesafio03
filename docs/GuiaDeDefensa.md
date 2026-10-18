# Guía breve para explicar el proyecto

Esta guía es un mapa de lo implementado. Antes de la defensa, recorran el código
y ejecuten los casos de `PlanDePruebas.md`; no memoricen respuestas sin haber
visto el comportamiento.

## 1. ¿Por qué hay tres proyectos?

`EquiposAPI` es la única parte que modifica los datos. `EquiposMVC` presenta
formularios y llama a la API por HTTP. `EquiposReportes` inicia sesión contra
la API para confirmar que el usuario es Admin, pero consulta SQL Server con una
cuenta que solo puede leer. Los tres usan la misma base `EquiposDb`.

```text
EquiposMVC ──HTTP + JWT──> EquiposAPI ──EF Core──> EquiposDb
EquiposReportes ──login──> EquiposAPI
EquiposReportes ──EF Core, solo lectura──> EquiposDb
```

## 2. ¿Qué significa código primero y base de datos primero aquí?

- En la API se escribieron las clases `Categoria`, `Equipo`, `Usuario` y
  `Prestamo` y las reglas del `EquiposDbContext`. La migración construye las
  tablas, índices, relaciones y semilla de categorías. Ese es el enfoque de
  **código primero**.
- En Reportes el contexto y las clases se generan después de existir la base
  mediante `Scaffold-DbContext`. Ese es el enfoque de **base de datos primero**.
  El contexto generado se registra en `Program.cs` con una conexión guardada
  en User Secrets.

La API asigna `FechaRegistro` al crear cada equipo. En el reporte de Inventario,
el rango de fechas filtra esa fecha de alta; el agrupamiento muestra el estado
actual. En los otros reportes el rango se aplica a la fecha de vencimiento o
préstamo, según el título del reporte.

## 3. ¿Por qué DTOs?

La API no expone directamente las entidades de EF. Los DTOs determinan qué
campos acepta cada operación y qué campos ve el cliente. Por ejemplo, el DTO
de usuario no incluye `PasswordHash`; el DTO de edición de equipo incluye
`rowVersion`. AutoMapper convierte entre entidades y DTOs donde corresponde.

## 4. ¿Cómo se protege el sistema?

La contraseña se guarda como hash con `PasswordHasher<Usuario>`. El login
comprueba ese hash y devuelve un JWT con ID y rol. `[Authorize]` exige token;
`[Authorize(Roles = ...)]` restringe las acciones. Para listar préstamos de un
Estudiante, la API lee su ID del token y filtra la consulta, así que cambiar
parámetros de la URL no permite ver los préstamos de otra persona.

La clave JWT, la conexión y la contraseña inicial del Admin se guardan fuera
del repositorio mediante User Secrets. Si no existe ningún usuario al iniciar,
el seeder crea el Admin con esa contraseña.

## 5. ¿Cómo se evita un préstamo duplicado?

La API comprueba que el equipo esté `Disponible`. Después agrega el préstamo y
cambia el estado a `Prestado` en el mismo `SaveChangesAsync()`: ambas acciones
se guardan juntas o ninguna se guarda. Un índice único filtrado en SQL Server
impide que haya dos préstamos activos para el mismo equipo. Si se intenta
prestar uno ocupado, la API responde 409.

## 6. ¿Cómo funciona `RowVersion`?

SQL Server cambia automáticamente el valor `rowversion` al actualizar un
equipo. Cuando se abre Edit, el cliente recibe esa versión y la devuelve al
guardar. Si otra persona modificó el equipo antes, la versión antigua ya no
coincide; la API devuelve 409 y el formulario conserva lo escrito para que el
usuario pueda revisarlo. Así no se sobrescribe silenciosamente un cambio.

## 7. ¿Cómo recibe el token el cliente MVC?

El login guarda token, rol y nombre de usuario en Session. Un
`DelegatingHandler` lo añade al encabezado de cada petición, sin cambiar los
encabezados compartidos de `HttpClient`. Ante 401 se limpia Session y se vuelve
al login; 403 muestra acceso denegado; 409 muestra el mensaje de la API dentro
del formulario; los errores 5xx muestran un mensaje general.

## 8. ¿Por qué Reportes no puede escribir?

Su cuenta de SQL Server debe pertenecer solo al rol `db_datareader`. Las consultas
usan `AsNoTracking()` y proyectan los resultados a ViewModels. La restricción
se aplica tanto en el código como en los permisos de la base. Solo el rol Admin
puede entrar, después de autenticarse en la API.

## Recorrido sugerido para la demostración

1. Mostrar las cuatro tablas, los índices únicos y `rowversion` en SSMS.
2. Iniciar sesión como Admin, crear un equipo y aplicar filtros en el listado.
3. Registrar un préstamo, intentar repetirlo y mostrar el 409.
4. Abrir Edit de un equipo en dos ventanas y mostrar el conflicto de la segunda.
5. Entrar como Estudiante y comprobar el menú y sus préstamos propios.
6. Abrir los cuatro reportes como Admin y comparar una consulta en SSMS.
7. Mostrar los secretos fuera del repositorio, la colección Postman y el
   historial de commits por fases.
