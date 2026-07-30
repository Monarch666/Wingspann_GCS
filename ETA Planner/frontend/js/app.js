// AERO-GCS Dashboard Controller (ES5 Compatible - DJI Pilot 2 / QGC Style)

document.addEventListener('DOMContentLoaded', function() {
    // STATE VARIABLES
    var aircraftList = [];
    var selectedAircraft = null;
    var flightLogs = [];
    
    var currentTuningCoeffs = { temp_coeff: 1.0, wind_coeff: 1.0, alt_coeff: 1.0 };
    var databaseCoeffs = { temp_coeff: 1.0, wind_coeff: 1.0, alt_coeff: 1.0 };

    // DOM ELEMENTS
    var systemTimeEl = document.getElementById('system-time');
    var connectionStatusEl = document.getElementById('connection-status');
    
    var aircraftSelect = document.getElementById('aircraft-select');
    var aircraftForm = document.getElementById('aircraft-form');
    var acNameInput = document.getElementById('ac-name');
    var acEmptyWeightInput = document.getElementById('ac-empty-weight');
    var acMotorsInput = document.getElementById('ac-motors');
    var acHoverCurrentInput = document.getElementById('ac-hover-current');
    var acPropSizeInput = document.getElementById('ac-prop-size');
    var acBatteryCapacityInput = document.getElementById('ac-battery-capacity');
    var acBatteryCellsInput = document.getElementById('ac-battery-cells');
    var acBatteryReserveInput = document.getElementById('ac-battery-reserve');
    var acBatteryEfficiencyInput = document.getElementById('ac-battery-efficiency');
    var acCruiseSpeedInput = document.getElementById('ac-cruise-speed');
    var acPayloadInput = document.getElementById('ac-payload');
    
    var btnSaveProfile = document.getElementById('btn-save-profile');
    var btnDeleteProfile = document.getElementById('btn-delete-profile');

    // Weather Sync & Sliders
    var weatherSearchInput = document.getElementById('weather-search');
    var btnSyncWeather = document.getElementById('btn-sync-weather');
    var weatherSyncIndicator = document.getElementById('weather-sync-indicator');

    var envTempSlider = document.getElementById('env-temp');
    var envWindSlider = document.getElementById('env-wind');
    var envAltitudeSlider = document.getElementById('env-altitude');
    var envHumiditySlider = document.getElementById('env-humidity');

    var valTemp = document.getElementById('val-temp');
    var valWind = document.getElementById('val-wind');
    var valAltitude = document.getElementById('val-altitude');
    var valHumidity = document.getElementById('val-humidity');

    // HUD Elements
    var hudTimer = document.getElementById('hud-timer');
    var hudWeight = document.getElementById('hud-weight');
    var hudRange = document.getElementById('hud-range');
    var hudCurrent = document.getElementById('hud-current');
    var hudAlt = document.getElementById('hud-alt');
    var hudWind = document.getElementById('hud-wind');
    var hudTempDerate = document.getElementById('hud-temp-derate');
    var profileEstTime = document.getElementById('profile-est-time');

    // Battery Card Elements
    var batCapVal = document.getElementById('bat-cap-val');
    var batHoverTime = document.getElementById('bat-hover-time');

    // Diagnostics & Advisories
    var advisoryFeed = document.getElementById('advisory-feed');
    var waterfallDisplay = document.getElementById('waterfall-display');

    // Tuning Sliders
    var coeffTempSlider = document.getElementById('coeff-temp');
    var coeffWindSlider = document.getElementById('coeff-wind');
    var coeffAltSlider = document.getElementById('coeff-alt');

    var valCoeffTemp = document.getElementById('val-coeff-temp');
    var valCoeffWind = document.getElementById('val-coeff-wind');
    var valCoeffAlt = document.getElementById('val-coeff-alt');

    var scatterSvg = document.getElementById('scatter-svg');

    // Export Buttons
    var btnExportSessionCsv = document.getElementById('btn-export-session-csv');
    var btnExportPdf = document.getElementById('btn-export-pdf');

    function init() {
        startClock();
        setupInputEventHandlers();
        loadAircraftProfiles();
        calculateEstimation();
    }

    function startClock() {
        var updateClock = function() {
            var now = new Date();
            if (systemTimeEl) {
                systemTimeEl.textContent = now.toISOString().replace('T', ' ').substring(11, 19) + ' UTC';
            }
        };
        updateClock();
        setInterval(updateClock, 1000);
    }

    function loadAircraftProfiles(selectIdToRestore) {
        return API.getAircrafts().then(function(list) {
            aircraftList = list;
            aircraftSelect.innerHTML = '<option value="">-- Manual Calculation (Unsaved) --</option>';
            for (var ai = 0; ai < aircraftList.length; ai++) {
                var ac = aircraftList[ai];
                var opt = document.createElement('option');
                opt.value = ac.id;
                opt.textContent = ac.name;
                aircraftSelect.appendChild(opt);
            }

            if (selectIdToRestore) {
                aircraftSelect.value = selectIdToRestore;
                var found = null;
                for (var fi = 0; fi < aircraftList.length; fi++) {
                    if (aircraftList[fi].id == selectIdToRestore) {
                        found = aircraftList[fi];
                        break;
                    }
                }
                if (found) {
                    selectedAircraft = found;
                    fillAircraftForm(found);
                }
            } else {
                aircraftSelect.value = "";
                selectedAircraft = null;
            }
            updateProfileButtonsState();
        }).catch(function(error) {
            console.error('Failed to load profiles:', error);
        });
    }

    aircraftSelect.addEventListener('change', function(e) {
        var acId = e.target.value;
        if (!acId) {
            selectedAircraft = null;
            aircraftForm.reset();
            flightLogs = [];
            calculateEstimation();
        } else {
            API.getAircraft(acId).then(function(aircraft) {
                selectedAircraft = aircraft;
                fillAircraftForm(selectedAircraft);
                return API.getFlightLogs(acId).then(function(logs) {
                    flightLogs = logs;
                    renderScatterPlot();
                    calculateEstimation();
                });
            });
        }
        updateProfileButtonsState();
    });

    function fillAircraftForm(ac) {
        acNameInput.value = ac.name;
        acEmptyWeightInput.value = ac.empty_weight.toFixed(2);
        acMotorsInput.value = ac.motors;
        acHoverCurrentInput.value = ac.hover_current.toFixed(1);
        acPropSizeInput.value = ac.prop_size || '15x5.2';
        acBatteryCapacityInput.value = ac.battery_capacity;
        acBatteryCellsInput.value = ac.battery_cells;
        acBatteryReserveInput.value = ac.battery_reserve;
        acBatteryEfficiencyInput.value = ac.battery_efficiency;
        acCruiseSpeedInput.value = ac.cruise_speed !== null ? ac.cruise_speed.toFixed(1) : '45.0';
    }

    function updateProfileButtonsState() {
        var isProfileSelected = !!selectedAircraft;
        btnDeleteProfile.disabled = !isProfileSelected;
        if (isProfileSelected) {
            btnDeleteProfile.classList.remove('btn-disabled');
        } else {
            btnDeleteProfile.classList.add('btn-disabled');
        }
    }

    aircraftForm.addEventListener('submit', function(e) {
        e.preventDefault();
        
        var payload = {
            name: acNameInput.value.trim(),
            empty_weight: parseFloat(acEmptyWeightInput.value),
            motors: parseInt(acMotorsInput.value),
            hover_current: parseFloat(acHoverCurrentInput.value),
            prop_size: acPropSizeInput.value.trim() || null,
            battery_capacity: parseInt(acBatteryCapacityInput.value),
            battery_cells: parseInt(acBatteryCellsInput.value),
            battery_reserve: parseFloat(acBatteryReserveInput.value),
            battery_efficiency: parseFloat(acBatteryEfficiencyInput.value),
            cruise_speed: acCruiseSpeedInput.value ? parseFloat(acCruiseSpeedInput.value) : null
        };

        if (selectedAircraft) {
            fetch(window.location.origin + '/api/aircraft/' + selectedAircraft.id, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            }).then(function(res) { return res.json(); }).then(function(savedAc) {
                loadAircraftProfiles(savedAc.id).then(function() { calculateEstimation(); });
            });
        } else {
            API.createAircraft(payload).then(function(savedAc) {
                loadAircraftProfiles(savedAc.id).then(function() { calculateEstimation(); });
            });
        }
    });

    btnSyncWeather.addEventListener('click', function() {
        var query = weatherSearchInput.value.trim();
        if (!query) return;
        weatherSyncIndicator.textContent = "Connecting weather telemetry...";
        btnSyncWeather.disabled = true;

        API.getWeather(query).then(function(res) {
            envTempSlider.value = res.temperature;
            envWindSlider.value = res.wind_speed;
            envAltitudeSlider.value = Math.min(envAltitudeSlider.max, Math.max(0, res.elevation));
            envHumiditySlider.value = res.humidity;

            updateSliderReadouts();
            weatherSyncIndicator.textContent = 'Synced: ' + res.location_name + ' (Elev: ' + res.elevation.toFixed(0) + 'm)';
            calculateEstimation();
        }).catch(function(error) {
            weatherSyncIndicator.textContent = 'Sync Failed: ' + error.message;
        }).then(function() {
            btnSyncWeather.disabled = false;
        });
    });

    function setupInputEventHandlers() {
        var sliders = [
            { s: envTempSlider, r: valTemp, unit: " °C", f: 1 },
            { s: envWindSlider, r: valWind, unit: " km/h", f: 1 },
            { s: envAltitudeSlider, r: valAltitude, unit: " m", f: 0 },
            { s: envHumiditySlider, r: valHumidity, unit: " %", f: 0 }
        ];

        for (var si = 0; si < sliders.length; si++) {
            (function(item) {
                item.s.addEventListener('input', function() {
                    item.r.textContent = parseFloat(item.s.value).toFixed(item.f) + item.unit;
                    calculateEstimation();
                });
            })(sliders[si]);
        }

        // Tuning sliders
        var coeffSliders = [
            { s: coeffTempSlider, r: valCoeffTemp, field: 'temp_coeff' },
            { s: coeffWindSlider, r: valCoeffWind, field: 'wind_coeff' },
            { s: coeffAltSlider, r: valCoeffAlt, field: 'alt_coeff' }
        ];

        for (var ci = 0; ci < coeffSliders.length; ci++) {
            (function(item) {
                item.s.addEventListener('input', function() {
                    var val = parseFloat(item.s.value);
                    item.r.textContent = val.toFixed(2) + 'x';
                    currentTuningCoeffs[item.field] = val;
                    calculateEstimation();
                });
            })(coeffSliders[ci]);
        }

        var formInputs = aircraftForm.querySelectorAll('input');
        for (var ii = 0; ii < formInputs.length; ii++) {
            formInputs[ii].addEventListener('input', function() {
                calculateEstimation();
            });
        }
    }

    function updateSliderReadouts() {
        valTemp.textContent = parseFloat(envTempSlider.value).toFixed(1) + ' °C';
        valWind.textContent = parseFloat(envWindSlider.value).toFixed(1) + ' km/h';
        valAltitude.textContent = parseFloat(envAltitudeSlider.value).toFixed(0) + ' m';
        valHumidity.textContent = parseFloat(envHumiditySlider.value).toFixed(0) + ' %';
    }

    function calculateEstimation() {
        var empty_weight = parseFloat(acEmptyWeightInput.value) || 2.0;
        var motors = parseInt(acMotorsInput.value) || 4;
        var hover_current = parseFloat(acHoverCurrentInput.value) || 5.0;
        var battery_capacity = parseInt(acBatteryCapacityInput.value) || 10000;
        var battery_cells = parseInt(acBatteryCellsInput.value) || 6;
        var battery_reserve = parseFloat(acBatteryReserveInput.value) || 20.0;
        var battery_efficiency = parseFloat(acBatteryEfficiencyInput.value) || 85.0;
        var cruise_speed = acCruiseSpeedInput.value ? parseFloat(acCruiseSpeedInput.value) : 45.0;

        var temperature = parseFloat(envTempSlider.value);
        var wind_speed = parseFloat(envWindSlider.value);
        var altitude = parseFloat(envAltitudeSlider.value);
        var humidity = parseFloat(envHumiditySlider.value);
        var payload = parseFloat(acPayloadInput.value) || 0.0;

        var requestBody = {
            empty_weight: empty_weight,
            payload: payload,
            battery_capacity: battery_capacity,
            battery_cells: battery_cells,
            battery_reserve: battery_reserve,
            battery_efficiency: battery_efficiency,
            motors: motors,
            hover_current: hover_current,
            cruise_speed: cruise_speed,
            temperature: temperature,
            wind_speed: wind_speed,
            altitude: altitude,
            humidity: humidity,
            temp_coeff: currentTuningCoeffs.temp_coeff,
            wind_coeff: currentTuningCoeffs.wind_coeff,
            alt_coeff: currentTuningCoeffs.alt_coeff
        };

        API.estimateEndurance(requestBody).then(function(result) {
            // HUD
            hudTimer.textContent = formatMinutesSeconds(result.final_estimated_time_min);
            if (profileEstTime) profileEstTime.textContent = formatMinutesSeconds(result.final_estimated_time_min) + ' min';
            
            hudWeight.innerHTML = (empty_weight + payload).toFixed(2) + ' <span class="unit">kg</span>';
            hudRange.innerHTML = result.estimated_range_km !== null ? result.estimated_range_km.toFixed(1) + ' <span class="unit">km</span>' : 'N/A';
            hudCurrent.innerHTML = result.hover_current_loaded.toFixed(2) + ' <span class="unit">A/mtr</span>';
            hudAlt.innerHTML = altitude.toFixed(0) + ' <span class="unit">m</span>';
            hudWind.innerHTML = wind_speed.toFixed(1) + ' <span class="unit">km/h</span>';
            hudTempDerate.innerHTML = Math.round(result.temp_derate_factor * 100) + ' <span class="unit">%</span>';

            // Battery Card
            if (batCapVal) batCapVal.textContent = battery_capacity + ' mAh';
            if (batHoverTime) batHoverTime.textContent = result.nameplate_time_min.toFixed(1) + ' min';

            // Waterfall & Advisories
            Charts.renderWaterfall(waterfallDisplay, result);
            renderAdvisories(result.advisories);
            renderScatterPlot();
        }).catch(function(err) {
            console.error("Estimation error:", err);
        });
    }

    function formatMinutesSeconds(time_min) {
        if (isNaN(time_min) || time_min < 0) return '00:00';
        var total_seconds = Math.round(time_min * 60);
        var m = Math.floor(total_seconds / 60);
        var s = total_seconds % 60;
        return (m < 10 ? '0' : '') + m + ':' + (s < 10 ? '0' : '') + s;
    }

    function renderAdvisories(advisories) {
        advisoryFeed.innerHTML = '';
        if (!advisories || advisories.length === 0) return;

        for (var ai = 0; ai < advisories.length; ai++) {
            var adv = advisories[ai];
            var div = document.createElement('div');
            div.className = 'advisory-banner level-' + adv.level;
            div.innerHTML = '<strong>' + (adv.level === 'critical' ? 'CRITICAL' : 'WARNING') + ':</strong> ' + adv.message;
            advisoryFeed.appendChild(div);
        }
    }

    function renderScatterPlot() {
        if (!selectedAircraft) {
            Charts.renderScatterPlot(scatterSvg, [], null, null);
            return;
        }
        Charts.renderScatterPlot(scatterSvg, flightLogs, predictFlightTimeLocal, currentTuningCoeffs);
    }

    function predictFlightTimeLocal(log, coeffs) {
        if (!selectedAircraft) return 0;

        var empty_weight = selectedAircraft.empty_weight;
        var payload = log.payload;
        var motors = selectedAircraft.motors;
        var hover_current_empty = selectedAircraft.hover_current;
        var capacity = selectedAircraft.battery_capacity;
        var reserve = selectedAircraft.battery_reserve;
        var efficiency = selectedAircraft.battery_efficiency;

        var total_weight = empty_weight + payload;
        var hover_current_loaded = hover_current_empty;
        if (empty_weight > 0) {
            hover_current_loaded = hover_current_empty * Math.pow(total_weight / empty_weight, 1.3);
        }

        var nameplate_time_min = (capacity / 1000.0) / (motors * hover_current_loaded) * 60.0;
        var usable_fraction = (1.0 - (reserve / 100.0)) * (efficiency / 100.0);
        var usable_time_min = nameplate_time_min * usable_fraction;

        var t = log.temperature;
        var temp_derate_raw = 1.0;
        if (t <= -20) temp_derate_raw = 0.50;
        else if (t < 20) temp_derate_raw = 0.50 + 0.50 * (t + 20) / 40.0;
        else if (t <= 35) temp_derate_raw = 1.00;
        else if (t <= 55) temp_derate_raw = 1.00 - 0.15 * (t - 35) / 20.0;
        else temp_derate_raw = 0.85;

        var temp_derate_factor = Math.max(0.1, Math.min(1.0, 1.0 - (1.0 - temp_derate_raw) * coeffs.temp_coeff));
        var temp_derated_time_min = usable_time_min * temp_derate_factor;

        var w = log.wind_speed;
        var wind_penalty_raw = 1.0;
        if (w <= 10) wind_penalty_raw = 1.0;
        else if (w <= 50) wind_penalty_raw = 1.0 + 0.65 * (w - 10) / 40.0;
        else wind_penalty_raw = 1.65;

        var wind_penalty_factor = Math.max(1.0, 1.0 + (wind_penalty_raw - 1.0) * coeffs.wind_coeff);
        var wind_adjusted_time_min = temp_derated_time_min / wind_penalty_factor;

        var alt = log.altitude;
        var density_ratio = Math.exp(-alt / 8500.0);
        var alt_penalty_raw = 1.0 / Math.sqrt(density_ratio || 0.0001);

        var altitude_penalty_factor = Math.max(0.5, 1.0 + (alt_penalty_raw - 1.0) * coeffs.alt_coeff);
        return wind_adjusted_time_min / altitude_penalty_factor;
    }

    btnExportSessionCsv.addEventListener('click', function() {
        var params = {
            empty_weight: parseFloat(acEmptyWeightInput.value) || 2.0,
            payload: parseFloat(acPayloadInput.value) || 0.0,
            battery_capacity: parseInt(acBatteryCapacityInput.value) || 10000,
            battery_cells: parseInt(acBatteryCellsInput.value) || 6,
            battery_reserve: parseFloat(acBatteryReserveInput.value) || 20.0,
            battery_efficiency: parseFloat(acBatteryEfficiencyInput.value) || 85.0,
            motors: parseInt(acMotorsInput.value) || 4,
            hover_current: parseFloat(acHoverCurrentInput.value) || 5.0,
            cruise_speed: acCruiseSpeedInput.value ? parseFloat(acCruiseSpeedInput.value) : null,
            temperature: parseFloat(envTempSlider.value),
            wind_speed: parseFloat(envWindSlider.value),
            altitude: parseFloat(envAltitudeSlider.value),
            humidity: parseFloat(envHumiditySlider.value),
            temp_coeff: currentTuningCoeffs.temp_coeff,
            wind_coeff: currentTuningCoeffs.wind_coeff,
            alt_coeff: currentTuningCoeffs.alt_coeff
        };

        window.location.href = API.getSessionExportUrl(params);
    });

    btnExportPdf.addEventListener('click', function() {
        window.print();
    });

    init();
});
