export interface Aircraft {
  icao24: string;
  callsign: string;
  originCountry: string;
  timePosition: number | null;
  lastContact: number;
  longitude: number | null;
  latitude: number | null;
  baroAltitude: number | null; // meters
  onGround: boolean;
  velocity: number | null; // m/s
  trueTrack: number | null; // decimal degrees clockwise from N
  verticalRate: number | null; // m/s
  sensors: number[] | null;
  geoAltitude: number | null; // meters
  squawk: string | null;
  spi: boolean;
  positionSource: number;
}
