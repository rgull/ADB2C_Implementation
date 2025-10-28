const API_BASE_URL = 'https://localhost:7000'

export const authService = {
  login: () => {
    window.location.href = `${API_BASE_URL}/oauth/adb2c/login`
  },

// export const authService = {
//  login: async () => {
//   try {
//     const response = await fetch(`${API_BASE_URL}/oauth/adb2c/login`, {
//       method: 'GET',
//       credentials: 'include' // if cookies or tokens are needed
//     });

//     const data = await response.json();
//     console.log('Login response:', data);
//   } catch (error) {
//     console.error('Error during login:', error);
//   }
// },

  

  logout: () => {
    // Clear local storage
    localStorage.removeItem('ADB2C_token')
    localStorage.removeItem('userId')
    
    // Redirect to home/login page
    window.location.href = '/'
  },

  // Helper function to clear local auth without redirecting
  clearLocalAuth: () => {
    localStorage.removeItem('ADB2C_token')
    localStorage.removeItem('userId')
  },


  updateProfile: async (userId: string, profileData: any) => {
    const token = localStorage.getItem('ADB2C_token')
    
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
    }
    
    // Add token to headers if available
    if (token) {
      headers['Authorization'] = `Bearer ${token}`
    }
    
    const response = await fetch(`${API_BASE_URL}/api/profile/${userId}`, {
      method: 'PUT',
      headers,
      credentials: 'include',
      body: JSON.stringify(profileData)
    })
    
    if (!response.ok) {
      throw new Error('Failed to update profile')
    }
    
    return response.json()
  },

  getProfileById: async (userId: string) => {
    const token = localStorage.getItem('ADB2C_token')
    
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
    }
    
    // Add token to headers if available
    if (token) {
      headers['Authorization'] = `Bearer ${token}`
    }
    
    const response = await fetch(`${API_BASE_URL}/api/profile/${userId}`, {
      headers,
      credentials: 'include',
    })
    
    if (!response.ok) {
      throw new Error('Failed to fetch profile')
    }
    console.log('resposnsessss', response);
    
    return response.json()
  },

  getProfiles: async () => {
    const token = localStorage.getItem('ADB2C_token')
    
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
    }
    
    // Add token to headers if available
    if (token) {
      headers['Authorization'] = `Bearer ${token}`
    }
    
    const response = await fetch(`${API_BASE_URL}/api/profile/`, {
      headers,
      credentials: 'include',
    })
    
    if (!response.ok) {
      throw new Error('Failed to fetch profile')
    }
    console.log('resposnsessss profiles', response);
    
    return response.json()
  }
}

export default authService
