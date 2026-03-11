import { useState } from 'react'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { activitiesApi } from '../api/activities.api.js'

const TYPES = ['Call', 'Email', 'Meeting', 'Task', 'Note']

export default function ActivityLogForm({ contactId, dealId, accountId, queryKey }) {
  const queryClient = useQueryClient()
  const [open, setOpen] = useState(false)
  const [type, setType] = useState('Note')
  const [subject, setSubject] = useState('')
  const [notes, setNotes] = useState('')
  const [scheduledAt, setScheduledAt] = useState('')
  const [error, setError] = useState('')

  const params = {}
  if (contactId) params.contactId = contactId
  if (dealId) params.dealId = dealId
  if (accountId) params.accountId = accountId

  const createMutation = useMutation({
    mutationFn: (data) => activitiesApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: [queryKey ?? 'activities', params] })
      setSubject('')
      setNotes('')
      setScheduledAt('')
      setError('')
      setOpen(false)
    },
    onError: (err) => setError(err.message),
  })

  function handleSubmit(e) {
    e.preventDefault()
    if (!subject.trim()) { setError('Subject is required.'); return }
    createMutation.mutate({
      type,
      subject: subject.trim(),
      notes: notes.trim() || undefined,
      scheduledAt: scheduledAt || undefined,
      ...params,
    })
  }

  return (
    <div className="activity-log-form">
      {!open ? (
        <button className="btn btn-secondary btn-sm" onClick={() => setOpen(true)}>
          + Log Activity
        </button>
      ) : (
        <form className="activity-form-panel" onSubmit={handleSubmit}>
          <div className="activity-form-row">
            <select value={type} onChange={(e) => setType(e.target.value)}>
              {TYPES.map((t) => <option key={t} value={t}>{t}</option>)}
            </select>
            <input
              placeholder="Subject"
              value={subject}
              onChange={(e) => setSubject(e.target.value)}
              autoFocus
            />
          </div>
          <textarea
            placeholder="Notes (optional)"
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            rows={2}
          />
          {type === 'Task' && (
            <label>
              Scheduled
              <input
                type="datetime-local"
                value={scheduledAt}
                onChange={(e) => setScheduledAt(e.target.value)}
              />
            </label>
          )}
          {error && <p className="form-error">{error}</p>}
          <div className="form-actions">
            <button type="submit" className="btn btn-primary" disabled={createMutation.isPending}>
              Save
            </button>
            <button type="button" className="btn btn-secondary" onClick={() => { setOpen(false); setError('') }}>
              Cancel
            </button>
          </div>
        </form>
      )}
    </div>
  )
}
