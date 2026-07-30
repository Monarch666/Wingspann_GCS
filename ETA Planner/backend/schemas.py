from pydantic import BaseModel, Field
from typing import Optional, List
from datetime import datetime

# --- Aircraft Schemas ---
class AircraftBase(BaseModel):
    name: str = Field(..., min_length=1, max_length=100)
    empty_weight: float = Field(..., gt=0, description="Empty weight in kg")
    motors: int = Field(..., gt=0, description="Number of motors")
    hover_current: float = Field(..., gt=0, description="Hover current per motor in Amperes at empty weight")
    prop_size: Optional[str] = Field(None, description="Propeller size description, e.g., '15x5.2'")
    battery_capacity: int = Field(..., gt=0, description="Battery capacity in mAh")
    battery_cells: int = Field(..., gt=0, description="Cell count S")
    battery_reserve: float = Field(20.0, ge=0, le=100, description="Reserve percentage to land with")
    battery_efficiency: float = Field(85.0, gt=0, le=100, description="Discharge efficiency percentage")
    cruise_speed: Optional[float] = Field(None, ge=0, description="Cruise speed in km/h (optional)")

class AircraftCreate(AircraftBase):
    pass

class AircraftResponse(AircraftBase):
    id: int
    temp_coeff: float
    wind_coeff: float
    alt_coeff: float
    created_at: datetime

    class Config:
        from_attributes = True

class AircraftUpdateCoefficients(BaseModel):
    temp_coeff: float = Field(..., ge=0.0, le=5.0)
    wind_coeff: float = Field(..., ge=0.0, le=5.0)
    alt_coeff: float = Field(..., ge=0.0, le=5.0)


# --- Flight Log Schemas ---
class FlightLogBase(BaseModel):
    aircraft_id: int
    flight_date: str = Field(..., pattern=r"^\d{4}-\d{2}-\d{2}$", description="Flight date in YYYY-MM-DD format")
    actual_flight_time: float = Field(..., gt=0, description="Measured flight time in minutes")
    payload: float = Field(0.0, ge=0, description="Payload weight in kg")
    temperature: float = Field(..., ge=-50, le=70, description="Ambient temperature in °C")
    wind_speed: float = Field(..., ge=0, le=150, description="Average wind speed in km/h")
    altitude: float = Field(..., ge=-100, le=10000, description="Takeoff site altitude MSL in meters")
    humidity: float = Field(50.0, ge=0, le=100, description="Relative humidity in %")
    notes: Optional[str] = None

class FlightLogCreate(FlightLogBase):
    pass

class FlightLogResponse(FlightLogBase):
    id: int
    created_at: datetime

    class Config:
        from_attributes = True


# --- Estimation Schemas ---
class EstimationRequest(BaseModel):
    aircraft_id: Optional[int] = None
    empty_weight: float = Field(..., gt=0)
    payload: float = Field(0.0, ge=0)
    battery_capacity: int = Field(..., gt=0)
    battery_cells: int = Field(..., gt=0)
    battery_reserve: float = Field(20.0, ge=0, le=100)
    battery_efficiency: float = Field(85.0, gt=0, le=100)
    motors: int = Field(..., gt=0)
    hover_current: float = Field(..., gt=0)
    cruise_speed: Optional[float] = Field(None, ge=0)
    
    # Environment
    temperature: float = Field(..., ge=-50, le=70)
    wind_speed: float = Field(..., ge=0, le=150)
    altitude: float = Field(..., ge=-100, le=10000)
    humidity: float = Field(50.0, ge=0, le=100)

    # Tuning Coefficients
    temp_coeff: float = Field(1.0, ge=0.0, le=5.0)
    wind_coeff: float = Field(1.0, ge=0.0, le=5.0)
    alt_coeff: float = Field(1.0, ge=0.0, le=5.0)

class AdvisoryItem(BaseModel):
    level: str  # "warning" or "critical"
    message: str

class EstimationResponse(BaseModel):
    # Calculated Waterfall Values
    nameplate_time_min: float
    usable_time_min: float
    temp_derated_time_min: float
    wind_adjusted_time_min: float
    final_estimated_time_min: float
    
    # Other Outputs
    estimated_range_km: Optional[float] = None
    hover_current_loaded: float
    
    # Derating factors for display
    temp_derate_factor: float
    wind_penalty_factor: float
    altitude_penalty_factor: float
    
    advisories: List[AdvisoryItem]


# --- Weather Cache Schemas ---
class WeatherResponse(BaseModel):
    location_name: str
    latitude: float
    longitude: float
    temperature: float
    wind_speed: float
    elevation: float
    humidity: float
    cached: bool
    timestamp: datetime

    class Config:
        from_attributes = True
