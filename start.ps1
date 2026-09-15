$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location -LiteralPath $Root

function Require-Command([string]$Name, [string]$Hint) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        Write-Host "Не знайдено команду '$Name'."
        Write-Host $Hint
        exit 1
    }
}

Require-Command "dotnet" "Встановіть .NET 10 SDK: https://dotnet.microsoft.com/download"
Require-Command "node" "Встановіть Node.js LTS: https://nodejs.org/"
Require-Command "npm" "npm іде разом з Node.js. Перезапустіть термінал після встановлення."

$dotnetVersion = (& dotnet --version)
if (-not $dotnetVersion.StartsWith("10.")) {
    Write-Host "Потрібен SDK .NET 10, зараз: $dotnetVersion"
    exit 1
}

if (Get-Command sqllocaldb -ErrorAction SilentlyContinue) {
    try {
        & sqllocaldb start MSSQLLocalDB | Out-Null
    } catch {
        Write-Host "LocalDB не стартував. Перевірте, що встановлено SQL Server LocalDB."
        exit 1
    }
} else {
    Write-Host "Немає sqllocaldb. Для локальної бази потрібен SQL Server LocalDB."
    exit 1
}

$frontendEnv = Join-Path $Root "Frontend\.env"
if (-not (Test-Path -LiteralPath $frontendEnv)) {
    Set-Content -LiteralPath $frontendEnv -Value "VITE_API_ORIGIN=http://localhost:5176" -Encoding utf8
}

Write-Host "Відновлення пакетів C#..."
& dotnet restore (Join-Path $Root "Backend\Backend.slnx")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$nodeModules = Join-Path $Root "Frontend\node_modules"
if (-not (Test-Path -LiteralPath $nodeModules)) {
    Write-Host "Встановлення пакетів фронтенду (перший запуск)..."
    Push-Location (Join-Path $Root "Frontend")
    try {
        & npm install
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    } finally {
        Pop-Location
    }
}

$backendDir = Join-Path $Root "Backend"
$frontendDir = Join-Path $Root "Frontend"
$apiCsproj = Join-Path $backendDir "Project.Api\Project.Api.csproj"

$apiCommand = @"
Set-Location -LiteralPath '$backendDir'
`$env:ASPNETCORE_ENVIRONMENT = 'Development'
`$env:ASPNETCORE_URLS = 'http://localhost:5176'
Write-Host 'API: http://localhost:5176'
dotnet run --project '$apiCsproj' --launch-profile http
"@

$webCommand = @"
Set-Location -LiteralPath '$frontendDir'
Write-Host 'Сайт: http://localhost:5173'
npm run dev
"@

Write-Host "Запускаю API і фронтенд у двох вікнах..."
Start-Process -FilePath "powershell.exe" -ArgumentList @("-NoExit", "-NoProfile", "-Command", $apiCommand)
Start-Sleep -Seconds 2
Start-Process -FilePath "powershell.exe" -ArgumentList @("-NoExit", "-NoProfile", "-Command", $webCommand)
Start-Process -FilePath "powershell.exe" -WindowStyle Hidden -ArgumentList @("-NoProfile", "-Command", "Start-Sleep -Seconds 8; Start-Process 'http://localhost:5173'")

Write-Host ""
Write-Host "Готово. Не закривайте два нові вікна PowerShell, поки працюєте з сайтом."
Write-Host "Сайт:  http://localhost:5173"
Write-Host "API:   http://localhost:5176"
Write-Host "Навчальний господар: host@everywherehome.local / Host123!"
Write-Host "Навчальний гість:    guest@everywherehome.local / Guest123!"
