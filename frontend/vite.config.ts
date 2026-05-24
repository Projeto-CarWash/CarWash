import path from 'path';

import babel from '@rolldown/plugin-babel';
import react, { reactCompilerPreset } from '@vitejs/plugin-react';
import { defineConfig } from 'vite';

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), babel({ presets: [reactCompilerPreset()] })],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    host: '0.0.0.0',
    proxy: {
      // Em dev o frontend (5173) usa caminhos absolutos `/api/...` para evitar
      // CORS e variação de baseURL por ambiente. O proxy encaminha ao backend.
      // Em docker-compose.dev.yml define `VITE_PROXY_TARGET=http://backend:8080`;
      // fora do compose o fallback aponta para a porta exposta no host.
      '/api': {
        target: process.env.VITE_PROXY_TARGET ?? 'http://localhost:8080',
        changeOrigin: true,
      },
    },
  },
});
