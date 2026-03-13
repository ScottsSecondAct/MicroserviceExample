import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { dealsApi } from '../../api/deals.api.js'
import { Button } from '../../components/ui/button.jsx'
import { Skeleton } from '../../components/ui/skeleton.jsx'

const STAGES = ['Prospecting', 'Proposal', 'Negotiation', 'ClosedWon', 'ClosedLost']

const STAGE_STYLES = {
  Prospecting: { header: 'text-indigo-600', border: 'border-indigo-200 bg-indigo-50/30' },
  Proposal:    { header: 'text-amber-600',  border: 'border-amber-200 bg-amber-50/30' },
  Negotiation: { header: 'text-blue-600',   border: 'border-blue-200 bg-blue-50/30' },
  ClosedWon:   { header: 'text-emerald-600', border: 'border-emerald-200 bg-emerald-50/30' },
  ClosedLost:  { header: 'text-red-500',    border: 'border-red-200 bg-red-50/30' },
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

  if (isLoading) {
    return (
      <div>
        <div className="flex items-center justify-between mb-5">
          <h1 className="text-2xl font-bold text-gray-900">Pipeline</h1>
        </div>
        <div className="flex gap-4 overflow-x-auto pb-4">
          {STAGES.map((s) => (
            <div key={s} className="flex-shrink-0 w-56">
              <Skeleton className="h-8 w-full mb-3" />
              {[1, 2].map((i) => <Skeleton key={i} className="h-20 w-full mb-2 rounded-lg" />)}
            </div>
          ))}
        </div>
      </div>
    )
  }

  if (error) return <p className="text-sm text-red-600 bg-red-50 px-3 py-2 rounded-md">{error.message}</p>

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
      <div className="flex items-center justify-between mb-5">
        <h1 className="text-2xl font-bold text-gray-900">Pipeline</h1>
        <Button onClick={() => navigate('/deals/new')}>+ New Deal</Button>
      </div>

      <div className="flex gap-4 overflow-x-auto pb-4 items-start">
        {board.map((col) => {
          const style = STAGE_STYLES[col.stage] ?? { header: 'text-gray-600', border: 'border-gray-200 bg-gray-50' }
          return (
            <div
              key={col.stage}
              onDrop={(e) => handleDrop(e, col.stage)}
              onDragOver={handleDragOver}
              className={`flex-shrink-0 w-56 rounded-lg border-2 border-transparent p-3 ${style.border}`}
              style={{ minWidth: '220px' }}
            >
              <div className="flex items-center justify-between mb-3">
                <span className={`text-sm font-semibold ${style.header}`}>{col.stage}</span>
                <span className="text-xs text-gray-400 tabular-nums">
                  ${(col.totalValue ?? 0).toLocaleString()}
                </span>
              </div>

              {col.deals.map((deal) => (
                <div
                  key={deal.dealId}
                  draggable
                  onDragStart={(e) => handleDragStart(e, deal)}
                  onClick={() => navigate(`/deals/${deal.dealId}`)}
                  className="bg-white border border-gray-200 rounded-lg p-2.5 mb-2 cursor-grab hover:shadow-sm transition-shadow select-none active:cursor-grabbing"
                >
                  <div className="text-sm font-medium text-gray-900 mb-1">{deal.title}</div>
                  <div className="text-xs text-gray-500 tabular-nums">
                    ${deal.value.toLocaleString()}
                    {deal.probability != null && ` · ${deal.probability}%`}
                  </div>
                </div>
              ))}

              {col.deals.length === 0 && (
                <p className="text-xs text-gray-400 text-center my-4">Drop here</p>
              )}
            </div>
          )
        })}
      </div>
    </div>
  )
}
