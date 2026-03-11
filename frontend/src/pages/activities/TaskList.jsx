import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { activitiesApi } from '../../api/activities.api.js'
import { useAuth } from '../../context/AuthContext.jsx'

function formatDate(iso) {
  if (!iso) return '—'
  return new Date(iso).toLocaleString(undefined, {
    month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit',
  })
}

export default function TaskList() {
  const { user } = useAuth()
  const queryClient = useQueryClient()

  const { data: tasks = [], isLoading, error } = useQuery({
    queryKey: ['tasks', user?.userId],
    queryFn: () => activitiesApi.list({ type: 'Task', ownerId: user?.userId }),
    enabled: !!user?.userId,
  })

  const completeMutation = useMutation({
    mutationFn: (id) => activitiesApi.update(id, { completedAt: new Date().toISOString() }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['tasks', user?.userId] }),
  })

  const incompleteTasks = tasks.filter((t) => !t.completedAt)
  const completedTasks = tasks.filter((t) => t.completedAt)

  if (isLoading) return <p className="loading">Loading…</p>
  if (error) return <p className="form-error">{error.message}</p>

  return (
    <div>
      <div className="page-header">
        <h1>My Tasks</h1>
        <span style={{ color: '#6b7280', fontSize: 13 }}>
          {incompleteTasks.length} open
        </span>
      </div>

      {incompleteTasks.length === 0 ? (
        <div className="card">
          <p className="empty">No open tasks. You're all caught up.</p>
        </div>
      ) : (
        <div className="card">
          <table className="data-table">
            <thead>
              <tr>
                <th>Subject</th>
                <th>Scheduled</th>
                <th>Linked To</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {incompleteTasks.map((t) => (
                <tr key={t.activityId} style={{ cursor: 'default' }}>
                  <td>{t.subject}</td>
                  <td>{formatDate(t.scheduledAt)}</td>
                  <td>
                    <span style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
                      {t.contactId && <Link to={`/contacts/${t.contactId}`}>Contact</Link>}
                      {t.dealId && <Link to={`/deals/${t.dealId}`}>Deal</Link>}
                      {t.accountId && <Link to={`/accounts/${t.accountId}`}>Account</Link>}
                      {!t.contactId && !t.dealId && !t.accountId && '—'}
                    </span>
                  </td>
                  <td>
                    <button
                      className="btn btn-sm btn-primary"
                      disabled={completeMutation.isPending}
                      onClick={() => completeMutation.mutate(t.activityId)}
                    >
                      Complete
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {completedTasks.length > 0 && (
        <details style={{ marginTop: '1.5rem' }}>
          <summary style={{ cursor: 'pointer', color: '#6b7280', fontSize: 13, marginBottom: '0.75rem' }}>
            {completedTasks.length} completed task{completedTasks.length !== 1 ? 's' : ''}
          </summary>
          <div className="card">
            <table className="data-table">
              <thead>
                <tr><th>Subject</th><th>Completed</th><th>Linked To</th></tr>
              </thead>
              <tbody>
                {completedTasks.map((t) => (
                  <tr key={t.activityId} style={{ cursor: 'default', opacity: 0.6 }}>
                    <td style={{ textDecoration: 'line-through' }}>{t.subject}</td>
                    <td>{formatDate(t.completedAt)}</td>
                    <td>
                      <span style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
                        {t.contactId && <Link to={`/contacts/${t.contactId}`}>Contact</Link>}
                        {t.dealId && <Link to={`/deals/${t.dealId}`}>Deal</Link>}
                        {t.accountId && <Link to={`/accounts/${t.accountId}`}>Account</Link>}
                        {!t.contactId && !t.dealId && !t.accountId && '—'}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </details>
      )}
    </div>
  )
}
