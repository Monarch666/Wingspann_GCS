import React from 'react';
import { ChevronDown, Menu, Crosshair } from 'lucide-react';
import './TopNav.css';

export const TopNav: React.FC = () => {
  return (
    <nav className="top-nav glass-panel">
      <div className="nav-left">
        <button className="hamburger-btn hover-brighten">
          <Menu size={20} className="text-accent" />
        </button>
        <div className="logo-container">
          <Crosshair className="logo-icon text-accent" size={24} />
          <div className="logo-text">
            <span className="logo-title">ODIN</span>
            <span className="logo-subtitle">GCS</span>
          </div>
        </div>
        <div className="nav-tabs">
          <button className="nav-tab">FLIGHT</button>
          <button className="nav-tab">ANALYTICS</button>
          <button className="nav-tab">WEATHER</button>
          <button className="nav-tab active">RADAR</button>
          <button className="nav-tab">ETA PLANNER</button>
          <button className="nav-tab">SETTINGS</button>
        </div>
      </div>
      <div className="nav-right">
        <div className="status-pill">
          <div className="status-dot live"></div>
          <span className="status-text">STABILIZE</span>
          <ChevronDown size={16} />
        </div>
      </div>
    </nav>
  );
};
