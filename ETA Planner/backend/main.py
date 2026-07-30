import os
import io
import csv
import logging
from typing import List, Optional
from fastapi import FastAPI, Depends, HTTPException, Query, status
from fastapi.middleware.cors import CORSMiddleware
from fastapi.staticfiles import StaticFiles
from fastapi.responses import StreamingResponse, FileResponse
from sqlalchemy.orm import Session

from backend.database import get_db, engine
from backend import models, schemas, physics, weather

# Setup Logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Initialize DB Tables
models.Base.metadata.create_all(bind=engine)

app = FastAPI(
    title="AERO-GCS Flight Endurance Estimator API",
    description="Backend calculation engine for UAV flight time estimation, battery degradation, and environmental derating.",
    version="1.0.0"
)

@app.middleware("http")
async def add_no_cache_headers(request, call_next):
    response = await call_next(request)
    response.headers["Cache-Control"] = "no-cache, no-store, must-revalidate, max-age=0"
    response.headers["Pragma"] = "no-cache"
    response.headers["Expires"] = "0"
    return response

# CORS configuration
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# --- AIRCRAFT ENDPOINTS ---

@app.post("/api/aircraft", response_model=schemas.AircraftResponse, status_code=status.HTTP_201_CREATED)
def create_aircraft(aircraft: schemas.AircraftCreate, db: Session = Depends(get_db)):
    db_aircraft = db.query(models.Aircraft).filter(models.Aircraft.name == aircraft.name).first()
    if db_aircraft:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=f"Aircraft profile with name '{aircraft.name}' already exists."
        )
    
    new_aircraft = models.Aircraft(**aircraft.model_dump())
    db.add(new_aircraft)
    try:
        db.commit()
        db.refresh(new_aircraft)
        return new_aircraft
    except Exception as e:
        db.rollback()
        logger.error(f"Error creating aircraft profile: {e}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="Failed to create aircraft profile due to a database error."
        )

@app.get("/api/aircraft", response_model=List[schemas.AircraftResponse])
def list_aircraft(db: Session = Depends(get_db)):
    return db.query(models.Aircraft).order_by(models.Aircraft.name).all()

@app.get("/api/aircraft/{aircraft_id}", response_model=schemas.AircraftResponse)
def get_aircraft(aircraft_id: int, db: Session = Depends(get_db)):
    aircraft = db.query(models.Aircraft).filter(models.Aircraft.id == aircraft_id).first()
    if not aircraft:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Aircraft profile with ID {aircraft_id} not found."
        )
    return aircraft

@app.put("/api/aircraft/{aircraft_id}/coefficients", response_model=schemas.AircraftResponse)
def update_aircraft_coefficients(
    aircraft_id: int, 
    coefficients: schemas.AircraftUpdateCoefficients, 
    db: Session = Depends(get_db)
):
    aircraft = db.query(models.Aircraft).filter(models.Aircraft.id == aircraft_id).first()
    if not aircraft:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Aircraft profile with ID {aircraft_id} not found."
        )
    
    aircraft.temp_coeff = coefficients.temp_coeff
    aircraft.wind_coeff = coefficients.wind_coeff
    aircraft.alt_coeff = coefficients.alt_coeff
    
    try:
        db.commit()
        db.refresh(aircraft)
        return aircraft
    except Exception as e:
        db.rollback()
        logger.error(f"Error updating coefficients: {e}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="Failed to update coefficients in the database."
        )

@app.put("/api/aircraft/{aircraft_id}", response_model=schemas.AircraftResponse)
def update_aircraft(
    aircraft_id: int,
    aircraft: schemas.AircraftCreate,
    db: Session = Depends(get_db)
):
    db_aircraft = db.query(models.Aircraft).filter(models.Aircraft.id == aircraft_id).first()
    if not db_aircraft:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Aircraft profile with ID {aircraft_id} not found."
        )
    
    # Check if name is taken by another profile
    name_check = db.query(models.Aircraft).filter(
        models.Aircraft.name == aircraft.name,
        models.Aircraft.id != aircraft_id
    ).first()
    if name_check:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail=f"Aircraft profile with name '{aircraft.name}' already exists."
        )
    
    # Update fields
    for key, value in aircraft.model_dump().items():
        setattr(db_aircraft, key, value)
        
    try:
        db.commit()
        db.refresh(db_aircraft)
        return db_aircraft
    except Exception as e:
        db.rollback()
        logger.error(f"Error updating aircraft profile: {e}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="Failed to update aircraft profile."
        )

@app.delete("/api/aircraft/{aircraft_id}", status_code=status.HTTP_204_NO_CONTENT)
def delete_aircraft(aircraft_id: int, db: Session = Depends(get_db)):
    aircraft = db.query(models.Aircraft).filter(models.Aircraft.id == aircraft_id).first()
    if not aircraft:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Aircraft profile with ID {aircraft_id} not found."
        )
    
    try:
        db.delete(aircraft)
        db.commit()
        return None
    except Exception as e:
        db.rollback()
        logger.error(f"Error deleting aircraft profile: {e}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="Failed to delete aircraft profile."
        )


