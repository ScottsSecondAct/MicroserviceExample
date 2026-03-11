import { useQuery } from '@tanstack/react-query'
import { Link, useNavigate } from 'react-router-dom'
import { accountsApi } from '../../api/accounts.api.js'

export default function AccountList() {
  const navigate = useNavigate()

  const { data: accounts = [], isLoading, error } = useQuery({
    queryKey: ['accounts'],
    queryFn: accountsApi.list,
  })

  if (isLoading) return <p className="loading">Loading accounts…</p>
  if (error) return <p className="form-error">{error.message}</p>

  return (
    <div>
      <div className="page-header">
        <h1>Accounts</h1>
        <button className="btn btn-primary" onClick={() => navigate('/accounts/new')}>
          + New Account
        </button>
      </div>

      {accounts.length === 0 ? (
        <p className="empty">No accounts found.</p>
      ) : (
        <table className="data-table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Industry</th>
              <th>Size</th>
              <th>Website</th>
              <th>Created</th>
            </tr>
          </thead>
          <tbody>
            {accounts.map((a) => (
              <tr key={a.accountId} onClick={() => navigate(`/accounts/${a.accountId}`)}>
                <td><Link to={`/accounts/${a.accountId}`}>{a.name}</Link></td>
                <td>{a.industry ?? '—'}</td>
                <td>{a.size ?? '—'}</td>
                <td>{a.website ? <a href={a.website} target="_blank" rel="noreferrer" onClick={(e) => e.stopPropagation()}>{a.website}</a> : '—'}</td>
                <td>{new Date(a.createdAt).toLocaleDateString()}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}
