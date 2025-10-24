const API_BASE_URL = 'https://localhost:7000'

export const authService = {
  login: () => {
    window.location.href = `${API_BASE_URL}/oauth/adb2c/login`
  },

  logout: () => {
    window.location.href = `${API_BASE_URL}/oauth/adb2c/logout`
  },

  getProfile: async () => {
    const response = await fetch(`${API_BASE_URL}/oauth/adb2c/profile`, {
      credentials: 'include', // Important for cookie-based authentication
    })
    
    if (!response.ok) {
      throw new Error('Failed to fetch profile')
    }
    
    return response.json()
  },

  isAuthenticated: async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/oauth/adb2c/profile`, {
        credentials: 'include',
      })
      return response.ok
    } catch {
      return false
    }
  }
}

export default authService
