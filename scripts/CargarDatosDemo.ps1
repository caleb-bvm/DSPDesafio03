# Carga datos de demostración llamando a la API (nunca escribe directo en la base):
#     .\scripts\CargarDatosDemo.ps1            # Sistema levantado con Docker
#     .\scripts\CargarDatosDemo.ps1 -Local     # Sistema levantado sin Docker
#
# Crea docentes, estudiantes, 15 equipos y préstamos (devueltos y activos).
# Se puede ejecutar varias veces: lo que ya existe no se vuelve a crear.
# Funciona con Windows PowerShell 5.1 y con PowerShell 7.
param(
    [switch]$Local,                              # Lee la contraseña de admin de User Secrets en vez de .env.
    [string]$ClaveDemo,                          # Contraseña de los usuarios de demostración.
    [string]$ApiUrl = 'http://localhost:5101'
)

$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)

# ---------------------------------------------------------------------------
# 1. Contraseñas. Ninguna se escribe en el script ni en el repositorio.
# ---------------------------------------------------------------------------
function LeerEnv([string]$nombre) {
    if (-not (Test-Path .env)) { return $null }
    $linea = Get-Content .env | Where-Object { $_ -like "$nombre=*" } | Select-Object -First 1
    if ($linea) { return $linea.Substring($nombre.Length + 1) }
    return $null
}

if ($Local) {
    $archivoSecretos = Join-Path $env:APPDATA 'Microsoft\UserSecrets\EquiposLaboratorio-EquiposAPI\secrets.json'
    if (-not (Test-Path $archivoSecretos)) { throw 'Configure primero los User Secrets de EquiposAPI.' }
    $claveAdmin = (Get-Content $archivoSecretos -Raw | ConvertFrom-Json).'SeedAdmin:Password'
} else {
    $claveAdmin = LeerEnv 'ADMIN_PASSWORD'
    if (-not $claveAdmin) { throw 'No se encontró ADMIN_PASSWORD en .env. Ejecute primero .\iniciar.ps1.' }
}

# Contraseña de los usuarios de demostración: parámetro, línea DEMO_PASSWORD de .env
# (con Docker se crea si no existe) o se pide por teclado.
if (-not $ClaveDemo -and -not $Local) {
    $ClaveDemo = LeerEnv 'DEMO_PASSWORD'
    if (-not $ClaveDemo) {
        $letras = 'abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789'.ToCharArray()
        $bytes = New-Object byte[] 8
        [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
        $ClaveDemo = 'Demo-' + (-join ($bytes | ForEach-Object { $letras[$_ % $letras.Length] }))
        Add-Content .env "DEMO_PASSWORD=$ClaveDemo" -Encoding ascii
    }
}
if (-not $ClaveDemo) {
    $segura = Read-Host 'Contraseña para los usuarios de demostración (mínimo 8 caracteres)' -AsSecureString
    $ClaveDemo = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($segura))
}
if ($ClaveDemo.Length -lt 8) { throw 'La contraseña de demostración debe tener al menos 8 caracteres.' }

# ---------------------------------------------------------------------------
# 2. Llamadas a la API. Devuelve { Codigo, Datos, Mensaje } en vez de lanzar
#    una excepción cuando la API responde 4xx o 5xx.
# ---------------------------------------------------------------------------
$script:token = $null

function Api([string]$metodo, [string]$ruta, $datos = $null) {
    $opciones = @{ Uri = "$ApiUrl/$ruta"; Method = $metodo }
    if ($script:token) { $opciones.Headers = @{ Authorization = "Bearer $script:token" } }
    if ($null -ne $datos) {
        $opciones.ContentType = 'application/json; charset=utf-8'
        $opciones.Body = [Text.Encoding]::UTF8.GetBytes(($datos | ConvertTo-Json -Depth 5))
    }
    try {
        return [pscustomobject]@{ Codigo = 200; Datos = (Invoke-RestMethod @opciones); Mensaje = '' }
    } catch {
        $respuesta = $_.Exception.Response
        if ($null -eq $respuesta) { throw "No se pudo conectar con la API en $ApiUrl. ¿Está encendida?" }
        $mensaje = if ($_.ErrorDetails) { $_.ErrorDetails.Message } else { '' }
        return [pscustomobject]@{ Codigo = [int]$respuesta.StatusCode; Datos = $null; Mensaje = $mensaje }
    }
}

$login = Api 'POST' 'api/auth/login' @{ username = 'admin'; password = $claveAdmin }
if ($login.Codigo -ne 200) { throw "No se pudo iniciar sesión como admin (HTTP $($login.Codigo))." }
$script:token = $login.Datos.token
Write-Host "Sesión iniciada como admin en $ApiUrl."

