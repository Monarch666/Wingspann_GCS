import React, { useEffect } from 'react';
import { MapContainer, TileLayer, Marker, Polyline, useMap } from 'react-leaflet';
import L from 'leaflet';
import type { Aircraft } from '../types';
import './RadarMap.css';

interface Props {
  flights: Aircraft[];
  selectedFlight: Aircraft | null;
  onSelectFlight: (flight: Aircraft) => void;
  isSatellite: boolean;
  mapCenter: [number, number] | null;
}

const TILE_DARK = 'https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png';
const TILE_SAT = 'https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}';

// Custom Map Controller to handle flyTo
const MapController: React.FC<{ center: [number, number] | null }> = ({ center }) => {
  const map = useMap();
  useEffect(() => {
    if (center) {
      map.flyTo(center, 9, { duration: 1.5 });
    }
  }, [center, map]);
  return null;
};

export const RadarMap: React.FC<Props> = ({ flights, selectedFlight, onSelectFlight, isSatellite, mapCenter }) => {
  
  // Create a custom plane icon
  const getPlaneIcon = (flight: Aircraft, isSelected: boolean) => {
    const rotation = flight.trueTrack || 0;
    
    // Determine color based on squawk/vertical rate
    let colorClass = 'text-accent'; // normal
    if (flight.squawk && flight.squawk.startsWith('7')) colorClass = 'text-danger'; // Emergency
    else if (Math.abs(flight.verticalRate || 0) > 15) colorClass = 'text-warning'; // rapid climb/descend

    const iconHtml = `
      <div class="plane-marker ${isSelected ? 'selected' : ''}" style="transform: rotate(${rotation}deg);">
        <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" class="${colorClass}">
          <path d="M17.8 19.2 16 11l3.5-3.5C21 6 21.5 4 21 3c-1-.5-3 0-4.5 1.5L13 8 4.8 6.2c-.5-.1-.9.2-1.1.5l-1.3 2.6c-.2.4-.1 1 .3 1.3L9 14l-4 4-3-1-1 1 4 4 1-1-1-3 4-4 3.4 6.3c.3.4.9.5 1.3.3l2.6-1.3c.3-.2.6-.6.5-1.1z"/>
        </svg>
      </div>
      ${isSelected ? `<div class="marker-label">${flight.callsign || flight.icao24}</div>` : ''}
    `;

    return L.divIcon({
      html: iconHtml,
      className: 'custom-leaflet-icon',
      iconSize: [24, 24],
      iconAnchor: [12, 12],
    });
  };

  return (
    <div className="map-wrapper">
      <MapContainer 
        center={[20.5937, 78.9629]} // Center of India
        zoom={5} 
        zoomControl={false}
        className="radar-map"
      >
        <MapController center={mapCenter} />
        
        <TileLayer
          attribution='&copy; <a href="https://carto.com/">CARTO</a>'
          url={isSatellite ? TILE_SAT : TILE_DARK}
          maxZoom={19}
        />

        {flights.map(flight => {
          if (!flight.latitude || !flight.longitude) return null;
          const isSelected = selectedFlight?.icao24 === flight.icao24;
          
          return (
            <Marker 
              key={flight.icao24}
              position={[flight.latitude, flight.longitude]}
              icon={getPlaneIcon(flight, isSelected)}
              eventHandlers={{
                click: () => onSelectFlight(flight),
              }}
              zIndexOffset={isSelected ? 1000 : 0}
            />
          );
        })}

        {/* Dummy route polyline if selected */}
        {selectedFlight && selectedFlight.latitude && selectedFlight.longitude && (
          <Polyline 
            positions={[
              [selectedFlight.latitude - 2, selectedFlight.longitude - 2],
              [selectedFlight.latitude, selectedFlight.longitude],
              [selectedFlight.latitude + 2, selectedFlight.longitude + 2]
            ]} 
            color="var(--accent-primary)"
            weight={2}
            dashArray="5, 10"
            opacity={0.6}
          />
        )}
      </MapContainer>
    </div>
  );
};
