/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{html,ts}'],
  theme: {
    extend: {
      colors: {
        ink: '#1F2937',
        muted: '#6B7280',
        canvas: '#FAF9F6',
        line: '#E5E7EB',
        sage: {
          surface: '#E6F0EC',
          ink: '#2D5A4C',
        },
        sand: {
          surface: '#FDF3E7',
          ink: '#8A5A2B',
        },
        slate: {
          surface: '#EBF1F5',
          ink: '#334E68',
        },
      },
      fontFamily: {
        sans: [
          'ui-sans-serif',
          'system-ui',
          '-apple-system',
          'BlinkMacSystemFont',
          '"Segoe UI"',
          'Roboto',
          'sans-serif',
        ],
      },
      boxShadow: {
        card: '0 1px 2px rgb(31 41 55 / 0.035)',
      },
    },
  },
  plugins: [],
};
