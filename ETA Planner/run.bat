@echo off
title AERO-GCS Flight Endurance Estimator
echo ==================================================
echo   AERO-GCS FLIGHT ENDURANCE ESTIMATOR BOOTSTRAP   
echo   Developed by Aadarsh Sinha & Krishnashish Das   
echo ==================================================
echo.
echo [INFO] Activating virtual environment...
if not exist ".venv" (
    echo [ERROR] Virtual environment not found. Please run installation setup.
    pause
    exit /b 1
)

echo [INFO] Starting FastAPI App server...
echo [INFO] Service will be available at http://localhost:7006/
echo.

:: Launch browser after a short delay to allow server startup
start /b cmd /c "timeout /t 3 >nul && start http://localhost:7006/"

:: Start Uvicorn reload server
.venv\Scripts\uvicorn backend.main:app --reload --port 7006 --host 127.0.0.1

pause
