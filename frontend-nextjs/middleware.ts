import { NextResponse } from 'next/server'
import type { NextRequest } from 'next/server'

export function middleware(request: NextRequest) {
  const token = request.cookies.get('ADB2C_token')
  const { pathname } = request.nextUrl

  // Define protected routes
  const protectedRoutes = ['/profile']
  
  // Check if the current path is a protected route
  const isProtectedRoute = protectedRoutes.some(route => pathname.startsWith(route))

  // If accessing a protected route without a token, redirect to home
  if (isProtectedRoute && !token) {
    const url = new URL('/', request.url)
    return NextResponse.redirect(url)
  }

  // If accessing the auth callback, allow it
  if (pathname.startsWith('/auth/callback')) {
    return NextResponse.next()
  }

  return NextResponse.next()
}

export const config = {
  matcher: [
    /*
     * Match all request paths except for the ones starting with:
     * - api (API routes)
     * - _next/static (static files)
     * - _next/image (image optimization files)
     * - favicon.ico (favicon file)
     */
    '/((?!api|_next/static|_next/image|favicon.ico).*)',
  ],
}

