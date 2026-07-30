import math
from backend.physics import (
    calculate_temp_derate,
    calculate_wind_penalty,
    calculate_altitude_penalty,
    run_estimation_model
)

def test_lipo_temperature_curve():
    print("Testing LiPo Temperature Derating Curve...")
    
    # Standard cases
    assert calculate_temp_derate(-25) == 0.50, "Capped at 50% for extreme cold"
    assert calculate_temp_derate(-20) == 0.50, "50% derate at -20°C"
    assert calculate_temp_derate(20) == 1.00, "100% capacity in comfortable band"
    assert calculate_temp_derate(30) == 1.00, "100% capacity in comfortable band"
    assert calculate_temp_derate(55) == 0.85, "85% capacity at 55°C due to thermals"
    assert calculate_temp_derate(60) == 0.85, "Capped at 85% for high thermal limit"
    
    # Interpolation checks
    # At 0°C, should be exactly halfway between 0.50 (at -20°C) and 1.00 (at 20°C) -> 0.75
    assert abs(calculate_temp_derate(0) - 0.75) < 1e-5, f"0°C should be 0.75, got {calculate_temp_derate(0)}"
    
    # At 45°C, should be exactly halfway between 1.00 (at 35°C) and 0.85 (at 55°C) -> 0.925
    assert abs(calculate_temp_derate(45) - 0.925) < 1e-5, f"45°C should be 0.925, got {calculate_temp_derate(45)}"
    
    print("[PASS] LiPo Temperature curves verified.")

def test_wind_penalty_curve():
    print("Testing Wind Drag Penalty Curve...")
    
    assert calculate_wind_penalty(0) == 1.00, "No penalty for calm wind"
    assert calculate_wind_penalty(10) == 1.00, "No penalty up to light-breeze limit (10 km/h)"
    assert calculate_wind_penalty(50) == 1.65, "Max penalty capped at 1.65x for 50 km/h"
    assert calculate_wind_penalty(60) == 1.65, "Capped at 1.65x for extreme wind (> 50 km/h)"
    
    # Interpolation checks
    # At 30 km/h, should be exactly halfway between 1.00 (at 10 km/h) and 1.65 (at 50 km/h) -> 1.325
    assert abs(calculate_wind_penalty(30) - 1.325) < 1e-5, f"30 km/h wind should scale current draw by 1.325x, got {calculate_wind_penalty(30)}"
    
    print("[PASS] Wind Drag penalties verified.")

def test_altitude_density_penalty():
    print("Testing Density Altitude Penalty Curve...")
    
    # Sea level (0m) -> 1.0x current draw
    assert abs(calculate_altitude_penalty(0) - 1.0) < 1e-5
    
    # At 8500m, density ratio is exp(-1) = 0.3678.
    # Current scales as 1/sqrt(density_ratio) = 1/sqrt(exp(-1)) = exp(0.5) ≈ 1.6487
    expected_8500 = math.exp(8500 / 17000)
    assert abs(calculate_altitude_penalty(8500) - expected_8500) < 1e-5
    
    print("[PASS] Density Altitude scaling verified.")

