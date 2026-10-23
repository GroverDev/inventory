import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
    // El template original importa componentes sin extensión (p. ej. "@/components/nav-bar").
    extensions: ['.mjs', '.js', '.ts', '.json', '.vue'],
  },
  server: { port: 5173, strictPort: true },
  css: {
    preprocessorOptions: {
      scss: { quietDeps: true, loadPaths: [fileURLToPath(new URL('.', import.meta.url))] },
    },
  },
})
