import {defineConfig} from 'vite'
import preact from '@preact/preset-vite'
import * as path from "path";
import {fileURLToPath} from "url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// https://vitejs.dev/config/
export default defineConfig({
    plugins: [preact()],
    resolve: {
        alias: [{find: '@', replacement: '/src'}],
    },
    server: {
        open: "webcomponent.html"
    },
    build: {
        outDir: "../dist",
        manifest: true,
        rollupOptions: {
            input: {
                app: 'webcomponent.html',
                'dev': path.resolve(__dirname, 'src/main.tsx'),
                'webComponent': path.resolve(__dirname, 'src/main.ts'),
            }
        }
    }
})
