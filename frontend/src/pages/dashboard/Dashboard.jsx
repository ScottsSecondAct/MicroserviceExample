import { useQuery } from '@tanstack/react-query'
import { reportsApi } from '../../api/reports.api.js'

function PipelineBar({ stage, dealCount, totalValue, maxValue }) {
  const pct = maxValue > 0 ? (totalValue / maxValue) * 100 : 0
  return (
    <div className="pipeline-bar-row">
      <div className="pipeline-bar-label">{stage}</div>
      <div className="pipeline-bar-track">
        <div className="pipeline-bar-fill" style={{ width: `${pct}%` }} />
      </div>
      <div className="pipeline-bar-stats">
        <span>{dealCount} deal{dealCount !== 1 ? 's' : ''}</span>
        <span>${totalValue.toLocaleString()}</span>
      </div>
    </div>
  )
}

export default function Dashboard() {
  const { data, isLoading, error } = useQuery({
    queryKey: ['dashboard'],
    queryFn: reportsApi.dashboard,
  })

  if (isLoading) return <p>Loading dashboard…</p>
  if (error) return <p className="error">Failed to load dashboard.</p>

  const { pipeline = [], activities = [], contacts = [] } = data

  const maxPipelineValue = Math.max(...pipeline.map(p => p.totalValue), 1)

  const statusOrder = ['Lead', 'Prospect', 'Customer', 'Churned']
  const funnelOrdered = statusOrder.map(s => contacts.find(c => c.status === s) ?? { status: s, count: 0 })
  const totalContacts = funnelOrdered.reduce((sum, c) => sum + c.count, 0)

  return (
    <div className="dashboard">
      <h1>Dashboard</h1>

      <div className="dashboard-grid">
        {/* Pipeline summary */}
        <section className="dashboard-card">
          <h2>Pipeline by Stage</h2>
          {pipeline.length === 0 ? (
            <p className="empty">No deals yet.</p>
          ) : (
            <div className="pipeline-bars">
              {pipeline.map(p => (
                <PipelineBar
                  key={p.stage}
                  stage={p.stage}
                  dealCount={p.dealCount}
                  totalValue={p.totalValue}
                  maxValue={maxPipelineValue}
                />
              ))}
            </div>
          )}
        </section>

        {/* Contact funnel */}
        <section className="dashboard-card">
          <h2>Contact Funnel</h2>
          {totalContacts === 0 ? (
            <p className="empty">No status transitions recorded yet.</p>
          ) : (
            <table className="report-table">
              <thead>
                <tr><th>Status</th><th>Count</th></tr>
              </thead>
              <tbody>
                {funnelOrdered.map(c => (
                  <tr key={c.status}>
                    <td>{c.status}</td>
                    <td>{c.count}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </section>

        {/* Activity counts by rep */}
        <section className="dashboard-card">
          <h2>Activity by Rep</h2>
          {activities.length === 0 ? (
            <p className="empty">No activities logged yet.</p>
          ) : (
            <table className="report-table">
              <thead>
                <tr><th>Owner ID</th><th>Activities</th></tr>
              </thead>
              <tbody>
                {activities.map(a => (
                  <tr key={a.ownerId}>
                    <td className="mono">{a.ownerId}</td>
                    <td>{a.totalCount}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </section>
      </div>
    </div>
  )
}
