import { useState } from 'react';
import { BookShipment } from './pages/BookShipment';
import { Shipments } from './pages/Shipments';
import { Track } from './pages/Track';

type Tab = 'book' | 'shipments' | 'track';

export default function App() {
  const [tab, setTab] = useState<Tab>('book');
  const [refreshKey, setRefreshKey] = useState(0);

  return (
    <div className="app">
      <header>
        <h1>Courier Platform</h1>
        <nav>
          <button className={tab === 'book' ? 'active' : ''} onClick={() => setTab('book')}>Book a shipment</button>
          <button className={tab === 'shipments' ? 'active' : ''} onClick={() => setTab('shipments')}>Shipments (ops)</button>
          <button className={tab === 'track' ? 'active' : ''} onClick={() => setTab('track')}>Track a parcel</button>
        </nav>
      </header>
      {tab === 'book' && <BookShipment onBooked={() => setRefreshKey((k) => k + 1)} />}
      {tab === 'shipments' && <Shipments refreshKey={refreshKey} />}
      {tab === 'track' && <Track />}
    </div>
  );
}
