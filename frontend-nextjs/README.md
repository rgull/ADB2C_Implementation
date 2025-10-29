# ADB2C Implementation - Next.js Frontend

This is a Next.js TypeScript application with Tailwind CSS for Azure AD B2C authentication.

## Features

- ✅ Next.js 16 with App Router
- ✅ TypeScript support
- ✅ Tailwind CSS v4 (latest)
- ✅ Azure AD B2C authentication integration
- ✅ Responsive design
- ✅ Modern UI components

## Project Structure

```
src/
├── app/
│   ├── profile/
│   │   └── page.tsx          # Profile page
│   ├── globals.css           # Global styles with Tailwind
│   ├── layout.tsx            # Root layout
│   └── page.tsx              # Home/login page
├── components/
│   ├── LoginPage.tsx         # Login component
│   └── ProfilePage.tsx       # Profile component
└── services/
    └── apiService.ts         # API service for auth
```

## Getting Started

1. **Install dependencies:**
   ```bash
   npm install
   ```

2. **Run the development server:**
   ```bash
   npm run dev
   ```

3. **Open your browser:**
   Navigate to [http://localhost:3000](http://localhost:3000)

## Pages

- **Home (`/`)**: Login page with Azure AD B2C authentication
- **Profile (`/profile`)**: User profile page showing authenticated user data

## API Integration

The application connects to your backend API at `https://localhost:7000` with the following endpoints:

- `GET /oauth/adb2c/login` - Initiate login flow
- `GET /oauth/adb2c/logout` - Initiate logout flow  
- `GET /oauth/adb2c/profile` - Get user profile data

## Tailwind CSS

This project uses Tailwind CSS v4 with the new `@import "tailwindcss"` syntax. All styles are configured in `src/app/globals.css`.

## Development

- **Build for production:**
  ```bash
  npm run build
  ```

- **Start production server:**
  ```bash
  npm start
  ```

- **Lint code:**
  ```bash
  npm run lint
  ```

## Technologies Used

- Next.js 16
- React 19
- TypeScript
- Tailwind CSS v4
- Next.js App Router
- ESLint