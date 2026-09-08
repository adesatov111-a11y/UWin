@echo off
REM UWin surum derlemesi - cift tiklanarak calistirilir.
REM PowerShell betigini yurutme ilkesini kalici degistirmeden calistirir.
cd /d "%~dp0"

echo ============================================
echo  UWin surum derlemesi
echo ============================================
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0yayinla.ps1"
set KOD=%ERRORLEVEL%

echo.
if %KOD% NEQ 0 (
    echo ------------------------------------------------
    echo  HATA: Derleme tamamlanamadi ^(kod %KOD%^).
    echo  Yukaridaki mesaji okuyun.
    echo ------------------------------------------------
) else (
    echo ------------------------------------------------
    echo  Tamam. Dosya: yayin\UWin.exe
    echo ------------------------------------------------
)

echo.
REM Pencere kapanmasin ki cift tiklayan kullanici sonucu gorebilsin.
pause
