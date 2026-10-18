# Caso 12: reportes y consultas SQL

Verificado el 17 de octubre de 2026 con la base local `EquiposDb`.

- Se inició sesión como Admin contra la API y se abrió el reporte de Inventario;
  un Estudiante no pudo entrar en las páginas protegidas.
- Los cuatro reportes se ejecutaron con consultas LINQ de solo lectura y sus
  resultados coincidieron con las consultas de `sql/VerificarReportes.sql`.
- Tras agregar `FechaRegistro`, el filtro de Inventario dio 4 equipos para la
  fecha de alta, 0 para el día anterior y 3 al combinar la fecha con la
  categoría *Kits de IoT*. SQL devolvió los mismos agregados.
- Se comprobó también que el límite `hasta=9999-12-31` no causa error en
  ninguno de los cuatro reportes.

Después de esa prueba se repitieron los casos de la API, que crearon más datos
de demostración. `ReportesSql.txt` contiene la salida actualizada de SQL Server
sin filtros para comparar los cuatro reportes. En este equipo aún falta activar
la autenticación mixta y crear el login SQL `reportes` con `db_datareader`;
por ello la prueba final de permisos de esa cuenta queda pendiente.
