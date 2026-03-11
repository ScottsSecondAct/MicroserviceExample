import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { accountsApi } from '../../api/accounts.api.js'
import { contactsApi } from '../../api/contacts.api.js'

export default function AccountDetail() {
  const { id } = useParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const { data: account, isLoading, error } = useQuery({
    queryKey: ['account', id],
    queryFn: () => accountsApi.get(id),
  })

  const { data: contacts = [] } = useQuery({
    queryKey: ['contacts', { accountId: id }],
    queryFn: () => contactsApi.list({ accountId: id }),
    enabled: !!id,
  })

  const deleteMutation = useMutation({
    mutationFn: () => accountsApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['accounts'] })
      navigate('/accounts')
    },
  })

  function handleDelete() {
    if (window.confirm('Delete this account?')) deleteMutation.mutate()
  }

  if (isLoading) return <p className="loading">Loading…</p>
  if (error) return <p className="form-error">{error.message}</p>
  if (!account) return null

  return (
    <div>
      <div className="page-header">
        <h1>{account.name}</h1>
        <div className="btn-group">
          <button className="btn btn-secondary" onClick={() => navigate(`/accounts/${id}/edit`)}>
            Edit
          </button>
          <button className="btn btn-danger" onClick={handleDelete} disabled={deleteMutation.isPending}>
            Delete
          </button>
        </div>
      </div>

      <div className="card">
        <h2 className="card-title">Details</h2>
        <table className="detail-table">
          <tbody>
            <tr><th>Industry</th><td>{account.industry ?? '—'}</td></tr>
            <tr><th>Size</th><td>{account.size ?? '—'}</td></tr>
            <tr><th>Website</th><td>{account.website ? <a href={account.website} target="_blank" rel="noreferrer">{account.website}</a> : '—'}</td></tr>
            <tr><th>Address</th><td>{[account.street, account.city, account.state, account.postalCode, account.country].filter(Boolean).join(', ') || '—'}</td></tr>
            <tr><th>Created</th><td>{new Date(account.createdAt).toLocaleString()}</td></tr>
            <tr><th>Updated</th><td>{new Date(account.updatedAt).toLocaleString()}</td></tr>
          </tbody>
        </table>
      </div>

      <div className="card">
        <div className="card-header">
          <h2 className="card-title">Contacts ({contacts.length})</h2>
          <button className="btn btn-secondary btn-sm" onClick={() => navigate(`/contacts/new`)}>
            + Add Contact
          </button>
        </div>
        {contacts.length === 0 ? (
          <p className="empty">No contacts linked to this account.</p>
        ) : (
          <table className="data-table">
            <thead>
              <tr><th>Name</th><th>Email</th><th>Status</th></tr>
            </thead>
            <tbody>
              {contacts.map((c) => (
                <tr key={c.contactId} onClick={() => navigate(`/contacts/${c.contactId}`)}>
                  <td><Link to={`/contacts/${c.contactId}`}>{c.firstName} {c.lastName}</Link></td>
                  <td>{c.email}</td>
                  <td><span className={`badge badge-${c.status.toLowerCase()}`}>{c.status}</span></td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  )
}