# --- FLIGHT LOG ENDPOINTS ---

@app.post("/api/flight-logs", response_model=schemas.FlightLogResponse, status_code=status.HTTP_201_CREATED)
def create_flight_log(log: schemas.FlightLogCreate, db: Session = Depends(get_db)):
    # Verify aircraft exists
    aircraft = db.query(models.Aircraft).filter(models.Aircraft.id == log.aircraft_id).first()
    if not aircraft:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Cannot log flight. Aircraft with ID {log.aircraft_id} does not exist."
        )
    
    new_log = models.FlightLog(**log.model_dump())
    db.add(new_log)
    try:
        db.commit()
        db.refresh(new_log)
        return new_log
    except Exception as e:
        db.rollback()
        logger.error(f"Error logging flight: {e}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="Failed to record flight log."
        )

@app.get("/api/flight-logs/{aircraft_id}", response_model=List[schemas.FlightLogResponse])
def list_flight_logs(aircraft_id: int, db: Session = Depends(get_db)):
    # Verify aircraft exists
    aircraft = db.query(models.Aircraft).filter(models.Aircraft.id == aircraft_id).first()
    if not aircraft:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Aircraft profile with ID {aircraft_id} not found."
        )
    
    return db.query(models.FlightLog).filter(
        models.FlightLog.aircraft_id == aircraft_id
    ).order_by(models.FlightLog.flight_date.desc(), models.FlightLog.created_at.desc()).all()

@app.delete("/api/flight-logs/{log_id}", status_code=status.HTTP_204_NO_CONTENT)
def delete_flight_log(log_id: int, db: Session = Depends(get_db)):
    log = db.query(models.FlightLog).filter(models.FlightLog.id == log_id).first()
    if not log:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Flight log with ID {log_id} not found."
        )
    
    try:
        db.delete(log)
        db.commit()
        return None
    except Exception as e:
        db.rollback()
        logger.error(f"Error deleting flight log: {e}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="Failed to delete flight log."
        )


# --- WEATHER ENDPOINT ---

@app.get("/api/weather", response_model=schemas.WeatherResponse)
async def get_weather(
    q: Optional[str] = Query(None, description="Location place name"),
    lat: Optional[float] = Query(None, description="Latitude"),
    lon: Optional[float] = Query(None, description="Longitude"),
    db: Session = Depends(get_db)
):
    if q:
        try:
            return await weather.fetch_weather_by_name(db, q)
        except ValueError as ve:
            raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail=str(ve))
        except RuntimeError as re:
            raise HTTPException(status_code=status.HTTP_503_SERVICE_UNAVAILABLE, detail=str(re))
    elif lat is not None and lon is not None:
        try:
            return await weather.fetch_weather_data(db, latitude=lat, longitude=lon)
        except RuntimeError as re:
            raise HTTPException(status_code=status.HTTP_533_SERVICE_UNAVAILABLE if hasattr(status, 'HTTP_533_SERVICE_UNAVAILABLE') else status.HTTP_503_SERVICE_UNAVAILABLE, detail=str(re))
    else:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="Please provide a location name 'q' or coordinate pair 'lat' and 'lon'."
        )


# --- ESTIMATION ENDPOINT ---

@app.post("/api/estimate", response_model=schemas.EstimationResponse)
def estimate_flight_endurance(request: schemas.EstimationRequest):
    try:
        result = physics.run_estimation_model(request.model_dump())
        return result
    except Exception as e:
        logger.error(f"Error in endurance calculation: {e}")
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"An error occurred during math estimation: {str(e)}"
        )


# --- CSV EXPORT ENDPOINTS ---

@app.get("/api/export/csv")
def export_flight_logs_csv(aircraft_id: int, db: Session = Depends(get_db)):
    aircraft = db.query(models.Aircraft).filter(models.Aircraft.id == aircraft_id).first()
    if not aircraft:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"Aircraft profile with ID {aircraft_id} not found."
        )
        
    logs = db.query(models.FlightLog).filter(
        models.FlightLog.aircraft_id == aircraft_id
    ).order_by(models.FlightLog.flight_date.desc()).all()
    
    output = io.StringIO()
    writer = csv.writer(output)
    
    # Headers
    writer.writerow([
        "Aircraft ID", "Aircraft Name", "Flight Date", "Actual Flight Time (min)", 
        "Payload (kg)", "Temperature (C)", "Wind Speed (km/h)", 
        "Altitude MSL (m)", "Humidity (%)", "Notes"
    ])
    
    for log in logs:
        writer.writerow([
            aircraft.id, aircraft.name, log.flight_date, log.actual_flight_time,
            log.payload, log.temperature, log.wind_speed, log.altitude,
            log.humidity, log.notes or ""
        ])
        
    output.seek(0)
    
    headers = {
        'Content-Disposition': f'attachment; filename="flight_logs_{aircraft.name.replace(" ", "_")}.csv"'
    }
    return StreamingResponse(
        iter([output.getvalue()]), 
        media_type="text/csv", 
        headers=headers
    )

