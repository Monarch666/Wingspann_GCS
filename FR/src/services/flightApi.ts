import type { Aircraft } from '../types';

/**
 * Flight API Service — Fetches REAL live ADS-B data via the C# WebView2 proxy.
 * 
 * Architecture:
 *   JS (fetch) → https://flightradar.local/api/flights?lat=X&lon=Y
 *                ↓ (intercepted by WebView2 WebResourceRequested)
 *   C# proxy  → ADSB.lol (primary) or OpenSky (secondary)
 *                ↓ (with rate limiting + 15s cache)
 *   JS        ← JSON response (no CORS issues, real data)
 * 
 * Guardrails (enforced in C#):
 *   - 15-second response cache between live fetches
 *   - 100 requests/hour hard cap
 *   - Multi-source failover (ADSB.lol → OpenSky → error)
 */

export async function fetchLiveFlights(
  gcsLocation: [number, number]
): Promise<{ data: Aircraft[]; error?: string; isMock?: boolean; source?: string; usage?: string }> {
  const [lat, lon] = gcsLocation;

  try {
    // Fetch from C# proxy (same-origin — no CORS)
    const proxyUrl = `https://flightradar.local/api/flights?lat=${lat}&lon=${lon}`;
    const res = await fetch(proxyUrl);

    if (!res.ok) {
      throw new Error(`Proxy returned HTTP ${res.status}`);
    }

    const json = await res.json();

    // Check for rate limiting
    if (json.rateLimited) {
      return {
        data: generateMockFlights(),
        error: `Rate Limited (${json.error || 'Hourly cap reached'})`,
        isMock: true,
        usage: `${json.requestsThisHour || '?'}/${json.maxRequestsPerHour || '?'} req/hr`
      };
    }

    // Check for upstream errors
    if (json.error && (!json.data || Object.keys(json.data).length === 0)) {
      return {
        data: generateMockFlights(),
        error: json.error,
        isMock: true
      };
    }

    const source = json.source || 'unknown';
    const usage = `${json.requestsThisHour || 0}/${json.maxRequestsPerHour || 100} req/hr`;
    const innerData = json.data || json;

    let aircraft: Aircraft[] = [];

    // Parse ADSB.lol format: { ac: [...] }
    if (innerData.ac && Array.isArray(innerData.ac)) {
      aircraft = innerData.ac
        .filter((item: any) => typeof item.lat === 'number' && typeof item.lon === 'number')
        .map((item: any) => ({
          icao24: item.hex || item.icao || Math.random().toString(16).substring(2, 8),
          callsign: (item.flight || item.r || item.hex || 'UNKNOWN').trim(),
          originCountry: item.t || 'Unknown',
          timePosition: Date.now(),
          lastContact: Date.now(),
          longitude: item.lon,
          latitude: item.lat,
          baroAltitude: typeof item.alt_baro === 'number' ? Math.round(item.alt_baro * 0.3048) : 0,
          onGround: item.alt_baro === 'ground' || Boolean(item.ground),
          velocity: typeof item.gs === 'number' ? Math.round(item.gs * 0.514444) : 0,
          trueTrack: item.track ?? item.true_heading ?? item.mag_heading ?? 0,
          verticalRate: typeof item.baro_rate === 'number' ? Math.round(item.baro_rate * 0.00508) : 0,
          sensors: null,
          geoAltitude: typeof item.alt_geom === 'number' ? Math.round(item.alt_geom * 0.3048) : 0,
          squawk: item.squawk || '1200',
          spi: Boolean(item.spi),
          positionSource: 0
        }));
    }

    // Parse OpenSky format: { states: [[...], ...] }
    if (aircraft.length === 0 && innerData.states && Array.isArray(innerData.states)) {
      aircraft = innerData.states.map((state: any[]) => ({
        icao24: state[0],
        callsign: (state[1] || '').trim(),
        originCountry: state[2],
        timePosition: state[3],
        lastContact: state[4],
        longitude: state[5],
        latitude: state[6],
        baroAltitude: state[7],
        onGround: state[8],
        velocity: state[9],
        trueTrack: state[10],
        verticalRate: state[11],
        sensors: state[12],
        geoAltitude: state[13],
        squawk: state[14],
        spi: state[15],
        positionSource: state[16]
      }));
    }

    if (aircraft.length > 0) {
      return { data: aircraft, source, usage };
    }

    // Live sources returned empty — no aircraft in range
    return { data: [], source, usage };

  } catch (err: any) {
    console.warn('C# proxy fetch failed, using simulation fallback:', err);
    return {
      data: generateMockFlights(),
      error: `Radar Offline — ${err.message || 'Connection Error'}`,
      isMock: true
    };
  }
}

// ── Simulation fallback (only used when C# proxy is unreachable) ──
let mockState: Aircraft[] = [
  {
    icao24: '800bc1', callsign: 'SIM-001', originCountry: 'Simulation', timePosition: Date.now(), lastContact: Date.now(),
    longitude: 77.1025, latitude: 28.5562, baroAltitude: 8500, onGround: false,
    velocity: 240, trueTrack: 135, verticalRate: 0, sensors: null, geoAltitude: 8600, squawk: '0000', spi: false, positionSource: 0
  },
  {
    icao24: '800bc2', callsign: 'SIM-002', originCountry: 'Simulation', timePosition: Date.now(), lastContact: Date.now(),
    longitude: 77.35, latitude: 28.72, baroAltitude: 10500, onGround: false,
    velocity: 255, trueTrack: 45, verticalRate: 5.5, sensors: null, geoAltitude: 10600, squawk: '0000', spi: false, positionSource: 0
  }
];

function generateMockFlights(): Aircraft[] {
  const timeDiff = 5;
  mockState = mockState.map(f => {
    if (!f.longitude || !f.latitude || !f.velocity || !f.trueTrack) return f;
    const distMeters = f.velocity * timeDiff;
    const distDegrees = distMeters / 111000;
    const rad = (f.trueTrack * Math.PI) / 180;
    const dLat = Math.cos(rad) * distDegrees;
    const dLon = (Math.sin(rad) * distDegrees) / Math.cos((f.latitude * Math.PI) / 180);
    return {
      ...f,
      latitude: f.latitude + dLat,
      longitude: f.longitude + dLon,
      timePosition: Date.now(),
      lastContact: Date.now(),
      baroAltitude: (f.baroAltitude || 0) + ((f.verticalRate || 0) * timeDiff)
    };
  });
  return mockState;
}
