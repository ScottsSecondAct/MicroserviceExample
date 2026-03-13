import { useQuery } from '@tanstack/react-query'
import { authApi } from '../../api/auth.api.js'
import { usersApi } from '../../api/users.api.js'
import { Card, CardContent, CardHeader, CardTitle } from '../../components/ui/card.jsx'
import { Skeleton } from '../../components/ui/skeleton.jsx'

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

  if (meLoading) {
    return (
      <div>
        <Skeleton className="h-8 w-32 mb-5" />
        <Card>
          <CardContent className="pt-6 flex flex-col gap-3">
            {[1, 2, 3, 4].map((i) => <Skeleton key={i} className="h-8 w-full" />)}
          </CardContent>
        </Card>
      </div>
    )
  }

  if (meError) return <p className="text-sm text-red-600 bg-red-50 px-3 py-2 rounded-md">{meError.message}</p>

  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-900 mb-5">Profile</h1>
      <Card>
        <CardHeader><CardTitle>Account Details</CardTitle></CardHeader>
        <CardContent className="pt-0">
          <table className="w-full text-sm">
            <tbody>
              <tr><th className="text-left w-40 py-2 pr-3 text-gray-500 font-medium">User ID</th><td className="py-2 font-mono text-xs text-gray-700">{me?.userId}</td></tr>
              <tr><th className="text-left py-2 pr-3 text-gray-500 font-medium">Email</th><td className="py-2 text-gray-900">{me?.email}</td></tr>
              <tr><th className="text-left py-2 pr-3 text-gray-500 font-medium">Role</th><td className="py-2 text-gray-900 capitalize">{me?.role}</td></tr>
              {profile && (
                <>
                  <tr><th className="text-left py-2 pr-3 text-gray-500 font-medium">Display Name</th><td className="py-2 text-gray-900">{profile.displayName ?? '—'}</td></tr>
                  <tr><th className="text-left py-2 pr-3 text-gray-500 font-medium">Member Since</th><td className="py-2 text-gray-900">{new Date(profile.createdAt).toLocaleDateString()}</td></tr>
                </>
              )}
            </tbody>
          </table>
        </CardContent>
      </Card>
    </div>
  )
}
