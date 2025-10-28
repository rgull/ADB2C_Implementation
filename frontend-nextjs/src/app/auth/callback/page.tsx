'use client';

import { useEffect, useState } from 'react';
import { useRouter } from 'next/navigation';

export default function AuthCallback() {
  const [status, setStatus] = useState<'loading' | 'success' | 'error'>('loading');
  const [message, setMessage] = useState('');
  const router = useRouter();

  useEffect(() => {
    const handleCallback = () => {
      try {
        const urlParams = new URLSearchParams(globalThis.location.search);
        const token = urlParams.get('token');
        const error = urlParams.get('error');
        const userId = urlParams.get('userId');

        console.log("token",token)


        if (error) {
          setStatus('error');
          setMessage(`Authentication failed: ${error}`);
          return;
        }

        if (token && userId) {
          localStorage.setItem('ADB2C_token', token);
          localStorage.setItem('userId', userId);
          document.cookie = `ADB2C_token=${token}; path=/; max-age=86400; secure; samesite=strict`;

          setStatus('success');
          setMessage('Authentication successful! Redirecting to dashboard...');

          setTimeout(() => router.push(`/profile?userId=${userId}`), 1500);
        } else {
          setStatus('error');
          setMessage('No authentication token or userId received');
        }
      } catch (err) {
        console.error('OAuth callback error:', err);
        setStatus('error');
        setMessage('An error occurred during authentication');
      }
    };

    handleCallback();
  }, [router]);

  return (
    <div className="flex items-center justify-center min-h-screen bg-gray-50 text-gray-800">
      <div className="flex flex-col items-center text-center bg-white p-8 rounded-2xl shadow-md w-full max-w-md space-y-4">
        {status === 'loading' && (
          <>
            <div className="w-12 h-12 border-4 border-gray-300 border-t-gray-800 rounded-full animate-spin mb-4"></div>
            <h2 className="text-2xl font-semibold mb-2">Processing Authentication...</h2>
            <p className="text-gray-500">Please wait while we complete your login.</p>
          </>
        )}

        {status === 'success' && (
          <>
            <h2 className="text-2xl font-semibold mb-2">Success!</h2>
            <p className="text-gray-600">{message}</p>
            <p className="text-sm text-gray-500">Redirecting ...</p>
          </>
        )}

        {status === 'error' && (
          <>
            <h2 className="text-2xl font-semibold mb-2 text-red-600">Authentication Failed</h2>
            <div className="bg-red-100 text-red-700 border border-red-200 p-3 rounded-lg w-full text-sm">
              {message}
            </div>
            <button
              onClick={() => router.push('/')}
              className="mt-2 bg-blue-600 hover:bg-blue-700 text-white font-medium py-2 px-4 rounded-lg transition-colors"
            >
              Try Again
            </button>
          </>
        )}
      </div>
    </div>
  );
}
