-- Consultas de referencia para comparar los cuatro reportes en SSMS.
-- Cambie los filtros cuando quiera repetir la comprobación.
USE EquiposDb;
GO

DECLARE @Desde date = NULL;
DECLARE @Hasta date = NULL;
DECLARE @CategoriaId int = NULL;
DECLARE @Username nvarchar(100) = NULL;
DECLARE @Hoy date = CONVERT(date,
    SYSUTCDATETIME() AT TIME ZONE 'UTC' AT TIME ZONE 'Central America Standard Time');

-- 1. Equipos registrados en el período, por categoría y estado actual.
SELECT c.Nombre AS Categoria,
       SUM(CASE WHEN e.Estado = 'Disponible' THEN 1 ELSE 0 END) AS Disponibles,
       SUM(CASE WHEN e.Estado = 'Prestado' THEN 1 ELSE 0 END) AS Prestados,
       SUM(CASE WHEN e.Estado = 'Mantenimiento' THEN 1 ELSE 0 END) AS EnMantenimiento
FROM Categorias AS c
LEFT JOIN Equipos AS e ON e.CategoriaId = c.Id
    AND (@Desde IS NULL OR e.FechaRegistro >= @Desde)
    AND (@Hasta IS NULL OR CONVERT(date, e.FechaRegistro) <= @Hasta)
WHERE @CategoriaId IS NULL OR c.Id = @CategoriaId
GROUP BY c.Id, c.Nombre
ORDER BY c.Nombre;

-- 2. Préstamos vencidos.
SELECT u.Username, e.CodigoInventario, e.Nombre AS Equipo,
       p.FechaPrestamo, p.FechaVencimiento,
       DATEDIFF(day, p.FechaVencimiento, @Hoy) AS DiasAtraso
FROM Prestamos AS p
JOIN Usuarios AS u ON u.Id = p.UsuarioId
JOIN Equipos AS e ON e.Id = p.EquipoId
WHERE p.FechaDevolucion IS NULL AND p.FechaVencimiento < @Hoy
  AND (@Desde IS NULL OR p.FechaVencimiento >= @Desde)
  AND (@Hasta IS NULL OR CONVERT(date, p.FechaVencimiento) <= @Hasta)
  AND (@CategoriaId IS NULL OR e.CategoriaId = @CategoriaId)
ORDER BY p.FechaVencimiento;

-- 3. Cinco equipos más prestados.
SELECT TOP (5) e.CodigoInventario, e.Nombre AS Equipo,
       c.Nombre AS Categoria, COUNT(*) AS CantidadPrestamos
FROM Prestamos AS p
JOIN Equipos AS e ON e.Id = p.EquipoId
JOIN Categorias AS c ON c.Id = e.CategoriaId
WHERE (@Desde IS NULL OR p.FechaPrestamo >= @Desde)
  AND (@Hasta IS NULL OR CONVERT(date, p.FechaPrestamo) <= @Hasta)
  AND (@CategoriaId IS NULL OR e.CategoriaId = @CategoriaId)
GROUP BY e.Id, e.CodigoInventario, e.Nombre, c.Nombre
ORDER BY CantidadPrestamos DESC, e.CodigoInventario;

-- 4. Historial por usuario.
SELECT u.Username, e.CodigoInventario, e.Nombre AS Equipo,
       c.Nombre AS Categoria, p.FechaPrestamo,
       p.FechaVencimiento, p.FechaDevolucion
FROM Prestamos AS p
JOIN Usuarios AS u ON u.Id = p.UsuarioId
JOIN Equipos AS e ON e.Id = p.EquipoId
JOIN Categorias AS c ON c.Id = e.CategoriaId
WHERE (@Username IS NULL OR u.Username LIKE '%' + @Username + '%')
  AND (@Desde IS NULL OR p.FechaPrestamo >= @Desde)
  AND (@Hasta IS NULL OR CONVERT(date, p.FechaPrestamo) <= @Hasta)
  AND (@CategoriaId IS NULL OR e.CategoriaId = @CategoriaId)
ORDER BY p.FechaPrestamo DESC;
