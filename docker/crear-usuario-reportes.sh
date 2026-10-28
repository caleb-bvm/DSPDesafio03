#!/bin/bash
# Crea el login SQL "reportes" (solo db_datareader) cuando la API ya creó la base.
# Lo ejecuta el servicio "db-init" de docker-compose.yml una sola vez y termina.
set -e

SQLCMD="/opt/mssql-tools18/bin/sqlcmd -S db -U sa -P $SA_PASSWORD -C -b"

# 1. Esperar a que la API aplique las migraciones y cree el Admin
#    (la tabla Usuarios existe y tiene al menos una fila).
echo "Esperando a que la API cree la base EquiposDb..."
until [ "$($SQLCMD -h -1 -W -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM EquiposDb.dbo.Usuarios" 2>/dev/null)" -ge 1 ] 2>/dev/null; do
    sleep 3
done

# 2. Ejecutar el mismo script que se usa en SSMS, pasando la contraseña como variable.
echo "Creando el usuario reportes..."
$SQLCMD -i /scripts/crear_usuario_reportes.sql -v ReportesPassword="$REPORTES_PASSWORD"
echo "Listo."
