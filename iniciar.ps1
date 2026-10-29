# Arranca todo el sistema con un solo comando y carga los datos de demostración.
#
#     .\iniciar.ps1                  Con Docker (SQL Server, API, MVC y Reportes en contenedores).
#     .\iniciar.ps1 -Local           Sin Docker: SQL Server Express del equipo y User Secrets.
#
# Opciones:
#     -SinDatos                      No carga los datos de demostración.
#     -ReiniciarBase                 Borra la base EquiposDb y empieza de cero.
#     -Instancia "SERVIDOR\NOMBRE"   Instancia de SQL Server para -Local (por defecto .\SQLEXPRESS).
#
# Las contraseñas se generan al azar la primera vez. Con Docker se guardan en .env;
# con -Local, en User Secrets. Ninguna de las dos ubicaciones se sube a Git.
param(
    [switch]$Local,
    [switch]$SinDatos,
    [switch]$ReiniciarBase,
    [string]$Instancia = '.\SQLEXPRESS'
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

# Genera una cadena aleatoria de letras y números.
function Aleatorio([int]$largo) {
    $letras = "abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray()
    $bytes = New-Object byte[] $largo
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    -join ($bytes | ForEach-Object { $letras[$_ % $letras.Length] })
}

# Espera hasta que la URL responda. Cualquier respuesta HTTP, incluso un error,
# indica que la aplicación ya arrancó.
function EsperarUrl([string]$url, [string]$nombre, [int]$segundos = 180) {
    Write-Host "Esperando que responda $nombre..." -NoNewline
    $limite = (Get-Date).AddSeconds($segundos)
    while ((Get-Date) -lt $limite) {
        try {
            Invoke-WebRequest $url -UseBasicParsing -TimeoutSec 5 | Out-Null
            Write-Host ' listo.'
            return
        } catch {
            if ($_.Exception.Response) { Write-Host ' listo.'; return }
        }
        Start-Sleep -Seconds 2
    }
    Write-Host ''
    throw "$nombre no respondió en $segundos segundos."
}

function CargarDemo([hashtable]$opciones) {
    if ($SinDatos) { return }
    Write-Host ''
    try {
        & "$PSScriptRoot\scripts\CargarDatosDemo.ps1" @opciones
    } catch {
        Write-Warning "El sistema está en marcha, pero no se cargaron los datos de demostración: $($_.Exception.Message)"
    }
}

function MostrarResumen([string]$claveAdmin, [string]$claveDemo, [string]$detener) {
    Write-Host ''
    Write-Host 'Listo. El sistema está en marcha:'
    Write-Host '  MVC:       http://localhost:5102'
    Write-Host '  Reportes:  http://localhost:5103'
    Write-Host '  Swagger:   http://localhost:5101/swagger'
    Write-Host "  Admin:     usuario admin, contraseña $claveAdmin"
    if (-not $SinDatos -and $claveDemo) {
        Write-Host "  Demo:      mlopez, jhernandez (Docente); acastro, dmartinez, sramirez, kflores (Estudiante); contraseña $claveDemo"
    }
    Write-Host "  Detener:   $detener"
}

# ===========================================================================
# Opción A: Docker
# ===========================================================================
function IniciarDocker {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        throw 'No se encontró Docker. Instale Docker Desktop o use .\iniciar.ps1 -Local.'
    }
    try { docker info *> $null } catch { }   # En PowerShell 5.1 el texto de error de docker lanzaría una excepción.
    if ($LASTEXITCODE -ne 0) { throw 'Docker no responde. Abra Docker Desktop y espere a que esté en ejecución.' }

    if ($ReiniciarBase) {
        Write-Host 'Borrando los contenedores y la base de datos...'
        docker compose down -v
    }

    if (-not (Test-Path .env)) {
        Write-Host 'Creando .env con contraseñas nuevas...'
        # "Lab-" y "-9a" garantizan mayúscula, minúscula, número y símbolo,
        # que es lo que exige SQL Server.
        @(
            "SA_PASSWORD=Lab-$(Aleatorio 16)-9a"
            "REPORTES_PASSWORD=Lab-$(Aleatorio 16)-9a"
            "ADMIN_PASSWORD=Admin-$(Aleatorio 8)"
            "DEMO_PASSWORD=Demo-$(Aleatorio 8)"
            "JWT_KEY=$(Aleatorio 64)"
            "TZ=America/El_Salvador"
        ) | Set-Content .env -Encoding ascii
    }

    docker compose up -d --build
    if ($LASTEXITCODE -ne 0) { throw 'docker compose falló. Revise los mensajes anteriores.' }

    try {
        EsperarUrl 'http://localhost:5101/swagger/index.html' 'la API'
    } catch {
        throw "La API no arrancó. Revise sus mensajes con: docker compose logs api. Si cambió o borró .env, ejecute .\iniciar.ps1 -ReiniciarBase."
    }
    EsperarUrl 'http://localhost:5102/Auth/Login' 'el MVC'
    EsperarUrl 'http://localhost:5103/Cuenta/Login' 'Reportes'

    CargarDemo @{}

    $valores = @{}
    Get-Content .env | ForEach-Object { $i = $_.IndexOf('='); if ($i -gt 0) { $valores[$_.Substring(0, $i)] = $_.Substring($i + 1) } }
    MostrarResumen $valores['ADMIN_PASSWORD'] $valores['DEMO_PASSWORD'] 'docker compose stop'
}

