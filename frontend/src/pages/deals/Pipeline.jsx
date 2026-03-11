import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { dealsApi } from '../../api/deals.api.js'

const STAGES = ['Prospecting', 'Proposal', 'Negotiation', 'ClosedWon', 'ClosedLost']

const STAGE_COLORS = {
  Prospecting: '#6366f1',
  Proposal: '#f59e0b',
  Negotiation: '#3b82f6',
  ClosedWon: '#22c55e',
  ClosedLost: '#ef4444',
}

export default function Pipeline() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [dragging, setDragging] = useState(null)

  const { data: board = [], isLoading, error } = useQuery({
    queryKey: ['pipeline'],
    queryFn: dealsApi.getPipeline,
  })

  const updateStage = useMutation({
    mutationFn: ({ dealId, stage }) => dealsApi.update(dealId, { stage }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['pipeline'] }),
  })

  if (isLoading) return <p className="loading">Loading pipeline…</p>
  if (error) return <p className="form-error">{error.message}</p>

  const handleDragStart = (e, deal) => {
    setDragging(deal)
    e.dataTransfer.effectAllowed = 'move'
  }

  const handleDrop = (e, targetStage) => {
    e.preventDefault()
    if (dragging && dragging.stage !== targetStage) {
      updateStage.mutate({ dealId: dragging.dealId, stage: targetStage })
    }
    setDragging(null)
  }

  const handleDragOver = (e) => {
    e.preventDefault()
    e.dataTransfer.dropEffect = 'move'
  }

  return (
    <div>
      <div className="page-header">
        <h1>Pipeline</h1>
        <button className="btn btn-primary" onClick={() => navigate('/deals/new')}>
          + New Deal
        </button>
      </div>

      <div style={{ display: 'flex', gap: '1rem', overflowX: 'auto', alignItems: 'flex-start' }}>
        {board.map((col) => (
          <div
            key={col.stage}
            onDrop={(e) => handleDrop(e, col.stage)}
            onDragOver={handleDragOver}
            style={{
              minWidth: '220px',
              flex: '0 0 220px',
              background: '#f8fafc',
              borderRadius: '8px',
              padding: '0.75rem',
              border: '2px solid transparent',
            }}
          >
            <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.5rem' }}>
              <strong style={{ color: STAGE_COLORS[col.stage] || '#64748b' }}>{col.stage}</strong>
              <span style={{ fontSize: '0.75rem', color: '#64748b' }}>
                ${(col.totalValue ?? 0).toLocaleString()}
              </span>
            </div>

            {col.deals.map((deal) => (
              <div
                key={deal.dealId}
                draggable
                onDragStart={(e) => handleDragStart(e, deal)}
                onClick={() => navigate(`/deals/${deal.dealId}`)}
                style={{
                  background: '#fff',
                  border: '1px solid #e2e8f0',
                  borderRadius: '6px',
                  padding: '0.6rem',
                  marginBottom: '0.5rem',
                  cursor: 'grab',
                  userSelect: 'none',
                }}
              >
                <div style={{ fontWeight: 500, marginBottom: '0.25rem' }}>{deal.title}</div>
                <div style={{ fontSize: '0.8rem', color: '#64748b' }}>
                  ${deal.value.toLocaleString()}
                  {deal.probability != null && ` · ${deal.probability}%`}
                </div>
              </div>
            ))}

            {col.deals.length === 0 && (
              <p style={{ color: '#94a3b8', fontSize: '0.8rem', textAlign: 'center', margin: '1rem 0' }}>
                Drop here
              </p>
            )}
          </div>
        ))}
      </div>
    </div>
  )
}
