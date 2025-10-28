'use client'

import { useEffect, useState } from 'react'
import { useRouter } from 'next/navigation'
import { authService } from '@/services/apiService'
import LoginPage from '@/components/LoginPage'

export default function Home() {
  const [isLoading, setIsLoading] = useState(true)
  const [isAuthenticated, setIsAuthenticated] = useState(false)
  const router = useRouter()

  // useEffect(() => {
  //   const checkAuth = async () => {
  //     try {
  //       const authenticated = await authService.isAuthenticated()
  //       setIsAuthenticated(authenticated)
        
  //       if (authenticated) {
  //         // After login, redirect to profile page
  //         router.push('/profile')
  //       }
  //     } catch (error) {
  //       console.error('Auth check error:', error)
  //     } finally {
  //       setIsLoading(false)
  //     }
  //   }

  //   checkAuth()
  // }, [router])

  if (false) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <div className="animate-spin rounded-full h-32 w-32 border-b-2 border-blue-600"></div>
      </div>
    )
  }

  return <LoginPage />
}
