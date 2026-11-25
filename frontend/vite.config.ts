import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// Dev: the browser talks to Vite, Vite forwards /api to the YARP gateway on :5100.
export default defineConfig({
  plugins: [react()],
  server: { port: 5173, proxy: { '/api': 'http://localhost:5100' } },
});
