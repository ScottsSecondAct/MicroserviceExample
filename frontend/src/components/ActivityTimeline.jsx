import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { activitiesApi } from '../api/activities.api.js'

const TYPE_COLORS = {
  Call: 'activity-call',
  Email: 'activity-email',
  Meeting: 'activity-meeting',
  Task: 'activity-task',
  Note: 'activity-note',
}

const TYPE_ICONS = {
  Call: '📞',
  Email: '✉️',
  Meeting: '🤝',
  Task: '✓',
  Note: '📝',
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

  if (isLoading) return <p className="loading">Loading activities…</p>
  if (activities.length === 0) return <p className="empty">No activities yet.</p>

  return (
    <div className="activity-timeline">
      {activities.map((a) => (
        <div key={a.activityId} className={`activity-item ${TYPE_COLORS[a.type] ?? ''}`}>
          <div className="activity-icon">{TYPE_ICONS[a.type] ?? '•'}</div>
          <div className="activity-body">
            <div className="activity-header">
              <span className="activity-type">{a.type}</span>
              {a.type === 'Task' && (
                a.completedAt
                  ? <span className="activity-done">Completed {formatDate(a.completedAt)}</span>
                  : <button
                      className="btn btn-sm btn-secondary"
                      disabled={completeMutation.isPending}
                      onClick={() => completeMutation.mutate(a.activityId)}
                    >
                      Mark complete
                    </button>
              )}
              <span className="activity-date">{formatDate(a.createdAt)}</span>
              <button
                className="btn btn-sm btn-danger"
                disabled={deleteMutation.isPending}
                onClick={() => deleteMutation.mutate(a.activityId)}
              >
                ×
              </button>
            </div>
            <p className="activity-subject">{a.subject}</p>
            {a.notes && <p className="activity-notes">{a.notes}</p>}
            {a.scheduledAt && !a.completedAt && (
              <p className="activity-scheduled">Scheduled: {formatDate(a.scheduledAt)}</p>
            )}
          </div>
        </div>
      ))}
    </div>
  )
}
