import React from 'react';
import { Map, Route, Shield, MapPin, ChevronRight } from 'lucide-react';
import './RightPanel.css';

interface Props {
  isSatellite: boolean;
  toggleSatellite: () => void;
  onSelectBookmark: (lat: number, lon: number) => void;
}

const BOOKMARKS = [
  { name: 'New Delhi', lat: 28.5562, lon: 77.1000 },
  { name: 'Mumbai', lat: 19.0896, lon: 72.8656 },
  { name: 'Bangalore', lat: 13.1986, lon: 77.7066 },
  { name: 'Chennai', lat: 12.9941, lon: 80.1709 },
  { name: 'Kolkata', lat: 22.6547, lon: 88.4467 }
];

export const RightPanel: React.FC<Props> = ({ isSatellite, toggleSatellite, onSelectBookmark }) => {
  return (
    <div className="right-panel">
      {/* LAYERS */}
      <div className="panel-section">
        <div className="panel-header">
          <span className="text-small-caps">LAYERS</span>
        </div>
        
        <div className="layer-list">
          <div className={`layer-item ${isSatellite ? 'active' : ''}`} onClick={toggleSatellite}>
            <Map size={16} className="layer-icon" />
            <span className="layer-name">Satellite</span>
          </div>
          <div className="layer-item active">
            <Route size={16} className="layer-icon" />
            <span className="layer-name">Flight Paths</span>
          </div>
          <div className="layer-item">
            <Shield size={16} className="layer-icon" />
            <span className="layer-name">Airspace Boundaries</span>
          </div>
          <div className="layer-item">
            <MapPin size={16} className="layer-icon" />
            <span className="layer-name">Airports & Runways</span>
          </div>
        </div>
      </div>

      {/* BOOKMARKS */}
      <div className="panel-section">
        <div className="panel-header flex-between">
          <span className="text-small-caps">BOOKMARKS</span>
          <span className="add-location text-accent">+ Add Location</span>
        </div>
        
        <div className="bookmark-list">
          {BOOKMARKS.map(b => (
            <div key={b.name} className="bookmark-item" onClick={() => onSelectBookmark(b.lat, b.lon)}>
              <span className="bookmark-name">{b.name}</span>
              <ChevronRight size={14} className="text-secondary" />
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};
