-- Opcional, solo para preparar la demostración del reporte "Préstamos vencidos".
--
-- La API siempre registra el préstamo con la fecha de hoy y no acepta un
-- vencimiento en el pasado, así que un préstamo recién creado nunca está vencido.
-- Este script mueve al pasado las fechas de tres préstamos activos que crea
-- scripts/CargarDatosDemo.ps1 (equipos IOT-002, LAP-003 y ARD-003).
--
-- Es una preparación de datos de prueba que hace el administrador de la base:
-- ejecútelo en SSMS con el usuario sa (el usuario reportes no puede escribir).
-- La aplicación sigue sin escribir directo en la base.
-- Se puede ejecutar más de una vez: un préstamo que ya se movió no se vuelve a mover.

USE EquiposDb;
GO

-- Necesario por el índice filtrado de Prestamos. SSMS ya lo activa; sqlcmd no.
SET QUOTED_IDENTIFIER ON;
GO

UPDATE p
SET p.FechaPrestamo    = DATEADD(day, -m.Dias, p.FechaPrestamo),
    p.FechaVencimiento = DATEADD(day, -m.Dias, p.FechaVencimiento)
FROM Prestamos p
JOIN Equipos e ON e.Id = p.EquipoId
JOIN (VALUES ('IOT-002', 4),    -- Vencía mañana: queda con 3 días de atraso.
             ('LAP-003', 7),    -- Vencía en 2 días: queda con 5 días de atraso.
             ('ARD-003', 2))    -- Vencía mañana: queda con 1 día de atraso.
     AS m (Codigo, Dias) ON m.Codigo = e.CodigoInventario
WHERE p.FechaDevolucion IS NULL
  AND p.FechaVencimiento >= CAST(GETDATE() AS date);
GO

-- Resultado: los préstamos vencidos y sus días de atraso.
SELECT e.CodigoInventario, u.Username, p.FechaPrestamo, p.FechaVencimiento,
       DATEDIFF(day, p.FechaVencimiento, CAST(GETDATE() AS date)) AS DiasAtraso
FROM Prestamos p
JOIN Equipos e ON e.Id = p.EquipoId
JOIN Usuarios u ON u.Id = p.UsuarioId
WHERE p.FechaDevolucion IS NULL
  AND p.FechaVencimiento < CAST(GETDATE() AS date)
ORDER BY DiasAtraso DESC;
GO
