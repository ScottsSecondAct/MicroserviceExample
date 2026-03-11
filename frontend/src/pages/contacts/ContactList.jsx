import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link, useNavigate } from 'react-router-dom'
import { contactsApi } from '../../api/contacts.api.js'
import { usersApi } from '../../api/users.api.js'

const STATUSES = ['Lead', 'Prospect', 'Customer', 'Churned']

export default function ContactList() {
  const navigate = useNavigate()
  const [statusFilter, setStatusFilter] = useState('')
  const [ownerFilter, setOwnerFilter] = useState('')

  const { data: contacts = [], isLoading, error } = useQuery({
    queryKey: ['contacts', { status: statusFilter, ownerId: ownerFilter }],
    queryFn: () => contactsApi.list({ status: statusFilter || undefined, ownerId: ownerFilter || undefined }),
  })

  const { data: team = [] } = useQuery({
    queryKey: ['team'],
    queryFn: usersApi.getTeam,
  })

  if (isLoading) return <p className="loading">Loading contacts…</p>
  if (error) return <p className="form-error">{error.message}</p>

  return (
    <div>
      <div className="page-header">
        <h1>Contacts</h1>
        <button className="btn btn-primary" onClick={() => navigate('/contacts/new')}>
          + New Contact
        </button>
      </div>

      <div className="filter-bar">
        <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}>
          <option value="">All statuses</option>
          {STATUSES.map((s) => <option key={s} value={s}>{s}</option>)}
        </select>
        <select value={ownerFilter} onChange={(e) => setOwnerFilter(e.target.value)}>
          <option value="">All owners</option>
          {team.map((m) => (
            <option key={m.userId} value={m.userId}>{m.displayName || m.userId}</option>
          ))}
        </select>
      </div>

      {contacts.length === 0 ? (
        <p className="empty">No contacts found.</p>
      ) : (
        <table className="data-table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Email</th>
              <th>Status</th>
              <th>Owner</th>
              <th>Created</th>
            </tr>
          </thead>
          <tbody>
            {contacts.map((c) => {
              const owner = team.find((m) => m.userId === c.ownerId)
              return (
                <tr key={c.contactId} onClick={() => navigate(`/contacts/${c.contactId}`)}>
                  <td><Link to={`/contacts/${c.contactId}`}>{c.firstName} {c.lastName}</Link></td>
                  <td>{c.email}</td>
                  <td><span className={`badge badge-${c.status.toLowerCase()}`}>{c.status}</span></td>
                  <td>{owner?.displayName ?? (c.ownerId ? c.ownerId.slice(0, 8) + '…' : '—')}</td>
                  <td>{new Date(c.createdAt).toLocaleDateString()}</td>
                </tr>
              )
            })}
          </tbody>
        </table>
      )}
    </div>
  )
}
