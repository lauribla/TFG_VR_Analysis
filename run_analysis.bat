@echo off
setlocal
REM ================================================================
REM  SCRIPT DE EJECUCION PORTABLE (PLUG & PLAY)
REM  Usa rutas relativas (dp0) para encontrar el entorno virtual
REM ================================================================

REM Cambiar al directorio donde está ESTE script (.bat)
cd /d "%~dp0"

echo [INFO] Directorio de trabajo: %CD%

REM Verificar si existe el entorno virtual
IF EXIST ".venv\Scripts\python.exe" (
    echo [INFO] Entorno virtual detectado. Ejecutando analisis...
    ".venv\Scripts\python.exe" python_analysis\vr_analysis.py
) ELSE (
    echo [ERROR] No se encuentra .venv\Scripts\python.exe
    echo Por favor asegurese de tener la carpeta .venv junto a este script.
    pause
    exit /b 1
)

echo [INFO] Analisis finalizado. Revisar logs/reportes.
REM Descomentar la siguiente linea si quieres que la ventana no se cierre sola
REM pause
