import datetime
from sqlalchemy import Column, Integer, String, Float, DateTime, ForeignKey
from sqlalchemy.orm import relationship
from sqlalchemy.sql import func
from backend.database import Base

class Aircraft(Base):
    __tablename__ = "aircraft"

    id = Column(Integer, primary_key=True, index=True, autoincrement=True)
    name = Column(String, unique=True, index=True, nullable=False)
    empty_weight = Column(Float, nullable=False)  # in kg
    motors = Column(Integer, nullable=False)
    hover_current = Column(Float, nullable=False)  # in A (per motor)
    prop_size = Column(String, nullable=True)      # e.g., "15x5.2"
    battery_capacity = Column(Integer, nullable=False)  # in mAh
    battery_cells = Column(Integer, nullable=False)     # e.g., 6 (for 6S)
    battery_reserve = Column(Float, nullable=False, default=20.0)  # in % (land with reserve)
    battery_efficiency = Column(Float, nullable=False, default=85.0)  # in % (discharge efficiency)
    cruise_speed = Column(Float, nullable=True)     # in km/h (optional)
    
    # Tuning coefficients
    temp_coeff = Column(Float, nullable=False, default=1.0)
    wind_coeff = Column(Float, nullable=False, default=1.0)
    alt_coeff = Column(Float, nullable=False, default=1.0)
    
    created_at = Column(DateTime(timezone=True), server_default=func.now())

    # Relationship to flight logs
    logs = relationship("FlightLog", back_populates="aircraft", cascade="all, delete-orphan")


class FlightLog(Base):
    __tablename__ = "flight_log"

    id = Column(Integer, primary_key=True, index=True, autoincrement=True)
    aircraft_id = Column(Integer, ForeignKey("aircraft.id", ondelete="CASCADE"), nullable=False)
    flight_date = Column(String, nullable=False)  # YYYY-MM-DD
    actual_flight_time = Column(Float, nullable=False)  # in minutes
    payload = Column(Float, nullable=False, default=0.0)  # in kg
    temperature = Column(Float, nullable=False)  # in °C
    wind_speed = Column(Float, nullable=False)   # in km/h
    altitude = Column(Float, nullable=False)     # in meters (MSL)
    humidity = Column(Float, nullable=False, default=50.0)  # in %
    notes = Column(String, nullable=True)
    
    created_at = Column(DateTime(timezone=True), server_default=func.now())

    # Relationship to aircraft
    aircraft = relationship("Aircraft", back_populates="logs")


class WeatherCache(Base):
    __tablename__ = "weather_cache"

    key = Column(String, primary_key=True, index=True, default="last_fetched")
    location_name = Column(String, nullable=False)
    latitude = Column(Float, nullable=False)
    longitude = Column(Float, nullable=False)
    temperature = Column(Float, nullable=False)  # °C
    wind_speed = Column(Float, nullable=False)   # km/h
    elevation = Column(Float, nullable=False)     # m
    humidity = Column(Float, nullable=False)      # %
    timestamp = Column(DateTime(timezone=True), server_default=func.now(), onupdate=func.now())
