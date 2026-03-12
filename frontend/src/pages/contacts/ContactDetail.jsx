import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { contactsApi } from '../../api/contacts.api.js'
import { accountsApi } from '../../api/accounts.api.js'
import { usersApi } from '../../api/users.api.js'
import ActivityTimeline from '../../components/ActivityTimeline.jsx'
import ActivityLogForm from '../../components/ActivityLogForm.jsx'
import Breadcrumb from '../../components/Breadcrumb.jsx'

const STATUS_TRANSITIONS = {
  Lead: ['Prospect', 'Churned'],
  Prospect: ['Customer', 'Churned'],
  Customer: ['Churned'],
  Churned: ['Lead'],
}

export default function ContactDetail() {
  const { id } = useParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const { data: contact, isLoading, error } = useQuery({
    queryKey: ['contact', id],
    queryFn: () => contactsApi.get(id),
  })

  const { data: account } = useQuery({
    queryKey: ['account', contact?.accountId],
    queryFn: () => accountsApi.get(contact.accountId),
    enabled: !!contact?.accountId,
  })

  const { data: team = [] } = useQuery({
    queryKey: ['team'],
    queryFn: usersApi.getTeam,
  })

  const updateMutation = useMutation({
    mutationFn: (data) => contactsApi.update(id, data),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['contact', id] }),
  })

  const deleteMutation = useMutation({
    mutationFn: () => contactsApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contacts'] })
      navigate('/contacts')
    },
  })

  function handleStatusChange(newStatus) {
    updateMutation.mutate({ status: newStatus })
  }

  function handleDelete() {
    if (window.confirm('Delete this contact?')) deleteMutation.mutate()
  }

  if (isLoading) return <p className="loading">Loading…</p>
  if (error) return <p className="form-error">{error.message}</p>
  if (!contact) return null

  const owner = team.find((m) => m.userId === contact.ownerId)
  const nextStatuses = STATUS_TRANSITIONS[contact.status] ?? []

  return (
    <div>
      <Breadcrumb items={[
        { label: 'Contacts', to: '/contacts' },
        { label: `${contact.firstName} ${contact.lastName}` },
      ]} />
      <div className="page-header">
        <h1>{contact.firstName} {contact.lastName}</h1>
        <div className="btn-group">
          <button className="btn btn-secondary" onClick={() => navigate(`/contacts/${id}/edit`)}>
            Edit
          </button>
          <button className="btn btn-danger" onClick={handleDelete} disabled={deleteMutation.isPending}>
            Delete
          </button>
        </div>
      </div>

      {nextStatuses.length > 0 && (
        <div className="status-actions">
          <span>Move to:</span>
          {nextStatuses.map((s) => (
            <button
              key={s}
              className={`btn badge-btn badge-${s.toLowerCase()}`}
              onClick={() => handleStatusChange(s)}
              disabled={updateMutation.isPending}
            >
              {s}
            </button>
          ))}
        </div>
      )}

      <div className="card">
        <table className="detail-table">
          <tbody>
            <tr><th>Status</th><td><span className={`badge badge-${contact.status.toLowerCase()}`}>{contact.status}</span></td></tr>
            <tr><th>Email</th><td>{contact.email}</td></tr>
            <tr><th>Phone</th><td>{contact.phone ?? '—'}</td></tr>
            <tr><th>Account</th><td>{account ? <Link to={`/accounts/${account.accountId}`}>{account.name}</Link> : (contact.accountId ? contact.accountId : '—')}</td></tr>
            <tr><th>Owner</th><td>{owner?.displayName ?? (contact.ownerId ? contact.ownerId : '—')}</td></tr>
            <tr><th>Created</th><td>{new Date(contact.createdAt).toLocaleString()}</td></tr>
            <tr><th>Updated</th><td>{new Date(contact.updatedAt).toLocaleString()}</td></tr>
          </tbody>
        </table>
      </div>

      <div className="card">
        <div className="card-header">
          <h2 className="card-title">Activity</h2>
          <ActivityLogForm contactId={id} queryKey="contact-activities" />
        </div>
        <ActivityTimeline contactId={id} queryKey="contact-activities" />
      </div>
    </div>
  )
}