# ---------------------------------------------------------------------------
# 3. Usuarios. Si el username ya existe (409), se conserva el que había.
# ---------------------------------------------------------------------------
$usuariosDemo = @(
    @{ username = 'mlopez';     email = 'mlopez@laboratorio.local';     rol = 'Docente' }
    @{ username = 'jhernandez'; email = 'jhernandez@laboratorio.local'; rol = 'Docente' }
    @{ username = 'acastro';    email = 'acastro@laboratorio.local';    rol = 'Estudiante' }
    @{ username = 'dmartinez';  email = 'dmartinez@laboratorio.local';  rol = 'Estudiante' }
    @{ username = 'sramirez';   email = 'sramirez@laboratorio.local';   rol = 'Estudiante' }
    @{ username = 'kflores';    email = 'kflores@laboratorio.local';    rol = 'Estudiante' }
)

$creados = 0
foreach ($u in $usuariosDemo) {
    $r = Api 'POST' 'api/auth/register' ($u + @{ password = $ClaveDemo })
    if ($r.Codigo -eq 200) { $creados++ }
    elseif ($r.Codigo -ne 409) { throw "No se pudo crear el usuario $($u.username): HTTP $($r.Codigo) $($r.Mensaje)" }
}
Write-Host "Usuarios: $creados nuevos, $($usuariosDemo.Count - $creados) ya existían."

$idUsuario = @{}
foreach ($u in @((Api 'GET' 'api/usuarios').Datos)) { $idUsuario[$u.username] = $u.id }

# ---------------------------------------------------------------------------
# 4. Equipos. Si el código de inventario ya existe (409), se busca el existente.
# ---------------------------------------------------------------------------
$idCategoria = @{}
foreach ($c in @((Api 'GET' 'api/categorias').Datos)) { $idCategoria[$c.nombre] = $c.id }

$equiposDemo = @(
    ,@('LAP-001', 'Laptop Dell Latitude 5440',      'Core i5, 16 GB RAM, 512 GB SSD',            'Laptops',          'Disponible')
    ,@('LAP-002', 'Laptop HP ProBook 450 G9',       'Core i5, 8 GB RAM, 256 GB SSD',             'Laptops',          'Disponible')
    ,@('LAP-003', 'Laptop Lenovo ThinkPad E14',     'Ryzen 5, 16 GB RAM, 512 GB SSD',            'Laptops',          'Disponible')
    ,@('LAP-004', 'Laptop Acer Aspire 5',           'Pantalla dañada, en revisión',              'Laptops',          'Mantenimiento')
    ,@('PRY-001', 'Proyector Epson PowerLite X49',  '3600 lúmenes, HDMI y VGA',                  'Proyectores',      'Disponible')
    ,@('PRY-002', 'Proyector BenQ MS560',           '4000 lúmenes, incluye control remoto',      'Proyectores',      'Disponible')
    ,@('PRY-003', 'Proyector ViewSonic PA503S',     'Cambio de lámpara pendiente',               'Proyectores',      'Mantenimiento')
    ,@('IOT-001', 'Kit ESP32 con sensores',         'ESP32, DHT22, HC-SR04 y protoboard',        'Kits de IoT',      'Disponible')
    ,@('IOT-002', 'Kit Raspberry Pi 4',             'Raspberry Pi 4 de 4 GB, fuente y microSD',  'Kits de IoT',      'Disponible')
    ,@('IOT-003', 'Kit de sensores ambientales',    'Sensores de gas, luz, humedad y temperatura', 'Kits de IoT',    'Disponible')
    ,@('ARD-001', 'Arduino Uno R3',                 'Con cable USB y kit de LEDs',               'Tarjetas Arduino', 'Disponible')
    ,@('ARD-002', 'Arduino Mega 2560',              'Con cable USB y shield de motores',         'Tarjetas Arduino', 'Disponible')
    ,@('ARD-003', 'Arduino Nano',                   'Con protoboard y cables dupont',            'Tarjetas Arduino', 'Disponible')
    ,@('ARD-004', 'Arduino Uno R4 WiFi',            'Con cable USB-C',                           'Tarjetas Arduino', 'Disponible')
    ,@('ARD-005', 'Arduino Leonardo',               'Con cable micro USB',                       'Tarjetas Arduino', 'Disponible')
)

