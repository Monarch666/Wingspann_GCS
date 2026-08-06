import React from 'react';
import { Play, SkipBack, SkipForward } from 'lucide-react';
import './PlaybackPanel.css';

export const PlaybackPanel: React.FC = () => {
  return (
    <div className="playback-panel">
      <div className="playback-controls">
        <button className="play-btn">
          <Play size={16} fill="currentColor" />
        </button>
        <button className="icon-btn hover-brighten"><SkipBack size={14} /></button>
        <button className="icon-btn hover-brighten"><SkipForward size={14} /></button>
      </div>
      <div className="live-pill">
        <div className="status-dot live-pulse"></div>
        LIVE NOW • 10:15 AM
      </div>
      <div className="scrubber-timeline disabled">
        <div className="tick">-12h</div>
        <div className="tick">-6h</div>
        <div className="tick current">
          <div className="tick-marker"></div>
          NOW
        </div>
        <div className="tick">+6h</div>
        <div className="tick">+12h</div>
        <div className="timeline-line"></div>
      </div>
    </div>
  );
};
