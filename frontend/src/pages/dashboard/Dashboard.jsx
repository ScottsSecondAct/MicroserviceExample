import { useQuery } from '@tanstack/react-query'
import { TrendingUp, TrendingDown, Minus, DollarSign, BarChart3, Users, Activity } from 'lucide-react'
import { reportsApi } from '../../api/reports.api.js'
import { Card, CardContent, CardHeader, CardTitle } from '../../components/ui/card.jsx'
import { Skeleton } from '../../components/ui/skeleton.jsx'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../../components/ui/table.jsx'

// trend: { value: number, direction: 'up' | 'down' | 'neutral' } | null
// When trend is null the badge is not rendered (no prior-period data available yet).
function TrendBadge({ trend }) {
  if (!trend) return null

  if (trend.direction === 'up') {
    return (
      <span className="inline-flex items-center gap-0.5 text-xs font-medium text-emerald-600 bg-emerald-50 px-1.5 py-0.5 rounded-full">
        <TrendingUp size={11} />
        {trend.value > 0 ? `+${trend.value}%` : `${trend.value}%`}
      </span>
    )
  }

  if (trend.direction === 'down') {
    return (
      <span className="inline-flex items-center gap-0.5 text-xs font-medium text-red-500 bg-red-50 px-1.5 py-0.5 rounded-full">
        <TrendingDown size={11} />
        {trend.value}%
      </span>
    )
  }

  return (
    <span className="inline-flex items-center gap-0.5 text-xs font-medium text-gray-400 bg-gray-100 px-1.5 py-0.5 rounded-full">
      <Minus size={11} />
      0%
    </span>
  )
}

function StatCard({ label, value, icon, trend, subLabel }) {
  return (
    <Card>
      <CardContent className="pt-5 pb-5">
        <div className="flex items-start justify-between">
          <div className="flex flex-col gap-1 min-w-0">
            <span className="text-xs font-medium text-gray-500 uppercase tracking-wide truncate">{label}</span>
            <span className="text-3xl font-bold text-gray-900 tabular-nums leading-none">{value}</span>
            {(trend || subLabel) && (
              <div className="flex items-center gap-1.5 mt-1">
                <TrendBadge trend={trend} />
                {subLabel && <span className="text-xs text-gray-400">{subLabel}</span>}
              </div>
            )}
          </div>
          <div className="flex-shrink-0 p-2 bg-blue-50 rounded-lg ml-3">
            {icon}
          </div>
        </div>
      </CardContent>
    </Card>
  )
}

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

const OPEN_STAGES = new Set(['Prospecting', 'Proposal', 'Negotiation'])

export default function Dashboard() {
  const { data, isLoading, error } = useQuery({
    queryKey: ['dashboard'],
    queryFn: reportsApi.dashboard,
  })

  if (isLoading) {
    return (
      <div>
        <h1 className="text-2xl font-bold text-gray-900 mb-5">Dashboard</h1>
        {/* KPI stat card skeletons */}
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
          {[1, 2, 3, 4].map((i) => (
            <Card key={i}>
              <CardContent className="pt-5 pb-5">
                <div className="flex items-start justify-between">
                  <div className="flex flex-col gap-2 flex-1">
                    <Skeleton className="h-3 w-24" />
                    <Skeleton className="h-8 w-20" />
                    <Skeleton className="h-4 w-16" />
                  </div>
                  <Skeleton className="h-9 w-9 rounded-lg flex-shrink-0 ml-3" />
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
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

  // Derive KPI aggregates from the dashboard data
  const totalPipelineValue = pipeline.reduce((sum, p) => sum + p.totalValue, 0)
  const openDeals = pipeline
    .filter(p => OPEN_STAGES.has(p.stage))
    .reduce((sum, p) => sum + p.dealCount, 0)
  const totalContacts = contacts.reduce((sum, c) => sum + c.count, 0)
  const totalActivities = activities.reduce((sum, a) => sum + a.totalCount, 0)

  // Trend props are null until the reporting API provides prior-period comparison data.
  const kpiCards = [
    {
      label: 'Pipeline Value',
      value: `$${totalPipelineValue.toLocaleString()}`,
      icon: <DollarSign size={18} className="text-blue-600" />,
      trend: null,
      subLabel: 'vs last period',
    },
    {
      label: 'Open Deals',
      value: openDeals.toLocaleString(),
      icon: <BarChart3 size={18} className="text-blue-600" />,
      trend: null,
      subLabel: 'vs last period',
    },
    {
      label: 'Total Contacts',
      value: totalContacts.toLocaleString(),
      icon: <Users size={18} className="text-blue-600" />,
      trend: null,
      subLabel: 'vs last period',
    },
    {
      label: 'Activities Logged',
      value: totalActivities.toLocaleString(),
      icon: <Activity size={18} className="text-blue-600" />,
      trend: null,
      subLabel: 'vs last period',
    },
  ]

  const maxPipelineValue = Math.max(...pipeline.map(p => p.totalValue), 1)

  const statusOrder = ['Lead', 'Prospect', 'Customer', 'Churned']
  const funnelOrdered = statusOrder.map(s => contacts.find(c => c.status === s) ?? { status: s, count: 0 })

  return (
    <div>
      <h1 className="text-2xl font-bold text-gray-900 mb-5">Dashboard</h1>

      {/* KPI stat cards */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        {kpiCards.map((card) => (
          <StatCard
            key={card.label}
            label={card.label}
            value={card.value}
            icon={card.icon}
            trend={card.trend}
            subLabel={card.subLabel}
          />
        ))}
      </div>

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
