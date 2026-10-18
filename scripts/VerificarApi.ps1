# Ejecuta los casos de la API sin guardar ni mostrar la contraseña Admin.
# Requiere la API en http://localhost:5101 y sus User Secrets configurados.

$ErrorActionPreference = 'Stop'
$baseUrl = 'http://localhost:5101'
$archivoSecretos = Join-Path $env:APPDATA 'Microsoft\UserSecrets\EquiposLaboratorio-EquiposAPI\secrets.json'
if (-not (Test-Path -LiteralPath $archivoSecretos)) {
    throw 'Configure primero los User Secrets de EquiposAPI.'
}
$secretos = Get-Content -LiteralPath $archivoSecretos -Raw | ConvertFrom-Json -AsHashtable
$claveAdmin = $secretos['SeedAdmin:Password']
if ([string]::IsNullOrWhiteSpace($claveAdmin)) { throw 'Falta SeedAdmin:Password.' }

function Enviar([string]$metodo, [string]$ruta, $datos = $null, [string]$token = '') {
    $opciones = @{
        Uri = "$baseUrl/$ruta"
        Method = $metodo
        SkipHttpErrorCheck = $true
    }
    if ($token) { $opciones.Headers = @{ Authorization = "Bearer $token" } }
    if ($null -ne $datos) {
        $opciones.ContentType = 'application/json'
        $opciones.Body = $datos | ConvertTo-Json -Depth 5
    }
    Invoke-WebRequest @opciones
}

function Verificar([string]$caso, [int]$obtenido, [int]$esperado) {
    $resultado = if ($obtenido -eq $esperado) { 'OK' } else { 'FALLÓ' }
    Write-Output "$caso : $obtenido (esperado $esperado) - $resultado"
    if ($obtenido -ne $esperado) { throw "Falló $caso." }
}

$login = Enviar 'POST' 'api/auth/login' @{ username = 'admin'; password = $claveAdmin }
Verificar '01 Login Admin' $login.StatusCode 200
$respuestaLogin = $login.Content | ConvertFrom-Json
$tokenAdmin = $respuestaLogin.token
$parte = $tokenAdmin.Split('.')[1].Replace('-', '+').Replace('_', '/')
$parte = $parte.PadRight([int]([Math]::Ceiling($parte.Length / 4) * 4), '=')
$claims = [Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($parte)) | ConvertFrom-Json
if ($claims.role -ne 'Admin') { throw 'El JWT no contiene el rol Admin.' }

$incorrecto = Enviar 'POST' 'api/auth/login' @{ username = 'admin'; password = "$claveAdmin-incorrecta" }
Verificar '02 Contraseña incorrecta' $incorrecto.StatusCode 401
Verificar '03 Sin token' (Enviar 'GET' 'api/categorias').StatusCode 401

$categorias = (Enviar 'GET' 'api/categorias' $null $tokenAdmin).Content | ConvertFrom-Json
$categoriaId = $categorias[0].id
$sufijo = [Guid]::NewGuid().ToString('N').Substring(0, 8)
$usuario = "estudiante_$sufijo"
$claveEstudiante = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(18))
$registro = Enviar 'POST' 'api/auth/register' @{
    username = $usuario; email = "$usuario@ejemplo.local";
    password = $claveEstudiante; rol = 'Estudiante'
} $tokenAdmin
Verificar 'Preparación: registrar estudiante' $registro.StatusCode 201
$estudianteId = ($registro.Content | ConvertFrom-Json).id
$loginEstudiante = Enviar 'POST' 'api/auth/login' @{ username = $usuario; password = $claveEstudiante }
Verificar 'Preparación: login estudiante' $loginEstudiante.StatusCode 200
$tokenEstudiante = ($loginEstudiante.Content | ConvertFrom-Json).token

$prohibido = Enviar 'POST' 'api/equipos' @{
    codigoInventario = "LAB-X-$sufijo"; nombre = 'Prueba de rol';
    descripcion = ''; categoriaId = $categoriaId; estado = 'Disponible'
} $tokenEstudiante
Verificar '04 Estudiante crea equipo' $prohibido.StatusCode 403
$duplicado = Enviar 'POST' 'api/auth/register' @{
    username = $usuario; email = "$usuario@ejemplo.local";
    password = $claveEstudiante; rol = 'Estudiante'
} $tokenAdmin
Verificar '05 Username repetido' $duplicado.StatusCode 409

