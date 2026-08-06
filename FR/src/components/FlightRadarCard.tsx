import React from 'react';
import { Radar, Navigation, Star } from 'lucide-react';
import type { Aircraft } from '../types';
import './FlightRadarCard.css';

interface Props {
  selectedFlight: Aircraft | null;
  nearbyFlights: Aircraft[];
  onSelectFlight: (flight: Aircraft) => void;
}

export const FlightRadarCard: React.FC<Props> = ({ selectedFlight, nearbyFlights, onSelectFlight }) => {
  return (
    <div className="flight-radar-card">
      {/* Header */}
      <div className="card-header">
        <div className="header-icon-box">
          <Radar size={16} className="text-primary" />
        </div>
        <div className="header-text">
          <h2 className="card-title">ODIN RADAR</h2>
          <span className="card-subtitle">LIVE TRACKING & TELEMETRY</span>
        </div>
      </div>

      {/* Hero Section */}
      <div className="hero-section">
        <div className="hero-header flex-between">
          <div className="hero-title-group">
            <h1 className="hero-title">{selectedFlight?.callsign || 'SELECT FLIGHT'}</h1>
            <span className="hero-sub">{selectedFlight?.originCountry || 'N/A'}</span>
          </div>
          <button className="icon-btn hover-brighten">
            <Star size={18} className="text-secondary" />
          </button>
        </div>

        <div className="hero-metrics-container">
          <div className="main-metric">
            <Radar size={32} className="text-warning hero-main-icon" />
            <div className="metric-group">
              <div className="hero-value">{Math.round(selectedFlight?.baroAltitude || 0)}<span className="unit">m</span></div>
              <div className="hero-desc">Altitude</div>
              <div className="hero-sub-stats">
                <span className="text-danger">↓{selectedFlight?.verticalRate || 0}</span> • <span className="text-accent">↑{Math.round((selectedFlight?.velocity || 0) * 3.6)} km/h</span>
              </div>
            </div>
          </div>
        </div>

        <div className="forecast-text">
          Live telemetry stream is active. Track data updating every 5 seconds.
        </div>
      </div>

      {/* 24-HOUR FORECAST Equivalent -> NEARBY FLIGHTS */}
      <div className="strip-section">
        <div className="section-title flex-between">
          <span>NEARBY FLIGHTS</span>
          <span className="chevron-right">»</span>
        </div>
        <div className="horizontal-strip">
          {nearbyFlights.map(flight => (
            <div 
              key={flight.icao24} 
              className={`strip-item ${selectedFlight?.icao24 === flight.icao24 ? 'active' : ''}`}
              onClick={() => onSelectFlight(flight)}
            >
              <span className="strip-time">{flight.callsign}</span>
              <Navigation size={18} className={selectedFlight?.icao24 === flight.icao24 ? 'text-accent' : 'text-warning'} style={{ transform: `rotate(${flight.trueTrack || 0}deg)` }} />
              <span className="strip-val">{Math.round(flight.baroAltitude || 0)}m</span>
            </div>
          ))}
        </div>
      </div>

      {/* 7-DAY OUTLOOK Equivalent -> WATCHLIST */}
      <div className="list-section">
        <div className="section-title">WATCHLIST</div>
        <div className="vertical-list">
          {nearbyFlights.slice(0, 3).map((flight, idx) => (
            <div 
              key={`wl-${flight.icao24}`} 
              className="list-item"
              onClick={() => onSelectFlight(flight)}
            >
              <span className="list-day">FLT 0{idx+1}</span>
              <Navigation size={14} className="text-warning" />
              <div className="list-bar">
                <div className="bar-fill" style={{width: `${Math.random() * 50 + 20}%`}}></div>
              </div>
              <div className="list-vals">
                <span>{Math.round(flight.baroAltitude || 0)}</span>
                <span className="text-secondary"> / {Math.round((flight.velocity || 0) * 3.6)}</span>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};
