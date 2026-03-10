import { useState, useEffect } from 'react'
import { getMe, getUserProfile } from '../api.js'

export default function Profile({ token, onLogout }) {
  const [me, setMe] = useState(null)
  const [profile, setProfile] = useState(null)
  const [error, setError] = useState(null)

  useEffect(() => {
    async function load() {
      try {
        const meData = await getMe(token)
        setMe(meData)
        const profileData = await getUserProfile(meData.userId, token)
        setProfile(profileData)
      } catch (err) {
        setError(err.message)
      }
    }
    load()
  }, [token])

  return (
    <div className="container">
      <div className="profile-header">
        <h2>Profile</h2>
        <button onClick={onLogout}>Logout</button>
      </div>
      {error && <p className="error">{error}</p>}
      {me && (
        <table>
          <tbody>
            <tr><th>User ID</th><td>{me.userId}</td></tr>
            <tr><th>Email</th><td>{me.email}</td></tr>
            <tr><th>Role</th><td>{me.role}</td></tr>
            {profile && (
              <>
                <tr><th>Display Name</th><td>{profile.displayName ?? '—'}</td></tr>
                <tr><th>Member Since</th><td>{new Date(profile.createdAt).toLocaleDateString()}</td></tr>
              </>
            )}
          </tbody>
        </table>
      )}
      {!me && !error && <p>Loading…</p>}
    </div>
  )
}
