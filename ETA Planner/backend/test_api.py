import httpx
import sys

BASE_URL = "http://127.0.0.1:7006"

def run_flight_model_test():
    print("==================================================")
    print(" RUNNING INTEGRATION TESTS FOR API ON PORT 7006  ")
    print("==================================================")
    
    # 1. Create/Save aircraft profile in database
    aircraft_payload = {
        "name": "DJI Matrice 300 RTK Integration Test",
        "empty_weight": 6.3,
        "motors": 4,
        "hover_current": 12.0,
        "prop_size": "21x10",
        "battery_capacity": 5850,
        "battery_cells": 12,
        "battery_reserve": 20,
        "battery_efficiency": 90,
        "cruise_speed": 60
    }
    
    try:
        print("[INFO] Creating aircraft profile...")
        create_resp = httpx.post(f"{BASE_URL}/api/aircraft", json=aircraft_payload)
        
        # If it already exists (e.g. from previous run), let's get the list and delete/recreate it
        if create_resp.status_code == 400:
            print("[INFO] Profile name exists, fetching profiles to find ID...")
            list_resp = httpx.get(f"{BASE_URL}/api/aircraft")
            profiles = list_resp.json()
            existing_id = next((ac["id"] for ac in profiles if ac["name"] == aircraft_payload["name"]), None)
            if existing_id:
                print(f"[INFO] Deleting existing profile ID: {existing_id} for clean run...")
                httpx.delete(f"{BASE_URL}/api/aircraft/{existing_id}")
                create_resp = httpx.post(f"{BASE_URL}/api/aircraft", json=aircraft_payload)
        
        assert create_resp.status_code == 201, f"Failed profile creation: {create_resp.text}"
        aircraft_id = create_resp.json()["id"]
        print(f"[PASS] Profile saved successfully with ID: {aircraft_id}")
        
        # 2. Request flight endurance estimate under standard atmospheric conditions
        estimation_payload = {
            "aircraft_id": aircraft_id,
            "empty_weight": 6.3,
            "payload": 1.0,
            "battery_capacity": 5850,
            "battery_cells": 12,
            "battery_reserve": 20,
            "battery_efficiency": 90,
            "motors": 4,
            "hover_current": 12.0,
            "cruise_speed": 60,
            "temperature": 15.0,
            "wind_speed": 0.0,
            "altitude": 0.0,
            "humidity": 50.0,
            "temp_coeff": 1.0,
            "wind_coeff": 1.0,
            "alt_coeff": 1.0
        }
        
        print("[INFO] Querying /api/estimate endpoint...")
        est_resp = httpx.post(f"{BASE_URL}/api/estimate", json=estimation_payload)
        assert est_resp.status_code == 200, f"Estimation request failed: {est_resp.text}"
        
        data = est_resp.json()
        print("[INFO] API Estimation Results:")
        print(f"  - Final Estimated Time: {data['final_estimated_time_min']} mins")
        print(f"  - Loaded Hover Current: {data['hover_current_loaded']} A/motor")
        print(f"  - Cruise Range: {data['estimated_range_km']} km")
        print(f"  - Number of Advisories: {len(data['advisories'])}")
        
        # Assert values against standard validation criteria matching the UI
        # 4.08 min is expected under standard atmospheric conditions with 1.0kg payload
        assert abs(data["final_estimated_time_min"] - 4.08) < 0.1, f"Expected ~4.08 min, got {data['final_estimated_time_min']}"
        assert abs(data["hover_current_loaded"] - 14.53) < 0.1, f"Expected ~14.53 A, got {data['hover_current_loaded']}"
        assert abs(data["estimated_range_km"] - 4.08) < 0.1, f"Expected ~4.1 km, got {data['estimated_range_km']}"
        
        # Cleanup
        print("[INFO] Cleaning up test profile...")
        httpx.delete(f"{BASE_URL}/api/aircraft/{aircraft_id}")
        
        print("==================================================")
        print("    SUCCESS: API INTEGRATION TESTS PASSED!        ")
        print("==================================================")
        
    except Exception as e:
        print(f"[FAIL] Integration test failed: {e}")
        sys.exit(1)

if __name__ == "__main__":
    run_flight_model_test()
