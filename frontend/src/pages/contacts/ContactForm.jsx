import { useState, useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { contactsApi } from '../../api/contacts.api.js'
import { accountsApi } from '../../api/accounts.api.js'
import { usersApi } from '../../api/users.api.js'

const STATUSES = ['Lead', 'Prospect', 'Customer', 'Churned']

const EMPTY = { firstName: '', lastName: '', email: '', phone: '', status: 'Lead', accountId: '', ownerId: '' }

export default function ContactForm() {
  const { id } = useParams()
  const isEdit = !!id
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [fields, setFields] = useState(EMPTY)
  const [error, setError] = useState(null)

  const { data: existing } = useQuery({
    queryKey: ['contact', id],
    queryFn: () => contactsApi.get(id),
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
      setFields({
        firstName: existing.firstName ?? '',
        lastName: existing.lastName ?? '',
        email: existing.email ?? '',
        phone: existing.phone ?? '',
        status: existing.status ?? 'Lead',
        accountId: existing.accountId ?? '',
        ownerId: existing.ownerId ?? '',
      })
    }
  }, [existing])

  const mutation = useMutation({
    mutationFn: (data) =>
      isEdit ? contactsApi.update(id, data) : contactsApi.create(data),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: ['contacts'] })
      if (isEdit) queryClient.invalidateQueries({ queryKey: ['contact', id] })
      navigate(isEdit ? `/contacts/${id}` : `/contacts/${result.contactId}`)
    },
    onError: (err) => setError(err.message),
  })

  function set(field, value) {
    setFields((prev) => ({ ...prev, [field]: value }))
  }

  function handleSubmit(e) {
    e.preventDefault()
    setError(null)
    const payload = {
      ...fields,
      accountId: fields.accountId || null,
      ownerId: fields.ownerId || null,
    }
    mutation.mutate(payload)
  }

  return (
    <div>
      <h1>{isEdit ? 'Edit Contact' : 'New Contact'}</h1>
      <div className="card">
        <form onSubmit={handleSubmit} className="edit-form">
          {error && <p className="form-error">{error}</p>}
          <div className="form-row">
            <label>
              First Name *
              <input value={fields.firstName} onChange={(e) => set('firstName', e.target.value)} required />
            </label>
            <label>
              Last Name *
              <input value={fields.lastName} onChange={(e) => set('lastName', e.target.value)} required />
            </label>
          </div>
          <div className="form-row">
            <label>
              Email *
              <input type="email" value={fields.email} onChange={(e) => set('email', e.target.value)} required />
            </label>
            <label>
              Phone
              <input type="tel" value={fields.phone} onChange={(e) => set('phone', e.target.value)} />
            </label>
          </div>
          <div className="form-row">
            <label>
              Status
              <select value={fields.status} onChange={(e) => set('status', e.target.value)}>
                {STATUSES.map((s) => <option key={s} value={s}>{s}</option>)}
              </select>
            </label>
            <label>
              Account
              <select value={fields.accountId} onChange={(e) => set('accountId', e.target.value)}>
                <option value="">— None —</option>
                {accounts.map((a) => <option key={a.accountId} value={a.accountId}>{a.name}</option>)}
              </select>
            </label>
          </div>
          <label>
            Owner
            <select value={fields.ownerId} onChange={(e) => set('ownerId', e.target.value)}>
              <option value="">— Unassigned —</option>
              {team.map((m) => <option key={m.userId} value={m.userId}>{m.displayName || m.userId}</option>)}
            </select>
          </label>
          <div className="form-actions">
            <button type="submit" className="btn btn-primary" disabled={mutation.isPending}>
              {mutation.isPending ? 'Saving…' : isEdit ? 'Save changes' : 'Create contact'}
            </button>
            <button type="button" className="btn btn-secondary" onClick={() => navigate(-1)}>
              Cancel
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
