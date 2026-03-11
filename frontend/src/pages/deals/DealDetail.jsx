import { useParams, useNavigate, Link } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { dealsApi } from '../../api/deals.api.js'
import { contactsApi } from '../../api/contacts.api.js'
import { accountsApi } from '../../api/accounts.api.js'
import ActivityTimeline from '../../components/ActivityTimeline.jsx'
import ActivityLogForm from '../../components/ActivityLogForm.jsx'

const STAGES = ['Prospecting', 'Proposal', 'Negotiation', 'ClosedWon', 'ClosedLost']
const ROLES = ['DecisionMaker', 'Influencer', 'Champion']

export default function DealDetail() {
  const { id } = useParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [contactId, setContactId] = useState('')
  const [contactRole, setContactRole] = useState('Influencer')

  const { data: deal, isLoading, error } = useQuery({
    queryKey: ['deal', id],
    queryFn: () => dealsApi.get(id),
  })

  const { data: allContacts = [] } = useQuery({
    queryKey: ['contacts'],
    queryFn: () => contactsApi.list(),
  })

  const { data: account } = useQuery({
    queryKey: ['account', deal?.accountId],
    queryFn: () => accountsApi.get(deal.accountId),
    enabled: !!deal?.accountId,
  })

  const stageUpdate = useMutation({
    mutationFn: (stage) => dealsApi.update(id, { stage }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['deal', id] }),
  })

  const deleteDeal = useMutation({
    mutationFn: () => dealsApi.delete(id),
    onSuccess: () => navigate('/deals'),
  })

  const addContact = useMutation({
    mutationFn: () => dealsApi.addContact(id, { contactId, role: contactRole }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['deal', id] })
      setContactId('')
    },
  })

  const removeContact = useMutation({
    mutationFn: (cid) => dealsApi.removeContact(id, cid),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['deal', id] }),
  })

  if (isLoading) return <p className="loading">Loading…</p>
  if (error) return <p className="form-error">{error.message}</p>
  if (!deal) return <p className="form-error">Deal not found.</p>

  const currentStageIndex = STAGES.indexOf(deal.stage)
  const isClosed = deal.stage === 'ClosedWon' || deal.stage === 'ClosedLost'

  const associatedContactIds = new Set(deal.contacts?.map((c) => c.contactId))
  const availableContacts = allContacts.filter((c) => !associatedContactIds.has(c.contactId))

  return (
    <div style={{ maxWidth: '800px' }}>
      <div className="page-header">
        <div>
          <h1>{deal.title}</h1>
          {account && <p style={{ color: '#64748b', margin: 0 }}>{account.name}</p>}
        </div>
        <div style={{ display: 'flex', gap: '0.5rem' }}>
          <button className="btn" onClick={() => navigate(`/deals/${id}/edit`)}>Edit</button>
          <button className="btn btn-danger" onClick={() => deleteDeal.mutate()}>Delete</button>
        </div>
      </div>

      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem', marginBottom: '1.5rem' }}>
        <div className="detail-card">
          <label>Value</label>
          <p>${deal.value.toLocaleString()}</p>
        </div>
        <div className="detail-card">
          <label>Probability</label>
          <p>{deal.probability != null ? `${deal.probability}%` : '—'}</p>
        </div>
        <div className="detail-card">
          <label>Expected Close</label>
          <p>{deal.expectedCloseDate ? new Date(deal.expectedCloseDate).toLocaleDateString() : '—'}</p>
        </div>
        <div className="detail-card">
          <label>Stage</label>
          <p><span className={`badge badge-${deal.stage.toLowerCase()}`}>{deal.stage}</span></p>
        </div>
      </div>

      {!isClosed && (
        <div style={{ marginBottom: '1.5rem' }}>
          <h3>Move to Stage</h3>
          <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap' }}>
            {STAGES.filter((s) => s !== deal.stage).map((s) => (
              <button
                key={s}
                className="btn"
                disabled={stageUpdate.isPending}
                onClick={() => stageUpdate.mutate(s)}
              >
                {s}
              </button>
            ))}
          </div>
        </div>
      )}

      <div>
        <h3>Associated Contacts</h3>
        {deal.contacts?.length > 0 ? (
          <table className="data-table" style={{ marginBottom: '1rem' }}>
            <thead>
              <tr><th>Contact</th><th>Role</th><th></th></tr>
            </thead>
            <tbody>
              {deal.contacts.map((dc) => {
                const contact = allContacts.find((c) => c.contactId === dc.contactId)
                return (
                  <tr key={dc.dealContactId}>
                    <td>
                      {contact
                        ? <Link to={`/contacts/${dc.contactId}`}>{contact.firstName} {contact.lastName}</Link>
                        : dc.contactId.slice(0, 8) + '…'}
                    </td>
                    <td>{dc.role}</td>
                    <td>
                      <button className="btn btn-sm btn-danger"
                        onClick={() => removeContact.mutate(dc.contactId)}>
                        Remove
                      </button>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        ) : (
          <p className="empty">No contacts associated.</p>
        )}

        {availableContacts.length > 0 && (
          <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center', flexWrap: 'wrap' }}>
            <select value={contactId} onChange={(e) => setContactId(e.target.value)}>
              <option value="">Select contact…</option>
              {availableContacts.map((c) => (
                <option key={c.contactId} value={c.contactId}>
                  {c.firstName} {c.lastName}
                </option>
              ))}
            </select>
            <select value={contactRole} onChange={(e) => setContactRole(e.target.value)}>
              {ROLES.map((r) => <option key={r} value={r}>{r}</option>)}
            </select>
            <button
              className="btn btn-primary"
              disabled={!contactId || addContact.isPending}
              onClick={() => addContact.mutate()}
            >
              Add Contact
            </button>
          </div>
        )}
      </div>

      <div style={{ marginTop: '1.5rem' }}>
        <div className="card-header" style={{ marginBottom: '0.75rem' }}>
          <h3 style={{ fontSize: 16, fontWeight: 600, color: '#111827', margin: 0 }}>Activity</h3>
          <ActivityLogForm dealId={id} queryKey="deal-activities" />
        </div>
        <ActivityTimeline dealId={id} queryKey="deal-activities" />
      </div>
    </div>
  )
}
