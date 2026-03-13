import { useQuery } from '@tanstack/react-query'
import { reportsApi } from '../../api/reports.api.js'
import { Card, CardContent, CardHeader, CardTitle } from '../../components/ui/card.jsx'
import { Skeleton } from '../../components/ui/skeleton.jsx'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../../components/ui/table.jsx'

function PipelineBar({ stage, dealCount, totalValue, maxValue }) {
  const pct = maxValue > 0 ? (totalValue / maxValue) * 100 : 0
  return (
    <div className="flex items-center gap-3 py-2">
      <div className="w-28 text-sm text-gray-700 font-medium truncate">{stage}</div>
      <div className="flex-1 h-2 bg-gray-100 rounded-full overflow-hidden">
        <div
          className="h-full bg-blue-500 rounded-full transition-all"
          style={{ width: `${pct}%` }}
        />
      </div>
      <div className="flex gap-3 text-xs text-gray-500 tabular-nums w-32 text-right justify-end">
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

  if (isLoading) {
    return (
      <div>
        <h1 className="text-2xl font-bold text-gray-900 mb-5">Dashboard</h1>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {[1, 2, 3].map((i) => (
            <Card key={i}>
              <CardContent className="pt-6">
                <Skeleton className="h-4 w-32 mb-4" />
                {[1, 2, 3].map((j) => <Skeleton key={j} className="h-8 w-full mb-2" />)}
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    )
  }

  if (error) return <p className="text-sm text-red-600 bg-red-50 px-3 py-2 rounded-md">Failed to load dashboard.</p>

  const { pipeline = [], activities = [], contacts = [] } = data

  const maxPipelineValue = Math.max(...pipeline.map(p => p.totalValue), 1)

  const statusOrder = ['Lead', 'Prospect', 'Customer', 'Churned']
  const funnelOrdered = statusOrder.map(s => contacts.find(c => c.status === s) ?? { status: s, count: 0 })
  const totalContacts = funnelOrdered.reduce((sum, c) => sum + c.count, 0)

  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-900 mb-5">Dashboard</h1>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {/* Pipeline summary */}
        <Card>
          <CardHeader><CardTitle>Pipeline by Stage</CardTitle></CardHeader>
          <CardContent className="pt-0">
            {pipeline.length === 0 ? (
              <p className="text-sm text-gray-400">No deals yet.</p>
            ) : (
              <div>
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
          </CardContent>
        </Card>

        {/* Contact funnel */}
        <Card>
          <CardHeader><CardTitle>Contact Funnel</CardTitle></CardHeader>
          <CardContent className="pt-0">
            {totalContacts === 0 ? (
              <p className="text-sm text-gray-400">No status transitions recorded yet.</p>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Status</TableHead>
                    <TableHead>Count</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {funnelOrdered.map(c => (
                    <TableRow key={c.status}>
                      <TableCell>{c.status}</TableCell>
                      <TableCell className="tabular-nums">{c.count}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </CardContent>
        </Card>

        {/* Activity counts by rep */}
        <Card>
          <CardHeader><CardTitle>Activity by Rep</CardTitle></CardHeader>
          <CardContent className="pt-0">
            {activities.length === 0 ? (
              <p className="text-sm text-gray-400">No activities logged yet.</p>
            ) : (
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Owner ID</TableHead>
                    <TableHead>Activities</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {activities.map(a => (
                    <TableRow key={a.ownerId}>
                      <TableCell className="font-mono text-xs">{a.ownerId}</TableCell>
                      <TableCell className="tabular-nums">{a.totalCount}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  )
}
