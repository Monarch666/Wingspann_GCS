import math
from typing import Dict, Any, List

def calculate_temp_derate(t: float) -> float:
    """
    Returns LiPo usable capacity multiplier based on temperature in °C.
    - 50% usable at -20°C
    - 100% in 20-35°C band
    - tapering to 85% by 55°C
    """
    if t <= -20:
        return 0.50
    elif t < 20:
        # Interpolate between -20°C (0.50) and 20°C (1.00)
        return 0.50 + 0.50 * (t - (-20)) / (20 - (-20))
    elif t <= 35:
        return 1.00
    elif t <= 55:
        # Interpolate between 35°C (1.00) and 55°C (0.85)
        return 1.00 - 0.15 * (t - 35) / (55 - 35)
    else:
        # Tapering capped at 0.85 above 55°C (due to thermal limiting)
        return 0.85

def calculate_wind_penalty(w: float) -> float:
    """
    Returns motor current draw multiplier based on wind speed in km/h.
    - Scales up roughly linearly above 10 km/h
    - Caps at 1.65x around 50 km/h
    """
    if w <= 10:
        return 1.0
    elif w <= 50:
        # Interpolate between 10 km/h (1.00) and 50 km/h (1.65)
        return 1.0 + 0.65 * (w - 10) / (50 - 10)
    else:
        return 1.65

def calculate_altitude_penalty(altitude_m: float) -> float:
    """
    Returns motor current draw multiplier based on altitude in meters.
    - Uses barometric density formula: density_ratio = exp(-altitude/8500)
    - Power/current scales as 1/sqrt(density_ratio) = exp(altitude/17000)
    """
    density_ratio = math.exp(-altitude_m / 8500.0)
    # Avoid division by zero
    if density_ratio <= 0:
        density_ratio = 0.0001
    return 1.0 / math.sqrt(density_ratio)

def run_estimation_model(inputs: Dict[str, Any]) -> Dict[str, Any]:
    # Extract inputs
    empty_weight = inputs["empty_weight"]
    payload = inputs["payload"]
    battery_capacity = inputs["battery_capacity"]
    battery_reserve = inputs["battery_reserve"]
    battery_efficiency = inputs["battery_efficiency"]
    motors = inputs["motors"]
    hover_current_empty = inputs["hover_current"]
    cruise_speed = inputs.get("cruise_speed")
    
    temperature = inputs["temperature"]
    wind_speed = inputs["wind_speed"]
    altitude = inputs["altitude"]
    
    # Extract tuning coefficients (default to 1.0 if not provided)
    temp_coeff = inputs.get("temp_coeff", 1.0)
    wind_coeff = inputs.get("wind_coeff", 1.0)
    alt_coeff = inputs.get("alt_coeff", 1.0)

    # 1. Payload current scaling (aerodynamic momentum theory model)
    total_weight = empty_weight + payload
    if empty_weight > 0:
        weight_ratio = total_weight / empty_weight
        # Power and current scale non-linearly with weight (exponent 1.3 - 1.5)
        hover_current_loaded = hover_current_empty * (weight_ratio ** 1.3)
    else:
        hover_current_loaded = hover_current_empty

    # 2. Nameplate hover time
    # (capacity_mAh / 1000) / (motors * hover_current) * 60 minutes
    nameplate_time_min = (battery_capacity / 1000.0) / (motors * hover_current_loaded) * 60.0

    # 3. Usable capacity limit (Reserve and Efficiency)
    # usable_fraction = (1 - reserve/100) * (efficiency/100)
    usable_fraction = (1.0 - (battery_reserve / 100.0)) * (battery_efficiency / 100.0)
    usable_time_min = nameplate_time_min * usable_fraction

    # 4. Temperature derating
    temp_derate_raw = calculate_temp_derate(temperature)
    # Apply sensitivity tuning coefficient
    # If temp_coeff=1.0, same as raw. If 0.0, no derate (factor=1.0). If 2.0, double the penalty.
    temp_derate_factor = 1.0 - (1.0 - temp_derate_raw) * temp_coeff
    temp_derate_factor = max(0.1, min(1.0, temp_derate_factor))  # Clamp between 10% and 100%
    temp_derated_time_min = usable_time_min * temp_derate_factor

    # 5. Wind penalty
    wind_penalty_raw = calculate_wind_penalty(wind_speed)
    # Apply sensitivity tuning coefficient
    wind_penalty_factor = 1.0 + (wind_penalty_raw - 1.0) * wind_coeff
    wind_penalty_factor = max(1.0, wind_penalty_factor)
    wind_adjusted_time_min = temp_derated_time_min / wind_penalty_factor

    # 6. Altitude penalty
    alt_penalty_raw = calculate_altitude_penalty(altitude)
    # Apply sensitivity tuning coefficient
    altitude_penalty_factor = 1.0 + (alt_penalty_raw - 1.0) * alt_coeff
    altitude_penalty_factor = max(0.5, altitude_penalty_factor)
    final_estimated_time_min = wind_adjusted_time_min / altitude_penalty_factor

    # Range calculation if cruise speed is provided
    estimated_range_km = None
    if cruise_speed is not None and cruise_speed > 0:
        # range = (time_in_minutes / 60) * speed_km_h
        estimated_range_km = (final_estimated_time_min / 60.0) * cruise_speed

    # Advisories generation
    advisories = []
    
    if wind_speed > 25.0 and wind_speed <= 40.0:
        advisories.append({
            "level": "warning",
            "message": f"High wind warning ({wind_speed} km/h). Increased drag and stabilization current will severely reduce flight stability and range."
        })
    elif wind_speed > 40.0:
        advisories.append({
            "level": "critical",
            "message": f"Extreme wind advisories ({wind_speed} km/h). Safe flight operation is unlikely. High risk of motor thermal overload or loss of control."
        })

    if temperature < 0.0:
        advisories.append({
            "level": "warning",
            "message": f"Sub-zero battery temperature ({temperature}°C). LiPo cells will suffer high internal resistance and voltage sag. Pre-heat batteries before takeoff."
        })
    elif temperature > 45.0:
        advisories.append({
            "level": "warning",
            "message": f"High ambient temperature ({temperature}°C). Reduced motor and ESC cooling efficiency. Monitor component temperatures closely."
        })

    if altitude > 3000.0:
        advisories.append({
            "level": "warning",
            "message": f"High altitude takeoff ({altitude}m). Thinner air reduces propeller lift efficiency, requiring higher hover RPM and current."
        })

    if final_estimated_time_min < 3.0:
        advisories.append({
            "level": "critical",
            "message": f"Critical flight endurance limit ({final_estimated_time_min:.1f} minutes estimated). Flight time is insufficient for safe operations, leaving almost no margin for return-to-home."
        })

    return {
        "nameplate_time_min": round(nameplate_time_min, 2),
        "usable_time_min": round(usable_time_min, 2),
        "temp_derated_time_min": round(temp_derated_time_min, 2),
        "wind_adjusted_time_min": round(wind_adjusted_time_min, 2),
        "final_estimated_time_min": round(final_estimated_time_min, 2),
        "estimated_range_km": round(estimated_range_km, 2) if estimated_range_km is not None else None,
        "hover_current_loaded": round(hover_current_loaded, 2),
        "temp_derate_factor": round(temp_derate_factor, 3),
        "wind_penalty_factor": round(wind_penalty_factor, 3),
        "altitude_penalty_factor": round(altitude_penalty_factor, 3),
        "advisories": advisories
    }
