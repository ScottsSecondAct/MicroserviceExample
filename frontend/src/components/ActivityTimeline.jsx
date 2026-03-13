import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { activitiesApi } from '../api/activities.api.js'
import { Button } from './ui/button.jsx'
import { Skeleton } from './ui/skeleton.jsx'

const TYPE_BORDER = {
  Call:    'border-l-blue-500',
  Email:   'border-l-violet-500',
  Meeting: 'border-l-amber-500',
  Task:    'border-l-emerald-500',
  Note:    'border-l-gray-400',
}

const TYPE_ICONS = {
  Call:    '📞',
  Email:   '✉️',
  Meeting: '🤝',
  Task:    '✓',
  Note:    '📝',
}

function formatDate(iso) {
  if (!iso) return '—'
  return new Date(iso).toLocaleString(undefined, {
    month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit',
  })
}

export default function ActivityTimeline({ contactId, dealId, accountId, queryKey }) {
  const queryClient = useQueryClient()

  const params = {}
  if (contactId) params.contactId = contactId
  if (dealId) params.dealId = dealId
  if (accountId) params.accountId = accountId

  const { data: activities = [], isLoading } = useQuery({
    queryKey: [queryKey ?? 'activities', params],
    queryFn: () => activitiesApi.list(params),
  })

  const completeMutation = useMutation({
    mutationFn: (id) => activitiesApi.update(id, { completedAt: new Date().toISOString() }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [queryKey ?? 'activities', params] }),
  })

  const deleteMutation = useMutation({
    mutationFn: (id) => activitiesApi.delete(id),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [queryKey ?? 'activities', params] }),
  })

  if (isLoading) {
    return (
      <div className="flex flex-col gap-3">
        {[1, 2, 3].map((i) => (
          <Skeleton key={i} className="h-16 w-full rounded-lg" />
        ))}
      </div>
    )
  }

  if (activities.length === 0) {
    return <p className="text-sm text-gray-400 py-2">No activities yet.</p>
  }

  return (
    <div className="flex flex-col gap-3">
      {activities.map((a) => (
        <div
          key={a.activityId}
          className={`flex gap-3 p-3 rounded-lg border-l-[3px] bg-gray-50 ${TYPE_BORDER[a.type] ?? 'border-l-gray-300'}`}
        >
          <div className="text-base w-6 flex-shrink-0 pt-px">{TYPE_ICONS[a.type] ?? '•'}</div>
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 flex-wrap mb-1">
              <span className="text-[11px] font-bold uppercase tracking-wider text-gray-500">
                {a.type}
              </span>
              {a.type === 'Task' && (
                a.completedAt
                  ? <span className="text-xs text-emerald-600">Completed {formatDate(a.completedAt)}</span>
                  : (
                    <Button
                      size="sm"
                      variant="outline"
                      className="h-6 text-xs px-2"
                      disabled={completeMutation.isPending}
                      onClick={() => completeMutation.mutate(a.activityId)}
                    >
                      Mark complete
                    </Button>
                  )
              )}
              <span className="ml-auto text-xs text-gray-400">{formatDate(a.createdAt)}</span>
              <Button
                size="sm"
                variant="ghost"
                className="h-6 w-6 p-0 text-gray-400 hover:text-red-500 hover:bg-red-50"
                disabled={deleteMutation.isPending}
                onClick={() => deleteMutation.mutate(a.activityId)}
              >
                ×
              </Button>
            </div>
            <p className="text-sm font-medium text-gray-900 m-0">{a.subject}</p>
            {a.notes && <p className="text-sm text-gray-500 mt-0.5">{a.notes}</p>}
            {a.scheduledAt && !a.completedAt && (
              <p className="text-xs text-amber-600 mt-0.5">Scheduled: {formatDate(a.scheduledAt)}</p>
            )}
          </div>
        </div>
      ))}
    </div>
  )
}
