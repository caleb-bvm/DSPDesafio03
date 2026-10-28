# Arranca todo el sistema en Docker con un solo comando:
#     .\iniciar.ps1
#
# 1. Si no existe el archivo .env, lo crea con contraseñas aleatorias.
# 2. Compila y levanta los contenedores (SQL Server, API, MVC y Reportes).
# 3. Muestra las URL y el usuario Admin.
Set-Location $PSScriptRoot

# Genera una cadena aleatoria de letras y números.
function Aleatorio([int]$largo) {
    $letras = "abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray()
    $bytes = New-Object byte[] $largo
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    -join ($bytes | ForEach-Object { $letras[$_ % $letras.Length] })
}

if (-not (Test-Path .env)) {
    Write-Host "Creando .env con contraseñas nuevas..."
    # "Lab-" y "-9a" garantizan mayúscula, minúscula, número y símbolo,
    # que es lo que exige SQL Server.
    @(
        "SA_PASSWORD=Lab-$(Aleatorio 16)-9a"
        "REPORTES_PASSWORD=Lab-$(Aleatorio 16)-9a"
        "ADMIN_PASSWORD=Admin-$(Aleatorio 8)"
        "JWT_KEY=$(Aleatorio 64)"
        "TZ=America/El_Salvador"
    ) | Set-Content .env -Encoding ascii
}

docker compose up -d --build
if ($LASTEXITCODE -ne 0) { throw "docker compose falló. ¿Está abierto Docker Desktop?" }

$admin = (Get-Content .env | Where-Object { $_ -like "ADMIN_PASSWORD=*" }) -replace "^ADMIN_PASSWORD=", ""
Write-Host ""
Write-Host "Listo. Espere unos segundos a que arranque Reportes."
Write-Host "  MVC:      http://localhost:5102"
Write-Host "  Reportes: http://localhost:5103"
Write-Host "  Swagger:  http://localhost:5101/swagger"
Write-Host "  Usuario:  admin   Contraseña: $admin"
