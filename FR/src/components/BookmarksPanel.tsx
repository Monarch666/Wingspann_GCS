import React from 'react';
import { Bookmark, ChevronRight } from 'lucide-react';
import './Panels.css';

interface Props {
  onSelectBookmark: (lat: number, lon: number) => void;
}

const BOOKMARKS = [
  { name: 'Delhi IGI', code: 'DEL', lat: 28.5562, lon: 77.1000 },
  { name: 'Mumbai BOM', code: 'BOM', lat: 19.0896, lon: 72.8656 },
  { name: 'Bangalore BLR', code: 'BLR', lat: 13.1986, lon: 77.7066 },
  { name: 'Chennai MAA', code: 'MAA', lat: 12.9941, lon: 80.1709 },
  { name: 'Kolkata CCU', code: 'CCU', lat: 22.6547, lon: 88.4467 }
];

export const BookmarksPanel: React.FC<Props> = ({ onSelectBookmark }) => {
  return (
    <div className="bookmarks-panel glass-panel">
      <div className="panel-header">
        <Bookmark size={14} className="text-secondary" />
        <span className="text-small-caps">BOOKMARKS</span>
      </div>
      
      <div className="bookmark-list">
        {BOOKMARKS.map(b => (
          <div key={b.code} className="bookmark-item" onClick={() => onSelectBookmark(b.lat, b.lon)}>
            <div className="bookmark-info">
              <span className="bookmark-code">{b.code}</span>
              <span className="bookmark-name">{b.name}</span>
            </div>
            <ChevronRight size={16} className="text-secondary" />
          </div>
        ))}
      </div>
    </div>
  );
};
