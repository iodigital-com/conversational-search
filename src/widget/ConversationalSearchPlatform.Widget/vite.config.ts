import {defineConfig} from 'vite'
import preact from '@preact/preset-vite'
import * as path from "path";

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
        rollupOptions: {
            input: {
                app: 'webcomponent.html',
                'dev': path.resolve(__dirname, 'src/main.tsx'),
                'webComponent': path.resolve(__dirname, 'src/main.ts'),
            }
        }
    }
})
