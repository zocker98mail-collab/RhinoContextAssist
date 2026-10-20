param([string]$Configuration = "Release")
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
  dotnet restore .\RhinoContextAssist.csproj
  dotnet build .\RhinoContextAssist.csproj -c $Configuration --no-restore
  $payload = Join-Path $root "yak-payload"
  Remove-Item $payload -Recurse -Force -ErrorAction SilentlyContinue
  New-Item $payload -ItemType Directory | Out-Null
  Copy-Item .\package\manifest.yml $payload
  Copy-Item .\bin\$Configuration\net7.0\RhinoContextAssist.rhp $payload
  Copy-Item .\bin\$Configuration\net7.0\RhinoContextAssist.deps.json $payload -ErrorAction SilentlyContinue
  $yak = "C:\Program Files\Rhino 8\System\yak.exe"
  if (-not (Test-Path $yak)) { throw "Rhino 8 yak.exe nicht gefunden: $yak" }
  Push-Location $payload
  try { & $yak build --platform win } finally { Pop-Location }
  Write-Host "Yak-Paket erstellt in $payload"
} finally { Pop-Location }