def test_full_model():
    print("Testing Full Estimation Model Calculations...")
    
    inputs = {
        "empty_weight": 2.0,       # kg
        "payload": 0.5,            # kg (total weight = 2.5 kg)
        "battery_capacity": 10000, # mAh (10 Ah)
        "battery_reserve": 20.0,   # % (land-with)
        "battery_efficiency": 90.0,# % (discharge efficiency)
        "motors": 4,
        "hover_current": 5.0,      # A per motor at empty weight
        "cruise_speed": 40.0,      # km/h
        
        "temperature": 20.0,       # °C (no temp derate)
        "wind_speed": 10.0,        # km/h (no wind penalty)
        "altitude": 0.0,           # m (no altitude penalty)
        
        "temp_coeff": 1.0,
        "wind_coeff": 1.0,
        "alt_coeff": 1.0
    }
    
    # Manual verification math:
    # weight_ratio = 2.5 / 2.0 = 1.25
    # loaded_hover_current = 5.0 * (1.25 ** 1.3) ≈ 5.0 * 1.33939 ≈ 6.6969 A per motor
    # total_current = 4 * 6.6969 ≈ 26.7877 A
    # nameplate_time = 10.0 / 26.7877 * 60 ≈ 22.398 min
    # usable_time = 22.398 * (1 - 0.20) * 0.90 = 22.398 * 0.72 ≈ 16.126 min
    
    result = run_estimation_model(inputs)
    
    # Assert values
    assert abs(result["hover_current_loaded"] - 6.70) < 0.05, f"Loaded current got {result['hover_current_loaded']}"
    assert abs(result["nameplate_time_min"] - 22.40) < 0.2, f"Nameplate time got {result['nameplate_time_min']}"
    assert abs(result["final_estimated_time_min"] - 16.13) < 0.2, f"Final estimated time got {result['final_estimated_time_min']}"
    
    # Cruise speed range: 16.13 minutes @ 40 km/h -> (16.13 / 60) * 40 ≈ 10.75 km
    assert abs(result["estimated_range_km"] - 10.75) < 0.2, f"Estimated range got {result['estimated_range_km']}"
    
    print("[PASS] Full estimation model math verified.")

def test_sensitivity_coefficients():
    print("Testing Tuning Coefficient Effects...")
    
    inputs = {
        "empty_weight": 2.0,
        "payload": 0.0,
        "battery_capacity": 10000,
        "battery_reserve": 20.0,
        "battery_efficiency": 100.0,
        "motors": 4,
        "hover_current": 5.0,
        
        # Harsh environment
        "temperature": 0.0,    # standard derate = 0.75
        "wind_speed": 30.0,    # standard penalty = 1.325
        "altitude": 1700.0,    # standard penalty = exp(1700/17000) = exp(0.1) ≈ 1.105
        
        # Custom sensitivity tuning coefficients
        "temp_coeff": 0.5,     # temp derate sensitivity halved
        "wind_coeff": 1.5,     # wind penalty sensitivity increased by 50%
        "alt_coeff": 0.0       # altitude penalty ignored
    }
    
    # Calculations:
    # 1. nameplate_time = 10.0 / 20.0 * 60 = 30 min
    # 2. usable_time = 30 * 0.8 * 1.0 = 24 min
    # 3. temp derate: raw = 0.75. Adj = 1.0 - (1.0 - 0.75) * 0.5 = 0.875
    #    time after temp = 24 * 0.875 = 21.0 min
    # 4. wind penalty: raw = 1.325. Adj = 1.0 + (1.325 - 1.0) * 1.5 = 1.0 + 0.325 * 1.5 = 1.4875
    #    time after wind = 21.0 / 1.4875 ≈ 14.117 min
    # 5. altitude penalty: raw ≈ 1.105. Adj = 1.0 + (1.105 - 1.0) * 0 = 1.0
    #    time after altitude (final) = 14.117 / 1.0 = 14.117 min
    
    result = run_estimation_model(inputs)
    
    assert abs(result["temp_derate_factor"] - 0.875) < 1e-5
    assert abs(result["wind_penalty_factor"] - 1.488) < 0.01
    assert abs(result["altitude_penalty_factor"] - 1.000) < 1e-5
    assert abs(result["final_estimated_time_min"] - 14.12) < 0.1
    
    print("[PASS] Sensitivity coefficient overrides verified.")

if __name__ == "__main__":
    print("==================================================")
    print(" RUNNING FLIGHT ENDURANCE ESTIMATOR PHYSICS TESTS ")
    print("==================================================")
    test_lipo_temperature_curve()
    test_wind_penalty_curve()
    test_altitude_density_penalty()
    test_full_model()
    test_sensitivity_coefficients()
    print("==================================================")
    print("           ALL TESTS COMPLETED SUCCESS           ")
    print("==================================================")
