import httpx
import logging
from sqlalchemy.orm import Session
from datetime import datetime
from typing import Optional, Dict, Any
from backend.models import WeatherCache

logger = logging.getLogger(__name__)

GEOCODING_API_URL = "https://geocoding-api.open-meteo.com/v1/search"
WEATHER_API_URL = "https://api.open-meteo.com/v1/forecast"

def get_cached_weather(db: Session, key: str = "last_fetched") -> Optional[WeatherCache]:
    """Retrieves cached weather from database."""
    return db.query(WeatherCache).filter(WeatherCache.key == key).first()

def save_weather_to_cache(
    db: Session, 
    location_name: str, 
    latitude: float, 
    longitude: float, 
    temperature: float, 
    wind_speed: float, 
    elevation: float, 
    humidity: float,
    key: str = "last_fetched"
) -> WeatherCache:
    """Saves or updates weather cache in database."""
    cache_entry = db.query(WeatherCache).filter(WeatherCache.key == key).first()
    if not cache_entry:
        cache_entry = WeatherCache(key=key)
        db.add(cache_entry)
    
    cache_entry.location_name = location_name
    cache_entry.latitude = latitude
    cache_entry.longitude = longitude
    cache_entry.temperature = temperature
    cache_entry.wind_speed = wind_speed
    cache_entry.elevation = elevation
    cache_entry.humidity = humidity
    cache_entry.timestamp = datetime.utcnow()
    
    db.commit()
    db.refresh(cache_entry)
    return cache_entry

async def fetch_weather_data(db: Session, latitude: float, longitude: float, location_name: str = "Query Coordinates") -> Dict[str, Any]:
    """
    Fetches weather data from Open-Meteo Forecast API.
    Falls back to cached database reading if API request fails (offline support).
    """
    params = {
        "latitude": latitude,
        "longitude": longitude,
        "current": "temperature_2m,wind_speed_10m,relative_humidity_2m",
        "wind_speed_unit": "kmh"
    }
    
    try:
        async with httpx.AsyncClient(timeout=5.0) as client:
            response = await client.get(WEATHER_API_URL, params=params)
            response.raise_for_status()
            data = response.json()
            
            # Extract data
            current = data.get("current", {})
            temperature = current.get("temperature_2m", 0.0)
            wind_speed = current.get("wind_speed_10m", 0.0)
            humidity = current.get("relative_humidity_2m", 50.0)
            elevation = data.get("elevation", 0.0)
            
            # Save to last_fetched cache
            save_weather_to_cache(
                db, 
                location_name=location_name, 
                latitude=latitude, 
                longitude=longitude, 
                temperature=temperature, 
                wind_speed=wind_speed, 
                elevation=elevation, 
                humidity=humidity,
                key="last_fetched"
            )
            
            # Also save to specific coordinate cache for offline lookups of this coordinate
            coord_key = f"coord:{round(latitude,2)},{round(longitude,2)}"
            save_weather_to_cache(
                db,
                location_name=location_name,
                latitude=latitude,
                longitude=longitude,
                temperature=temperature,
                wind_speed=wind_speed,
                elevation=elevation,
                humidity=humidity,
                key=coord_key
            )
            
            return {
                "location_name": location_name,
                "latitude": latitude,
                "longitude": longitude,
                "temperature": temperature,
                "wind_speed": wind_speed,
                "elevation": elevation,
                "humidity": humidity,
                "cached": False,
                "timestamp": datetime.utcnow()
            }
            
    except Exception as e:
        logger.warning(f"Failed to fetch weather from Open-Meteo API: {e}. Attempting cache recovery.")
        
        # Try specific coord cache first
        coord_key = f"coord:{round(latitude,2)},{round(longitude,2)}"
        cached_entry = get_cached_weather(db, coord_key)
        if not cached_entry:
            # Fallback to absolute last fetched cache
            cached_entry = get_cached_weather(db, "last_fetched")
            
        if cached_entry:
            return {
                "location_name": cached_entry.location_name + " (Cached)",
                "latitude": cached_entry.latitude,
                "longitude": cached_entry.longitude,
                "temperature": cached_entry.temperature,
                "wind_speed": cached_entry.wind_speed,
                "elevation": cached_entry.elevation,
                "humidity": cached_entry.humidity,
                "cached": True,
                "timestamp": cached_entry.timestamp
            }
        
        # If no cache exists, propagate the error
        raise RuntimeError("Weather service is offline and no cached readings are available. Please enter conditions manually.")

async def fetch_weather_by_name(db: Session, name: str) -> Dict[str, Any]:
    """
    Geocodes place name to lat/lon, then fetches weather.
    Falls back to cached database reading if API request fails (offline support).
    """
    clean_name = name.strip()
    name_key = f"name:{clean_name.lower()}"
    
    try:
        async with httpx.AsyncClient(timeout=5.0) as client:
            response = await client.get(
                GEOCODING_API_URL, 
                params={"name": clean_name, "count": 1, "language": "en", "format": "json"}
            )
            response.raise_for_status()
            data = response.json()
            
            results = data.get("results", [])
            if not results:
                raise ValueError(f"Location '{clean_name}' not found.")
                
            loc = results[0]
            lat = loc["latitude"]
            lon = loc["longitude"]
            elevation = loc.get("elevation", 0.0)
            
            # Formulate full location label
            city = loc.get("name", "")
            country = loc.get("country", "")
            admin = loc.get("admin1", "")
            parts = [p for p in [city, admin, country] if p]
            full_name = ", ".join(parts) if parts else clean_name
            
            # Fetch actual weather
            result = await fetch_weather_data(db, latitude=lat, longitude=lon, location_name=full_name)
            
            # Save to place-name cache for future offline queries
            save_weather_to_cache(
                db, 
                location_name=full_name, 
                latitude=lat, 
                longitude=lon, 
                temperature=result["temperature"], 
                wind_speed=result["wind_speed"], 
                elevation=result["elevation"], 
                humidity=result["humidity"],
                key=name_key
            )
            
            return result
            
    except Exception as e:
        logger.warning(f"Geocoding/Weather fetch failed for name '{clean_name}': {e}. Attempting cache recovery.")
        
        # Check name cache
        cached_entry = get_cached_weather(db, name_key)
        if not cached_entry:
            # Try global last_fetched cache
            cached_entry = get_cached_weather(db, "last_fetched")
            
        if cached_entry:
            return {
                "location_name": cached_entry.location_name + " (Cached)",
                "latitude": cached_entry.latitude,
                "longitude": cached_entry.longitude,
                "temperature": cached_entry.temperature,
                "wind_speed": cached_entry.wind_speed,
                "elevation": cached_entry.elevation,
                "humidity": cached_entry.humidity,
                "cached": True,
                "timestamp": cached_entry.timestamp
            }
            
        # Re-raise or return a specific error
        if isinstance(e, ValueError):
            raise e
        raise RuntimeError(f"Could not connect to geocoding/weather service and location '{clean_name}' is not cached.")
