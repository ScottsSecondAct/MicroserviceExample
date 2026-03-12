import { useState, useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { dealsApi } from '../../api/deals.api.js'
import { accountsApi } from '../../api/accounts.api.js'
import { usersApi } from '../../api/users.api.js'
import Breadcrumb from '../../components/Breadcrumb.jsx'

const STAGES = ['Prospecting', 'Proposal', 'Negotiation', 'ClosedWon', 'ClosedLost']

export default function DealForm() {
  const { id } = useParams()
  const isEdit = !!id
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const [form, setForm] = useState({
    title: '',
    accountId: '',
    stage: 'Prospecting',
    value: '',
    probability: '',
    expectedCloseDate: '',
    ownerId: '',
  })
  const [error, setError] = useState('')

  const { data: existing } = useQuery({
    queryKey: ['deal', id],
    queryFn: () => dealsApi.get(id),
    enabled: isEdit,
  })

  const { data: accounts = [] } = useQuery({
    queryKey: ['accounts'],
    queryFn: accountsApi.list,
  })

  const { data: team = [] } = useQuery({
    queryKey: ['team'],
    queryFn: usersApi.getTeam,
  })

  useEffect(() => {
    if (existing) {
      setForm({
        title: existing.title ?? '',
        accountId: existing.accountId ?? '',
        stage: existing.stage ?? 'Prospecting',
        value: existing.value ?? '',
        probability: existing.probability ?? '',
        expectedCloseDate: existing.expectedCloseDate
          ? existing.expectedCloseDate.slice(0, 10)
          : '',
        ownerId: existing.ownerId ?? '',
      })
    }
  }, [existing])

  const mutation = useMutation({
    mutationFn: (data) => isEdit ? dealsApi.update(id, data) : dealsApi.create(data),
    onSuccess: (deal) => {
      queryClient.invalidateQueries({ queryKey: ['pipeline'] })
      queryClient.invalidateQueries({ queryKey: ['deals'] })
      navigate(isEdit ? `/deals/${id}` : `/deals/${deal.dealId}`)
    },
    onError: (err) => setError(err.message),
  })

  const handleSubmit = (e) => {
    e.preventDefault()
    if (!form.title.trim()) { setError('Title is required.'); return }
    setError('')
    mutation.mutate({
      title: form.title,
      accountId: form.accountId || undefined,
      stage: form.stage,
      value: parseFloat(form.value) || 0,
      probability: form.probability !== '' ? parseInt(form.probability) : undefined,
      expectedCloseDate: form.expectedCloseDate || undefined,
      ownerId: form.ownerId || undefined,
    })
  }

  const set = (field) => (e) => setForm((f) => ({ ...f, [field]: e.target.value }))

  const breadcrumbItems = isEdit && existing
    ? [
        { label: 'Pipeline', to: '/deals' },
        { label: existing.title, to: `/deals/${id}` },
        { label: 'Edit' },
      ]
    : [
        { label: 'Pipeline', to: '/deals' },
        { label: 'New Deal' },
      ]

  return (
    <div style={{ maxWidth: '520px' }}>
      <Breadcrumb items={breadcrumbItems} />
      <h1>{isEdit ? 'Edit Deal' : 'New Deal'}</h1>

      {error && <p className="form-error">{error}</p>}

      <form onSubmit={handleSubmit}>
        <div className="form-group">
          <label>Title *</label>
          <input value={form.title} onChange={set('title')} required />
        </div>

        <div className="form-group">
          <label>Account</label>
          <select value={form.accountId} onChange={set('accountId')}>
            <option value="">— None —</option>
            {accounts.map((a) => (
              <option key={a.accountId} value={a.accountId}>{a.name}</option>
            ))}
          </select>
        </div>

        <div className="form-group">
          <label>Stage</label>
          <select value={form.stage} onChange={set('stage')}>
            {STAGES.map((s) => <option key={s} value={s}>{s}</option>)}
          </select>
        </div>

        <div className="form-group">
          <label>Value ($)</label>
          <input type="number" min="0" step="0.01" value={form.value} onChange={set('value')} />
        </div>

        <div className="form-group">
          <label>Probability (%)</label>
          <input type="number" min="0" max="100" value={form.probability} onChange={set('probability')} />
        </div>

        <div className="form-group">
          <label>Expected Close Date</label>
          <input type="date" value={form.expectedCloseDate} onChange={set('expectedCloseDate')} />
        </div>

        <div className="form-group">
          <label>Owner</label>
          <select value={form.ownerId} onChange={set('ownerId')}>
            <option value="">— Unassigned —</option>
            {team.map((m) => (
              <option key={m.userId} value={m.userId}>{m.displayName || m.userId}</option>
            ))}
          </select>
        </div>

        <div style={{ display: 'flex', gap: '0.5rem' }}>
          <button type="submit" className="btn btn-primary" disabled={mutation.isPending}>
            {isEdit ? 'Save Changes' : 'Create Deal'}
          </button>
          <button type="button" className="btn" onClick={() => navigate(-1)}>Cancel</button>
        </div>
      </form>
    </div>
  )
}
