import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { TrendingUp, TrendingDown, Minus, DollarSign, BarChart3, Users, Activity } from 'lucide-react'
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  Tooltip,
  ResponsiveContainer,
  Cell,
  FunnelChart,
  Funnel,
  LabelList,
} from 'recharts'
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

// Custom tooltip for the Pipeline BarChart
function PipelineTooltip({ active, payload }) {
  if (!active || !payload?.length) return null
  const { stage, dealCount, totalValue } = payload[0].payload
  return (
    <div className="bg-white border border-gray-200 rounded-lg shadow-lg px-3 py-2 text-sm">
      <p className="font-semibold text-gray-900 mb-1">{stage}</p>
      <p className="text-gray-500">{dealCount} deal{dealCount !== 1 ? 's' : ''}</p>
      <p className="text-blue-600 font-medium">${totalValue.toLocaleString()}</p>
    </div>
  )
}

// Custom tooltip for the Contact Funnel
function FunnelTooltip({ active, payload }) {
  if (!active || !payload?.length) return null
  const entry = payload[0].payload
  return (
    <div className="bg-white border border-gray-200 rounded-lg shadow-lg px-3 py-2 text-sm">
      <p className="font-semibold text-gray-900">{entry.status}</p>
      <p className="text-gray-500">{entry.count} contact{entry.count !== 1 ? 's' : ''}</p>
    </div>
  )
}

const STAGE_COLORS = ['#3b82f6', '#6366f1', '#8b5cf6', '#a855f7', '#ec4899']
const STAGE_COLORS_ACTIVE = ['#1d4ed8', '#4338ca', '#7c3aed', '#9333ea', '#db2777']

const FUNNEL_COLORS = ['#3b82f6', '#6366f1', '#10b981', '#f43f5e']
const FUNNEL_COLORS_ACTIVE = ['#1d4ed8', '#4338ca', '#059669', '#e11d48']

const OPEN_STAGES = new Set(['Prospecting', 'Proposal', 'Negotiation'])

export default function Dashboard() {
  const [selectedStage, setSelectedStage] = useState(null)
  const [selectedStatus, setSelectedStatus] = useState(null)

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

  const statusOrder = ['Lead', 'Prospect', 'Customer', 'Churned']
  const funnelOrdered = statusOrder.map(s => contacts.find(c => c.status === s) ?? { status: s, count: 0 })

  const handleStageClick = (data) => {
    if (!data?.activePayload?.[0]) return
    const stage = data.activePayload[0].payload.stage
    setSelectedStage(prev => prev === stage ? null : stage)
  }

  const handleFunnelClick = (entry) => {
    if (!entry?.status) return
    setSelectedStatus(prev => prev === entry.status ? null : entry.status)
  }

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
        {/* Pipeline by Stage — interactive horizontal BarChart */}
        <Card>
          <CardHeader><CardTitle>Pipeline by Stage</CardTitle></CardHeader>
          <CardContent className="pt-0">
            {pipeline.length === 0 ? (
              <p className="text-sm text-gray-400">No deals yet.</p>
            ) : (
              <>
                <ResponsiveContainer width="100%" height={pipeline.length * 44 + 20}>
                  <BarChart
                    data={pipeline}
                    layout="vertical"
                    margin={{ top: 4, right: 12, bottom: 4, left: 0 }}
                    onClick={handleStageClick}
                    style={{ cursor: 'pointer' }}
                  >
                    <XAxis
                      type="number"
                      tickFormatter={(v) => v >= 1000 ? `$${(v / 1000).toFixed(0)}k` : `$${v}`}
                      tick={{ fontSize: 11, fill: '#6b7280' }}
                      axisLine={false}
                      tickLine={false}
                    />
                    <YAxis
                      type="category"
                      dataKey="stage"
                      width={90}
                      tick={{ fontSize: 12, fill: '#374151' }}
                      axisLine={false}
                      tickLine={false}
                    />
                    <Tooltip content={<PipelineTooltip />} cursor={{ fill: '#f8fafc' }} />
                    <Bar dataKey="totalValue" radius={[0, 4, 4, 0]}>
                      {pipeline.map((entry, i) => (
                        <Cell
                          key={entry.stage}
                          fill={
                            selectedStage === entry.stage
                              ? STAGE_COLORS_ACTIVE[i % STAGE_COLORS_ACTIVE.length]
                              : STAGE_COLORS[i % STAGE_COLORS.length]
                          }
                          opacity={selectedStage && selectedStage !== entry.stage ? 0.45 : 1}
                        />
                      ))}
                    </Bar>
                  </BarChart>
                </ResponsiveContainer>
                {selectedStage && (() => {
                  const p = pipeline.find(p => p.stage === selectedStage)
                  return (
                    <div className="mt-2 px-3 py-2 bg-blue-50 rounded-md text-sm flex items-center justify-between">
                      <span className="font-medium text-blue-800">{p.stage}</span>
                      <span className="text-blue-700">{p.dealCount} deal{p.dealCount !== 1 ? 's' : ''} · ${p.totalValue.toLocaleString()}</span>
                    </div>
                  )
                })()}
              </>
            )}
          </CardContent>
        </Card>

        {/* Contact Funnel — interactive FunnelChart */}
        <Card>
          <CardHeader><CardTitle>Contact Funnel</CardTitle></CardHeader>
          <CardContent className="pt-0">
            {totalContacts === 0 ? (
              <p className="text-sm text-gray-400">No status transitions recorded yet.</p>
            ) : (
              <>
                <ResponsiveContainer width="100%" height={220}>
                  <FunnelChart>
                    <Tooltip content={<FunnelTooltip />} />
                    <Funnel
                      dataKey="count"
                      data={funnelOrdered}
                      isAnimationActive
                      onClick={handleFunnelClick}
                      style={{ cursor: 'pointer' }}
                    >
                      <LabelList
                        position="center"
                        content={({ x, y, width, height, index }) => {
                          if (!width || width < 40) return null
                          const entry = funnelOrdered[index]
                          return (
                            <text
                              x={x + width / 2}
                              y={y + height / 2}
                              textAnchor="middle"
                              dominantBaseline="middle"
                              fontSize={12}
                              fill="white"
                              fontWeight={500}
                            >
                              {entry?.status}
                            </text>
                          )
                        }}
                      />
                      {funnelOrdered.map((entry, i) => (
                        <Cell
                          key={entry.status}
                          fill={
                            selectedStatus === entry.status
                              ? FUNNEL_COLORS_ACTIVE[i % FUNNEL_COLORS_ACTIVE.length]
                              : FUNNEL_COLORS[i % FUNNEL_COLORS.length]
                          }
                          opacity={selectedStatus && selectedStatus !== entry.status ? 0.45 : 1}
                        />
                      ))}
                    </Funnel>
                  </FunnelChart>
                </ResponsiveContainer>
                {selectedStatus && (() => {
                  const c = funnelOrdered.find(c => c.status === selectedStatus)
                  return (
                    <div className="mt-2 px-3 py-2 bg-blue-50 rounded-md text-sm flex items-center justify-between">
                      <span className="font-medium text-blue-800">{c.status}</span>
                      <span className="text-blue-700">{c.count} contact{c.count !== 1 ? 's' : ''}</span>
                    </div>
                  )
                })()}
              </>
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
