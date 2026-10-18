# Contratos entre los proyectos

Este archivo fija los nombres y las respuestas que usan la API y los clientes. Las
propiedades JSON se escriben en `camelCase`. Todas las fechas viajan en ISO 8601.

## Autenticación

| Método y ruta | Entrada | Salida |
| --- | --- | --- |
| `POST /api/auth/login` | `{ username, password }` | `{ token, username, rol, expiresAt }` |
| `POST /api/auth/register` | `{ username, email, password, rol }` | `{ id, username, email, rol }` |
| `GET /api/usuarios` | Sin cuerpo. Admin y Docente. | Lista de `{ id, username, email, rol }` para elegir prestatario. |

`rol` es `Admin`, `Docente` o `Estudiante`. El token contiene los claims de ID,
nombre de usuario y rol. El registro de usuarios es exclusivo de Admin.

## Categorías

| Método y ruta | Entrada | Salida |
| --- | --- | --- |
| `GET /api/categorias` | — | Lista de `{ id, nombre }` |
| `GET /api/categorias/{id}` | — | `{ id, nombre }` |
| `POST /api/categorias` | `{ nombre }` | Categoría creada |
| `PUT /api/categorias/{id}` | `{ nombre }` | Categoría actualizada |
| `DELETE /api/categorias/{id}` | — | 204 |

## Equipos

`estado` es `Disponible`, `Prestado` o `Mantenimiento`. `rowVersion` es una
cadena Base64. El cliente debe devolver la versión que obtuvo al abrir Edit.
La API asigna `fechaRegistro` al crear el equipo, con hora de Guatemala; los
formularios no la envían ni pueden modificarla.

| Método y ruta | Entrada | Salida |
| --- | --- | --- |
| `GET /api/equipos?texto=&categoriaId=&estado=&page=1&pageSize=10` | Filtros combinables | `{ items, page, pageSize, totalCount, totalPages }` |
| `GET /api/equipos/{id}` | — | `{ id, codigoInventario, nombre, descripcion, categoriaId, categoriaNombre, estado, fechaRegistro, rowVersion }` |
| `POST /api/equipos` | `{ codigoInventario, nombre, descripcion, categoriaId, estado }` | Equipo creado |
| `PUT /api/equipos/{id}` | `{ codigoInventario, nombre, descripcion, categoriaId, estado, rowVersion }` | Equipo actualizado |
| `DELETE /api/equipos/{id}` | — | 204 |

Cada elemento del listado usa las mismas propiedades del detalle. El Estudiante
solo recibe equipos disponibles aunque intente otro filtro.

## Préstamos

| Método y ruta | Entrada | Salida |
| --- | --- | --- |
| `GET /api/prestamos?soloVencidos=true` | Filtro opcional | Lista de préstamos según el rol del token |
| `POST /api/prestamos` | `{ equipoId, usuarioId, fechaVencimiento, observaciones }` | Préstamo creado |
| `PUT /api/prestamos/{id}/devolucion` | — | Préstamo devuelto |

La lista contiene `{ id, equipoId, codigoInventario, equipoNombre, usuarioId,
username, fechaPrestamo, fechaVencimiento, fechaDevolucion, observaciones }`.
Admin y Docente ven todos; Estudiante solo los propios. El parámetro `usuarioId`
no existe en GET: la API obtiene el ID del token.

## Errores

Las respuestas de error incluyen `{ message }`, con un texto apto para mostrar
al usuario. 401 exige nuevo inicio de sesión; 403 indica acceso denegado; 409
se usa para reglas de negocio, duplicados y conflictos de concurrencia.
