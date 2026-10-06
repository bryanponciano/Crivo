import type { Config } from 'tailwindcss';

const config: Config = {
  content: [
    './index.html',
    './src/**/*.{js,ts,jsx,tsx}',
  ],
  theme: {
    extend: {
      colors: {
        brand: {
          dark: '#1e3a5f',
          accent: '#3b82f6',
          sidebar: '#1e293b',
        }
      }
    },
  },
  plugins: [],
};
export default config;
