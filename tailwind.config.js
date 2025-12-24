/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./public/**/*.{html,js}",
    "./portals/**/*.{html,js}",
    "./templates/**/*.{html,js}",
    "./shared/**/*.{html,js}",
    "./public/account/**/*.{html,js}"
  ],
  theme: {
    extend: {
      colors: {
        'primary-blue': '#2563EB',
        'accent-teal': '#14B8A6',
        'text-white': '#FFFFFF',
        'subtle-blue': '#EFF6FF',
        'error-red': '#EF4444',
        'success-green': '#10B981',
        'warning-yellow': '#F59E0B',
        'info-blue': '#3B82F6'
      },
      fontFamily: {
        'primary': ['Inter', 'sans-serif'],
        'display': ['Nunito', 'Varela Round', 'sans-serif']
      },
      animation: {
        'fade-in': 'fadeIn 0.3s ease-in-out',
        'slide-up': 'slideUp 0.3s ease-out',
        'pulse-soft': 'pulseSoft 2s infinite'
      },
      keyframes: {
        fadeIn: {
          '0%': { opacity: '0' },
          '100%': { opacity: '1' }
        },
        slideUp: {
          '0%': { transform: 'translateY(20px)', opacity: '0' },
          '100%': { transform: 'translateY(0)', opacity: '1' }
        },
        pulseSoft: {
          '0%, 100%': { opacity: '1' },
          '50%': { opacity: '0.8' }
        }
      }
    }
  },
  plugins: [
    require('@tailwindcss/forms'),
  ],
}
