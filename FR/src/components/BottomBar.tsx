import React from 'react';
import { Plane, ArrowUpRight, FastForward, AlertTriangle, Activity, Server } from 'lucide-react';
import './BottomBar.css';

interface BottomBarProps {
  totalAircraft: number;
  avgAltitude: number;
  avgSpeed: number;
  squawks: number;
  latency: number;
}

export const BottomBar: React.FC<BottomBarProps> = ({
  totalAircraft,
  avgAltitude,
  avgSpeed,
  squawks,
  latency
}) => {
  return (
    <div className="bottom-bar-container">
      <div className="status-strip">
        <div className="stat-group">
          <Plane size={16} className="text-secondary" />
          <div className="stat-text">
            <span className="text-small-caps">TOTAL AIRCRAFT</span>
            <div className="metric-value">{totalAircraft}</div>
          </div>
        </div>

        <div className="stat-group">
          <ArrowUpRight size={16} className="text-secondary" />
          <div className="stat-text">
            <span className="text-small-caps">AVG ALTITUDE</span>
            <div className="metric-value">{Math.round(avgAltitude).toLocaleString()}<span className="unit">m</span></div>
          </div>
        </div>

        <div className="stat-group">
          <FastForward size={16} className="text-secondary" />
          <div className="stat-text">
            <span className="text-small-caps">AVG SPEED</span>
            <div className="metric-value">{Math.round(avgSpeed * 3.6).toLocaleString()}<span className="unit">km/h</span></div>
          </div>
        </div>

        <div className="stat-group">
          <AlertTriangle size={16} className={squawks > 0 ? "text-warning" : "text-secondary"} />
          <div className="stat-text">
            <span className="text-small-caps">EMERGENCY SQUAWKS</span>
            <div className="metric-value">{squawks}</div>
          </div>
        </div>

        <div className="stat-group">
          <Activity size={16} className="text-secondary" />
          <div className="stat-text">
            <span className="text-small-caps">LAST UPDATE</span>
            <div className="metric-value">LIVE</div>
          </div>
        </div>

        <div className="stat-group">
          <Server size={16} className="text-secondary" />
          <div className="stat-text">
            <span className="text-small-caps">API LATENCY</span>
            <div className="metric-value">{latency}<span className="unit">ms</span></div>
          </div>
        </div>
      </div>
    </div>
  );
};
