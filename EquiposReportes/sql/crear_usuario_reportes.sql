-- Requiere que SQL Server tenga habilitada la autenticación mixta.
-- En SSMS, activar Modo SQLCMD y proporcionar la variable ReportesPassword.
-- No guardar la contraseña en este archivo ni en el repositorio.

USE master;
GO

IF SUSER_ID(N'reportes') IS NULL
BEGIN
    DECLARE @crearLogin nvarchar(max) =
        N'CREATE LOGIN [reportes] WITH PASSWORD = '
        + QUOTENAME(N'$(ReportesPassword)', '''')
        + N', CHECK_POLICY = ON';
    EXEC (@crearLogin);
END;
GO

USE EquiposDb;
GO

IF USER_ID(N'reportes') IS NULL
    CREATE USER [reportes] FOR LOGIN [reportes];
GO

IF IS_ROLEMEMBER(N'db_datareader', N'reportes') <> 1
    ALTER ROLE [db_datareader] ADD MEMBER [reportes];
GO