$equipoPrestamo = Enviar 'POST' 'api/equipos' @{
    codigoInventario = "LAB-P-$sufijo"; nombre = 'Laptop de prueba';
    descripcion = 'Datos para la defensa'; categoriaId = $categoriaId;
    estado = 'Disponible'
} $tokenAdmin
Verificar 'Preparación: equipo para préstamo' $equipoPrestamo.StatusCode 201
$equipoPrestamoId = ($equipoPrestamo.Content | ConvertFrom-Json).id
$fecha15 = (Get-Date).AddDays(15).ToString('yyyy-MM-ddTHH:mm:ss')
$fecha3 = (Get-Date).AddDays(3).ToString('yyyy-MM-ddTHH:mm:ss')
$fechaInvalida = Enviar 'POST' 'api/prestamos' @{
    equipoId = $equipoPrestamoId; usuarioId = $estudianteId;
    fechaVencimiento = $fecha15; observaciones = 'Debe rechazarse'
} $tokenAdmin
Verificar '07 Vencimiento a 15 días' $fechaInvalida.StatusCode 400
$prestamo = Enviar 'POST' 'api/prestamos' @{
    equipoId = $equipoPrestamoId; usuarioId = $estudianteId;
    fechaVencimiento = $fecha3; observaciones = 'Prueba de integración'
} $tokenAdmin
Verificar 'Preparación: préstamo válido' $prestamo.StatusCode 201
$prestamoRepetido = Enviar 'POST' 'api/prestamos' @{
    equipoId = $equipoPrestamoId; usuarioId = $estudianteId;
    fechaVencimiento = $fecha3; observaciones = 'Debe rechazarse'
} $tokenAdmin
Verificar '06 Equipo ya prestado' $prestamoRepetido.StatusCode 409
Verificar '08 Borrar equipo con historial' (Enviar 'DELETE' "api/equipos/$equipoPrestamoId" $null $tokenAdmin).StatusCode 409

$equipoEdicion = Enviar 'POST' 'api/equipos' @{
    codigoInventario = "LAB-C-$sufijo"; nombre = 'Proyector de prueba';
    descripcion = 'Concurrencia'; categoriaId = $categoriaId;
    estado = 'Disponible'
} $tokenAdmin
Verificar 'Preparación: equipo para concurrencia' $equipoEdicion.StatusCode 201
$equipoEdicionId = ($equipoEdicion.Content | ConvertFrom-Json).id
$anterior = (Enviar 'GET' "api/equipos/$equipoEdicionId" $null $tokenAdmin).Content | ConvertFrom-Json
$primera = Enviar 'PUT' "api/equipos/$equipoEdicionId" @{
    codigoInventario = "LAB-C-$sufijo"; nombre = 'Proyector actualizado';
    descripcion = 'Primer cambio'; categoriaId = $categoriaId;
    estado = 'Disponible'; rowVersion = $anterior.rowVersion
} $tokenAdmin
Verificar 'Preparación: primera edición' $primera.StatusCode 200
$segunda = Enviar 'PUT' "api/equipos/$equipoEdicionId" @{
    codigoInventario = "LAB-C-$sufijo"; nombre = 'Cambio tardío';
    descripcion = 'Segunda edición'; categoriaId = $categoriaId;
    estado = 'Disponible'; rowVersion = $anterior.rowVersion
} $tokenAdmin
Verificar '09 Segunda edición simultánea' $segunda.StatusCode 409

$propios = Enviar 'GET' 'api/prestamos?usuarioId=1' $null $tokenEstudiante
Verificar '10 Listado de estudiante' $propios.StatusCode 200
$lista = @($propios.Content | ConvertFrom-Json)
if ($lista.Count -eq 0 -or @($lista | Where-Object { $_.usuarioId -ne $estudianteId }).Count -gt 0) {
    throw 'El listado del Estudiante no contiene exclusivamente sus préstamos.'
}
Write-Output '10 Estudiante solo ve préstamos propios - OK'
Write-Output 'Casos API 1–10 verificados. Casos 11 y 12: verificar en MVC y Reportes.'