@app.get("/api/export/session-csv")
def export_session_csv(
    empty_weight: float,
    payload: float,
    battery_capacity: int,
    battery_cells: int,
    battery_reserve: float,
    battery_efficiency: float,
    motors: int,
    hover_current: float,
    cruise_speed: Optional[float] = None,
    temperature: float = 20.0,
    wind_speed: float = 0.0,
    altitude: float = 0.0,
    humidity: float = 50.0,
    temp_coeff: float = 1.0,
    wind_coeff: float = 1.0,
    alt_coeff: float = 1.0
):
    inputs = {
        "empty_weight": empty_weight,
        "payload": payload,
        "battery_capacity": battery_capacity,
        "battery_reserve": battery_reserve,
        "battery_efficiency": battery_efficiency,
        "motors": motors,
        "hover_current": hover_current,
        "cruise_speed": cruise_speed,
        "temperature": temperature,
        "wind_speed": wind_speed,
        "altitude": altitude,
        "temp_coeff": temp_coeff,
        "wind_coeff": wind_coeff,
        "alt_coeff": alt_coeff
    }
    
    try:
        result = physics.run_estimation_model(inputs)
    except Exception as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Failed to calculate results for export: {str(e)}"
        )
    
    output = io.StringIO()
    writer = csv.writer(output)
    
    # Write session details
    writer.writerow(["AERO-GCS FLIGHT ENDURANCE ESTIMATION REPORT"])
    writer.writerow([])
    writer.writerow(["--- CONFIGURATION SPECIFICATIONS ---"])
    writer.writerow(["Aircraft Weight Empty (kg)", empty_weight])
    writer.writerow(["Payload Weight (kg)", payload])
    writer.writerow(["Total Flight Weight (kg)", empty_weight + payload])
    writer.writerow(["Motors Count", motors])
    writer.writerow(["Baseline Hover Current (A/motor)", hover_current])
    writer.writerow(["Loaded Hover Current (A/motor)", result["hover_current_loaded"]])
    writer.writerow(["Battery Capacity (mAh)", battery_capacity])
    writer.writerow(["Battery Cells (S)", battery_cells])
    writer.writerow(["Land-With Reserve (%)", battery_reserve])
    writer.writerow(["Discharge Efficiency (%)", battery_efficiency])
    if cruise_speed:
        writer.writerow(["Target Cruise Speed (km/h)", cruise_speed])
    
    writer.writerow([])
    writer.writerow(["--- ENVIRONMENTAL PARAMETERS ---"])
    writer.writerow(["Temperature (°C)", temperature])
    writer.writerow(["Wind Speed (km/h)", wind_speed])
    writer.writerow(["Site Altitude MSL (m)", altitude])
    writer.writerow(["Relative Humidity (%)", humidity])
    
    writer.writerow([])
    writer.writerow(["--- MODEL SENSITIVITY COEFFICIENTS ---"])
    writer.writerow(["Temperature Derate Coeff", temp_coeff])
    writer.writerow(["Wind Penalty Coeff", wind_coeff])
    writer.writerow(["Altitude Penalty Coeff", alt_coeff])
    
    writer.writerow([])
    writer.writerow(["--- DERATING WATERFALL DIAGNOSTICS ---"])
    writer.writerow(["Stage Description", "Flight Time Limit (min)", "Stage Multiplier Applied"])
    writer.writerow(["1. Nameplate Flight Time", result["nameplate_time_min"], 1.0])
    
    # Usable fraction multiplier
    usable_mult = (1.0 - (battery_reserve / 100.0)) * (battery_efficiency / 100.0)
    writer.writerow(["2. Usable Battery Limit", result["usable_time_min"], round(usable_mult, 3)])
    writer.writerow(["3. Temperature Derated", result["temp_derated_time_min"], result["temp_derate_factor"]])
    writer.writerow(["4. Wind Adjusted", result["wind_adjusted_time_min"], round(1.0 / result["wind_penalty_factor"], 3)])
    writer.writerow(["5. Density Altitude (FINAL)", result["final_estimated_time_min"], round(1.0 / result["altitude_penalty_factor"], 3)])
    
    if result["estimated_range_km"] is not None:
        writer.writerow([])
        writer.writerow(["Estimated Cruise Range (km)", result["estimated_range_km"]])
        
    output.seek(0)
    
    headers = {
        'Content-Disposition': 'attachment; filename="flight_ops_estimation_report.csv"'
    }
    return StreamingResponse(
        iter([output.getvalue()]), 
        media_type="text/csv", 
        headers=headers
    )


# Serve Frontend SPA
# To allow routing to work, we make sure StaticFiles is mounted *after* all API paths.
# If files don't exist in frontend/ yet, FastAPI will start, but requests to "/" will error.
# This is fine, we will create the frontend immediately after.
frontend_path = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "frontend"))
if os.path.exists(frontend_path):
    app.mount("/", StaticFiles(directory=frontend_path, html=True), name="frontend")
else:
    logger.warning(f"Frontend directory '{frontend_path}' not found yet. App is running API-only mode.")
