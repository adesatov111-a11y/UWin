# UWin surum derlemesi - tek dosya, kurulum gerektirmez
$ErrorActionPreference = "Stop"

$cikti = Join-Path $PSScriptRoot "yayin"
if (Test-Path $cikti) { Remove-Item $cikti -Recurse -Force }

Write-Output "Testler calistiriliyor..."
dotnet test (Join-Path $PSScriptRoot "UWin.slnx") --nologo --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw "Testler basarisiz. Surum cikarilmadi." }

Write-Output "Yayin derlemesi hazirlaniyor..."
dotnet publish (Join-Path $PSScriptRoot "kaynak/UWin.Uygulama/UWin.Uygulama.csproj") `
    -c Release -r win-x64 -o $cikti --nologo

$exe = Join-Path $cikti "UWin.exe"
if (-not (Test-Path $exe)) { throw "UWin.exe uretilemedi." }

# Hata ayiklama sembolleri dagitima girmez
Get-ChildItem $cikti -Filter *.pdb | Remove-Item -Force

$boyutMb = [math]::Round((Get-Item $exe).Length / 1MB, 1)
Write-Output ""
Write-Output "UWin.exe hazir: $exe ($boyutMb MB)"
Write-Output "Surum oncesi docs/manuel-dogrulama.md listesini gercek donanimda yurut."
