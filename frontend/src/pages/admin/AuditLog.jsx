import { useQuery } from '@tanstack/react-query'
import { usersApi } from '../../api/users.api.js'
import { Skeleton } from '../../components/ui/skeleton.jsx'

const ACTION_LABELS = {
  RoleChanged: 'Role changed',
  Deactivated: 'Deactivated',
  Reactivated: 'Reactivated',
  InviteResent: 'Invite resent',
}

function formatDate(iso) {
  return new Date(iso).toLocaleString(undefined, {
    year: 'numeric', month: 'short', day: 'numeric',
    hour: '2-digit', minute: '2-digit', second: '2-digit',
  })
}

function shortId(uuid) {
  return uuid?.slice(0, 8) ?? '—'
}

export default function AuditLog() {
  const { data: entries = [], isLoading, isError } = useQuery({
    queryKey: ['audit-log'],
    queryFn: () => usersApi.getAuditLog(),
  })

  return (
    <div className="max-w-5xl mx-auto">
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Audit Log</h1>
        <p className="text-sm text-gray-500 mt-1">Admin actions recorded in chronological order (newest first).</p>
      </div>

      {isLoading && (
        <div className="flex flex-col gap-2">
          {[1, 2, 3, 4, 5].map(i => <Skeleton key={i} className="h-12 w-full rounded-lg" />)}
        </div>
      )}

      {isError && (
        <p className="text-sm text-red-600 bg-red-50 px-4 py-3 rounded-lg border border-red-200">
          Failed to load audit log.
        </p>
      )}

      {!isLoading && !isError && entries.length === 0 && (
        <p className="text-sm text-gray-400 py-4">No audit entries yet.</p>
      )}

      {!isLoading && !isError && entries.length > 0 && (
        <div className="overflow-x-auto rounded-lg border border-gray-200 bg-white">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-100 bg-gray-50 text-left text-xs font-semibold uppercase tracking-wider text-gray-500">
                <th className="px-4 py-3">Timestamp</th>
                <th className="px-4 py-3">Action</th>
                <th className="px-4 py-3">Actor</th>
                <th className="px-4 py-3">Target</th>
                <th className="px-4 py-3">Details</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {entries.map(entry => (
                <tr key={entry.id} className="hover:bg-gray-50">
                  <td className="px-4 py-3 text-gray-500 whitespace-nowrap">{formatDate(entry.timestamp)}</td>
                  <td className="px-4 py-3 font-medium text-gray-900">{ACTION_LABELS[entry.action] ?? entry.action}</td>
                  <td className="px-4 py-3 font-mono text-gray-500 text-xs">{shortId(entry.actorUserId)}</td>
                  <td className="px-4 py-3 font-mono text-gray-500 text-xs">{shortId(entry.targetUserId)}</td>
                  <td className="px-4 py-3 text-gray-600">{entry.details ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