# ===========================================================================
# Opción B: sin Docker (-Local)
# ===========================================================================
function Sql([string]$consulta) {
    $salida = sqlcmd -S $Instancia -E -C -b -h -1 -W -Q "SET NOCOUNT ON; $consulta"
    if ($LASTEXITCODE -ne 0) { throw "Falló la consulta en ${Instancia}: $salida" }
    return $salida
}

function LeerSecretos([string]$proyecto) {
    $secretos = @{}
    foreach ($linea in (dotnet user-secrets list --project $proyecto)) {
        $i = $linea.IndexOf(' = ')
        if ($i -gt 0) { $secretos[$linea.Substring(0, $i)] = $linea.Substring($i + 3) }
    }
    return $secretos
}

function FijarSecreto([string]$proyecto, [string]$clave, [string]$valor) {
    dotnet user-secrets set $clave $valor --project $proyecto | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "No se pudo guardar $clave en los User Secrets de $proyecto." }
    Write-Host "  $proyecto -> $clave"
}

function AbrirVentana([string]$proyecto, [int]$puerto) {
    $comando = "`$Host.UI.RawUI.WindowTitle = '$proyecto'; dotnet run --no-build --project $proyecto --urls http://localhost:$puerto"
    Start-Process powershell -WorkingDirectory $PSScriptRoot -ArgumentList "-NoExit -Command `"$comando`""
}

function IniciarLocal {
    # 1. Revisar lo que necesita el equipo.
    $sdks = try { dotnet --list-sdks } catch { @() }
    if (-not ($sdks -match '^10\.')) { throw 'Falta el SDK de .NET 10.' }
    if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
        throw 'No se encontró sqlcmd. Se instala junto con SQL Server; también puede instalarse con SSMS.'
    }
    foreach ($puerto in 5101, 5102, 5103) {
        if (Get-NetTCPConnection -LocalPort $puerto -State Listen -ErrorAction SilentlyContinue) {
            throw "El puerto $puerto ya está en uso. Si el sistema está corriendo en Docker, deténgalo con: docker compose stop"
        }
    }
    try {
        $soloWindows = Sql "SELECT CONVERT(int, SERVERPROPERTY('IsIntegratedSecurityOnly'))"
    } catch {
        throw "No se pudo conectar a $Instancia con autenticación de Windows. Revise que el servicio de SQL Server esté iniciado o indique otra instancia con -Instancia."
    }
    if ("$soloWindows".Trim() -eq '1') {
        # LoginMode 2 en el registro = modo mixto ya elegido, pero falta reiniciar el servicio.
        $modoGuardado = Sql "DECLARE @m int; EXEC master.dbo.xp_instance_regread N'HKEY_LOCAL_MACHINE', N'Software\Microsoft\MSSQLServer\MSSQLServer', N'LoginMode', @m OUTPUT; SELECT @m"
        if ("$modoGuardado".Trim() -eq '2') {
            throw "La autenticación de SQL Server ya está activada en $Instancia, pero falta reiniciar el servicio. En SSMS: clic derecho sobre el servidor > Reiniciar. Después vuelva a ejecutar .\iniciar.ps1 -Local"
        }
        throw @"
$Instancia solo acepta autenticación de Windows y Reportes necesita el login SQL "reportes".
Actívela una vez (README, opción B, paso 1):
  1. En SSMS, clic derecho sobre el servidor > Propiedades > Seguridad.
  2. Marque "Modo de autenticación de SQL Server y de Windows" y acepte.
  3. Clic derecho sobre el servidor > Reiniciar.
Después vuelva a ejecutar .\iniciar.ps1 -Local
"@
    }

    # 2. User Secrets. Solo se crean los que faltan; los existentes no se tocan.
    Write-Host 'Revisando los User Secrets...'
    $api = LeerSecretos 'EquiposAPI'
    if (-not $api['ConnectionStrings:EquiposDb']) {
        FijarSecreto 'EquiposAPI' 'ConnectionStrings:EquiposDb' "Server=$Instancia;Database=EquiposDb;Trusted_Connection=True;TrustServerCertificate=True"
    }
    if (-not $api['Jwt:Key'] -or $api['Jwt:Key'].Length -lt 32) {
        FijarSecreto 'EquiposAPI' 'Jwt:Key' (Aleatorio 64)
    }
    $claveAdmin = $api['SeedAdmin:Password']
    if (-not $claveAdmin) {
        $claveAdmin = "Admin-$(Aleatorio 8)"
        FijarSecreto 'EquiposAPI' 'SeedAdmin:Password' $claveAdmin
    }
    $claveDemo = $api['SeedDemo:Password']
    if (-not $claveDemo) {
        $claveDemo = "Demo-$(Aleatorio 8)"
        FijarSecreto 'EquiposAPI' 'SeedDemo:Password' $claveDemo
    }

    $cadenaReportes = (LeerSecretos 'EquiposReportes')['ConnectionStrings:EquiposReportes']
    if ($cadenaReportes -match 'Password=([^;]+)') {
        $claveReportes = $Matches[1]
    } else {
        $claveReportes = "Lab-$(Aleatorio 16)-9a"
        FijarSecreto 'EquiposReportes' 'ConnectionStrings:EquiposReportes' "Server=$Instancia;Database=EquiposDb;User Id=reportes;Password=$claveReportes;TrustServerCertificate=True"
    }

    # 3. Base de datos nueva, si se pidió.
    if ($ReiniciarBase) {
        Write-Host 'Borrando la base EquiposDb...'
        Sql "IF DB_ID('EquiposDb') IS NOT NULL BEGIN ALTER DATABASE EquiposDb SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE EquiposDb; END" | Out-Null
    }

    # 4. Compilar una vez y arrancar la API. Al arrancar aplica las migraciones
    #    y crea el usuario admin si la tabla Usuarios está vacía.
    Write-Host 'Compilando la solución...'
    dotnet build EquiposLaboratorio.slnx -nologo -v q
    if ($LASTEXITCODE -ne 0) { throw 'La solución no compila. Revise los errores anteriores.' }

    AbrirVentana 'EquiposAPI' 5101
    EsperarUrl 'http://localhost:5101/swagger/index.html' 'la API'

    # 5. Login de solo lectura "reportes" (necesita que la base ya exista).
    Write-Host 'Creando el login de solo lectura reportes...'
    sqlcmd -S $Instancia -E -C -b -i 'EquiposReportes\sql\crear_usuario_reportes.sql' -v "ReportesPassword=$claveReportes" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo crear el login reportes.' }

    # 6. MVC y Reportes.
    AbrirVentana 'EquiposMVC' 5102
    AbrirVentana 'EquiposReportes' 5103
    EsperarUrl 'http://localhost:5102/Auth/Login' 'el MVC'
    EsperarUrl 'http://localhost:5103/Cuenta/Login' 'Reportes'

    CargarDemo @{ Local = $true; ClaveDemo = $claveDemo }
    MostrarResumen $claveAdmin $claveDemo 'cierre las tres ventanas de PowerShell que se abrieron'
}

if ($Local) { IniciarLocal } else { IniciarDocker }
