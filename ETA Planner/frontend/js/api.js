// AERO-GCS API Client Layer (ES5 Compatible)

var API_BASE_URL = window.location.origin;

var API = {
    request: function(url, options) {
        if (!options) options = {};
        var headers = {
            'Content-Type': 'application/json'
        };
        if (options.headers) {
            for (var key in options.headers) {
                if (options.headers.hasOwnProperty(key)) {
                    headers[key] = options.headers[key];
                }
            }
        }

        var config = {};
        for (var k in options) {
            if (options.hasOwnProperty(k)) {
                config[k] = options[k];
            }
        }
        config.headers = headers;

        return fetch(API_BASE_URL + url, config).then(function(response) {
            if (response.status === 204) {
                return null;
            }
            return response.json().then(function(data) {
                if (!response.ok) {
                    throw new Error(data.detail || 'HTTP Error: ' + response.status);
                }
                return data;
            });
        }).catch(function(error) {
            console.error('API Error on ' + url + ':', error);
            throw error;
        });
    },

    // --- Aircraft Profiles ---
    getAircrafts: function() {
        return this.request('/api/aircraft');
    },

    getAircraft: function(id) {
        return this.request('/api/aircraft/' + id);
    },

    createAircraft: function(aircraftData) {
        return this.request('/api/aircraft', {
            method: 'POST',
            body: JSON.stringify(aircraftData)
        });
    },

    updateAircraftCoefficients: function(id, coefficients) {
        return this.request('/api/aircraft/' + id + '/coefficients', {
            method: 'PUT',
            body: JSON.stringify(coefficients)
        });
    },

    deleteAircraft: function(id) {
        return this.request('/api/aircraft/' + id, {
            method: 'DELETE'
        });
    },

    // --- Flight Logs ---
    getFlightLogs: function(aircraftId) {
        return this.request('/api/flight-logs/' + aircraftId);
    },

    createFlightLog: function(logData) {
        return this.request('/api/flight-logs', {
            method: 'POST',
            body: JSON.stringify(logData)
        });
    },

    deleteFlightLog: function(logId) {
        return this.request('/api/flight-logs/' + logId, {
            method: 'DELETE'
        });
    },

    // --- Weather Sync ---
    getWeather: function(query) {
        var url = '/api/weather';
        if (typeof query === 'string') {
            url += '?q=' + encodeURIComponent(query);
        } else if (query && query.lat !== undefined && query.lon !== undefined) {
            url += '?lat=' + query.lat + '&lon=' + query.lon;
        } else {
            throw new Error('Invalid weather query parameters');
        }
        return this.request(url);
    },

    // --- Flight Time Estimation ---
    estimateEndurance: function(estimateRequest) {
        return this.request('/api/estimate', {
            method: 'POST',
            body: JSON.stringify(estimateRequest)
        });
    },

    // --- Export Links ---
    getLogsExportUrl: function(aircraftId) {
        return API_BASE_URL + '/api/export/csv?aircraft_id=' + aircraftId;
    },

    getSessionExportUrl: function(params) {
        var query = new URLSearchParams();
        var keys = Object.keys(params);
        for (var i = 0; i < keys.length; i++) {
            var key = keys[i];
            var value = params[key];
            if (value !== null && value !== undefined && value !== '') {
                query.append(key, value);
            }
        }
        return API_BASE_URL + '/api/export/session-csv?' + query.toString();
    }
};
