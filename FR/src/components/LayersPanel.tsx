import React from 'react';
import { Layers, Map, Route, CloudLightning, Shield, MapPin } from 'lucide-react';
import './Panels.css';

interface Props {
  isSatellite: boolean;
  toggleSatellite: () => void;
}

export const LayersPanel: React.FC<Props> = ({ isSatellite, toggleSatellite }) => {
  return (
    <div className="layers-panel glass-panel">
      <div className="panel-header">
        <Layers size={14} className="text-secondary" />
        <span className="text-small-caps">LAYERS</span>
      </div>
      
      <div className="layer-list">
        <div className={`layer-item ${isSatellite ? 'active' : ''}`} onClick={toggleSatellite}>
          <Map size={16} className="layer-icon" />
          <span className="layer-name">Satellite Basemap</span>
        </div>
        <div className="layer-item active">
          <Route size={16} className="layer-icon" />
          <span className="layer-name">Flight Paths</span>
        </div>
        <div className="layer-item">
          <CloudLightning size={16} className="layer-icon" />
          <span className="layer-name">Weather Overlay</span>
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
  );
};
