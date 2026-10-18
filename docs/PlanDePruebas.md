# Plan de pruebas de la defensa

Registrar para cada caso la petición o acción, el resultado esperado, el resultado
obtenido y una captura o respuesta exportada. No marcar un caso como aprobado
sin ejecutarlo.

| Nº | Caso | Resultado esperado | Resultado obtenido | Evidencia |
| --- | --- | --- | --- | --- |
| 1 | Login correcto | 200; JWT con claim de rol | 200; claim `role=Admin` | `scripts/VerificarApi.ps1` |
| 2 | Contraseña incorrecta | 401 | 401 | `scripts/VerificarApi.ps1` |
| 3 | Recurso protegido sin token | 401 | 401 | `scripts/VerificarApi.ps1` |
| 4 | Estudiante crea equipo | 403 | 403 | `scripts/VerificarApi.ps1` |
| 5 | Username repetido | 409 | 409 | `scripts/VerificarApi.ps1` |
| 6 | Préstamo de equipo ya prestado | 409 | 409 | `scripts/VerificarApi.ps1` |
| 7 | Vencimiento a 15 días | 400 | 400 | `scripts/VerificarApi.ps1` |
| 8 | Borrar equipo con historial | 409 | 409 | `scripts/VerificarApi.ps1` |
| 9 | Segunda edición simultánea | 409 | 409; MVC conservó el formulario | Script y prueba en MVC |
| 10 | Estudiante intenta ver préstamos ajenos | Solo recibe los propios | 200; solo registros con su ID | `scripts/VerificarApi.ps1` |
| 11 | Token vencido en MVC | Redirige al login y limpia sesión | Tras 67 s, API devolvió 401; apareció login con aviso de sesión vencida; `Session.Clear()` está en el código | `docs/evidencias/Caso11.md` |
| 12 | Cuatro reportes | Datos coinciden con SQL Server | Coinciden, incluidos filtros de fecha; falta verificar login SQL de solo lectura | `docs/evidencias/Caso12.md` y `ReportesSql.txt` |

Para el caso 9: obtener el mismo equipo dos veces, guardar la primera edición
con su `rowVersion` y enviar la segunda con la versión anterior. Para el caso
10: usar el token de Estudiante y añadir a la URL parámetros con el ID de otro
usuario; el servidor debe ignorarlos.

Resultados de la API verificados el 17 de octubre de 2026 con la base local
`EquiposDb`. El script usa nombres únicos para crear datos de prueba y puede
repetirse. La colección Postman permite presentar las peticiones durante la
defensa sin incluir contraseñas reales en el repositorio. La salida de la última
ejecución está en `docs/evidencias/ApiCasos01-10.txt`.
