import { useQuery } from '@tanstack/react-query'
import { authApi } from '../../api/auth.api.js'
import { usersApi } from '../../api/users.api.js'

export default function Profile() {
  const { data: me, isLoading: meLoading, error: meError } = useQuery({
    queryKey: ['me'],
    queryFn: authApi.me,
  })

  const { data: profile } = useQuery({
    queryKey: ['profile', me?.userId],
    queryFn: () => usersApi.getProfile(me.userId),
    enabled: !!me?.userId,
  })

  if (meLoading) return <p className="loading">Loading…</p>
  if (meError) return <p className="form-error">{meError.message}</p>

  return (
    <div>
      <h1>Profile</h1>
      <div className="card">
        <table className="detail-table">
          <tbody>
            <tr><th>User ID</th><td>{me?.userId}</td></tr>
            <tr><th>Email</th><td>{me?.email}</td></tr>
            <tr><th>Role</th><td>{me?.role}</td></tr>
            {profile && (
              <>
                <tr><th>Display Name</th><td>{profile.displayName ?? '—'}</td></tr>
                <tr><th>Member Since</th><td>{new Date(profile.createdAt).toLocaleDateString()}</td></tr>
              </>
            )}
          </tbody>
        </table>
      </div>
    </div>
  )
}