$idEquipo = @{}
$creados = 0
foreach ($e in $equiposDemo) {
    $r = Api 'POST' 'api/equipos' @{
        codigoInventario = $e[0]; nombre = $e[1]; descripcion = $e[2]
        categoriaId = $idCategoria[$e[3]]; estado = $e[4]
    }
    if ($r.Codigo -eq 200) {
        $creados++
        $idEquipo[$e[0]] = $r.Datos.id
    } elseif ($r.Codigo -eq 409) {
        $busqueda = Api 'GET' "api/equipos?texto=$($e[0])"
        $idEquipo[$e[0]] = (@($busqueda.Datos.items) | Where-Object { $_.codigoInventario -eq $e[0] }).id
    } else {
        throw "No se pudo crear el equipo $($e[0]): HTTP $($r.Codigo) $($r.Mensaje)"
    }
}
Write-Host "Equipos: $creados nuevos, $($equiposDemo.Count - $creados) ya existían."

# ---------------------------------------------------------------------------
# 5. Préstamos. Solo se cargan una vez: si algún usuario de demostración ya
#    tiene préstamos, se omite este paso.
# ---------------------------------------------------------------------------
$nombresDemo = $usuariosDemo | ForEach-Object { $_.username }
$prestamosDemo = @(@((Api 'GET' 'api/prestamos').Datos) | Where-Object { $nombresDemo -contains $_.username })
if ($prestamosDemo.Count -gt 0) {
    Write-Host 'Préstamos: ya estaban cargados, no se agregan más.'
} else {
    function Prestar([string]$codigo, [string]$usuario, [int]$diasVence, [string]$observaciones) {
        $r = Api 'POST' 'api/prestamos' @{
            equipoId = $idEquipo[$codigo]; usuarioId = $idUsuario[$usuario]
            fechaVencimiento = (Get-Date).AddDays($diasVence).ToString('yyyy-MM-dd')
            observaciones = $observaciones
        }
        if ($r.Codigo -ne 200) { Write-Warning "No se prestó $codigo a ${usuario}: HTTP $($r.Codigo) $($r.Mensaje)"; return $null }
        return $r.Datos.id
    }

    # Préstamos ya devueltos: forman el historial y el Top 5.
    $historial = @(
        ,@('LAP-001', 'acastro',    'Proyecto de programación')
        ,@('LAP-001', 'dmartinez',  'Práctica de bases de datos')
        ,@('LAP-001', 'mlopez',     'Clase de desarrollo web')
        ,@('LAP-001', 'sramirez',   'Presentación de tesis')
        ,@('PRY-001', 'mlopez',     'Clase en el auditorio')
        ,@('PRY-001', 'jhernandez', 'Defensa de proyectos')
        ,@('PRY-001', 'acastro',    'Exposición de grupo')
        ,@('ARD-001', 'dmartinez',  'Laboratorio de electrónica')
        ,@('ARD-001', 'kflores',    'Práctica de sensores')
        ,@('ARD-001', 'sramirez',   'Proyecto de robótica')
        ,@('IOT-001', 'acastro',    'Práctica de MQTT')
        ,@('IOT-001', 'kflores',    'Estación meteorológica')
        ,@('LAP-002', 'sramirez',   'Práctica de redes')
        ,@('LAP-002', 'jhernandez', 'Taller de ofimática')
        ,@('PRY-002', 'jhernandez', 'Charla de orientación')
    )
    $devueltos = 0
    foreach ($p in $historial) {
        $id = Prestar $p[0] $p[1] 3 $p[2]
        if ($id -and (Api 'PUT' "api/prestamos/$id/devolucion").Codigo -eq 200) { $devueltos++ }
    }

    # Préstamos activos. Los de IOT-002, LAP-003 y ARD-003 son los que
    # sql/DemoPrestamosVencidos.sql puede mover al pasado para el reporte de vencidos.
    $activos = @(
        ,@('LAP-001', 'acastro',    3, 'Proyecto final de programación')
        ,@('PRY-001', 'mlopez',     2, 'Semana de clases en el aula 3')
        ,@('ARD-002', 'dmartinez',  7, 'Proyecto de control de motores')
        ,@('IOT-002', 'kflores',    1, 'Servidor para práctica de Linux')
        ,@('LAP-003', 'sramirez',   2, 'Trabajo de investigación')
        ,@('ARD-003', 'acastro',    1, 'Práctica de electrónica básica')
    )
    $prestados = 0
    foreach ($p in $activos) {
        if (Prestar $p[0] $p[1] $p[2] $p[3]) { $prestados++ }
    }
    Write-Host "Préstamos: $devueltos devueltos y $prestados activos."
}

Write-Host ''
Write-Host 'Listo. Usuarios de demostración: mlopez y jhernandez (Docente), acastro, dmartinez, sramirez y kflores (Estudiante).'
if (-not $Local -and -not $PSBoundParameters.ContainsKey('ClaveDemo')) {
    Write-Host 'Su contraseña está en la línea DEMO_PASSWORD del archivo .env.'
}
