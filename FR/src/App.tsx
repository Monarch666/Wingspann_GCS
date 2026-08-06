import { useState, useEffect } from 'react';
import { FlightRadarCard } from './components/FlightRadarCard';
import { RightPanel } from './components/RightPanel';
import { BottomBar } from './components/BottomBar';
import { PlaybackPanel } from './components/PlaybackPanel';
import { RadarMap } from './components/RadarMap';
import { fetchLiveFlights } from './services/flightApi';
import type { Aircraft } from './types';
import { Plus, Minus, Maximize, Crosshair, Search } from 'lucide-react';
import './App.css';

function App() {
  const [flights, setFlights] = useState<Aircraft[]>([]);
  const [selectedFlight, setSelectedFlight] = useState<Aircraft | null>(null);
  const [isSatellite, setIsSatellite] = useState(false);
  const [mapCenter, setMapCenter] = useState<[number, number] | null>(null);
  const [latency, setLatency] = useState(0);
  const [apiError, setApiError] = useState<string | null>(null);
  const [dataSource, setDataSource] = useState<string>('');
  const [apiUsage, setApiUsage] = useState<string>('');
  const [searchQuery, setSearchQuery] = useState('');
  
  // Mock Pixhawk GCS Location (New Delhi)
  const getGcsLocation = (): [number, number] => {
    return [28.6139, 77.2090];
  };
  const [gcsLocation] = useState<[number, number]>(getGcsLocation());

  useEffect(() => {
    const updateFlights = async () => {
      const start = Date.now();
      const res = await fetchLiveFlights(gcsLocation);
      setLatency(Date.now() - start);
      
      // Always update flights (live data or simulation fallback)
      setFlights(res.data);
      
      if (res.source) setDataSource(res.source);
      if (res.usage) setApiUsage(res.usage);
      
      if (res.error) {
        setApiError(res.error);
      } else {
        setApiError(null);
      }
      
      if (selectedFlight) {
        const updatedSelected = res.data.find(f => f.icao24 === selectedFlight.icao24);
        if (updatedSelected) setSelectedFlight(updatedSelected);
      }
    };

    updateFlights(); 
    const interval = setInterval(updateFlights, 30000); // 30s polling for GCS integration
    return () => clearInterval(interval);
  }, [selectedFlight, gcsLocation]);

  const handleSelectFlight = (flight: Aircraft) => {
    setSelectedFlight(flight);
    if (flight.latitude && flight.longitude) {
      setMapCenter([flight.latitude, flight.longitude]);
    }
  };

  const handleSelectBookmark = (lat: number, lon: number) => {
    setMapCenter([lat, lon]);
    setSelectedFlight(null);
  };

  const filteredFlights = flights.filter(f => f.callsign.toLowerCase().includes(searchQuery.toLowerCase()));

  const totalAircraft = filteredFlights.length;
  const avgAltitude = filteredFlights.reduce((acc, f) => acc + (f.baroAltitude || 0), 0) / (totalAircraft || 1);
  const avgSpeed = filteredFlights.reduce((acc, f) => acc + (f.velocity || 0), 0) / (totalAircraft || 1);
  const squawks = filteredFlights.filter(f => f.squawk && f.squawk.startsWith('7')).length;

  return (
    <div className="app-container">
      
      <RadarMap 
        flights={filteredFlights}
        selectedFlight={selectedFlight}
        onSelectFlight={handleSelectFlight}
        isSatellite={isSatellite}
        mapCenter={mapCenter}
      />

      {/* Floating Search Bar (like the screenshot) */}
      <div className="floating-search">
        <Search size={16} className="text-secondary" />
        <input 
          type="text" 
          placeholder="Search callsign..." 
          className="search-input"
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
        />
        <Crosshair size={16} className="text-secondary cursor-pointer hover-brighten" />
      </div>

      {/* Data source status badge */}
      {dataSource && !apiError && (
        <div style={{
          position: 'absolute', top: 20, left: '50%', transform: 'translateX(-50%)',
          background: 'rgba(40, 167, 69, 0.9)', color: '#fff', padding: '6px 16px',
          borderRadius: 20, zIndex: 9999, fontWeight: 700, fontSize: '0.75rem',
          letterSpacing: '0.5px', textTransform: 'uppercase',
          border: '1px solid #28a745', boxShadow: '0 4px 12px rgba(0,0,0,0.5)',
          backdropFilter: 'blur(4px)', display: 'flex', alignItems: 'center', gap: '8px'
        }}>
          <span style={{ width: 8, height: 8, borderRadius: '50%', background: '#7cff00', display: 'inline-block', boxShadow: '0 0 6px #7cff00' }}></span>
          LIVE — {dataSource.toUpperCase()} {apiUsage && `• ${apiUsage}`}
        </div>
      )}

      {apiError && (
        <div style={{
          position: 'absolute', top: 20, left: '50%', transform: 'translateX(-50%)',
          background: 'rgba(255, 149, 0, 0.85)', color: '#000', padding: '8px 18px',
          borderRadius: 20, zIndex: 9999, fontWeight: 700, fontSize: '0.8rem',
          letterSpacing: '0.5px', textTransform: 'uppercase',
          border: '1px solid #ff9500', boxShadow: '0 4px 12px rgba(0,0,0,0.5)',
          backdropFilter: 'blur(4px)', display: 'flex', alignItems: 'center', gap: '8px'
        }}>
          <span style={{ width: 8, height: 8, borderRadius: '50%', background: '#000', display: 'inline-block' }}></span>
          {apiError}
        </div>
      )}

      <FlightRadarCard 
        selectedFlight={selectedFlight}
        nearbyFlights={filteredFlights.slice(0, 10)} 
        onSelectFlight={handleSelectFlight}
      />

      <RightPanel 
        isSatellite={isSatellite}
        toggleSatellite={() => setIsSatellite(!isSatellite)}
        onSelectBookmark={handleSelectBookmark}
      />

      <PlaybackPanel />

      <div className="map-corner-controls">
        <button className="corner-btn hover-brighten" onClick={() => setMapCenter([20.5937, 78.9629])}><Crosshair size={18} /></button>
        <button className="corner-btn hover-brighten"><Plus size={18} /></button>
        <button className="corner-btn hover-brighten"><Minus size={18} /></button>
        <button className="corner-btn hover-brighten" onClick={() => {
          if (!document.fullscreenElement) {
            document.documentElement.requestFullscreen();
          } else if (document.exitFullscreen) {
            document.exitFullscreen();
          }
        }}><Maximize size={18} /></button>
      </div>

      <BottomBar 
        totalAircraft={totalAircraft}
        avgAltitude={avgAltitude}
        avgSpeed={avgSpeed}
        squawks={squawks}
        latency={latency}
      />
    </div>
  );
}

export default App;
