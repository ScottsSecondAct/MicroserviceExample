import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { activitiesApi } from '../../api/activities.api.js'
import { useAuth } from '../../context/AuthContext.jsx'
import { Button } from '../../components/ui/button.jsx'
import { Skeleton } from '../../components/ui/skeleton.jsx'
import { Card, CardContent } from '../../components/ui/card.jsx'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../../components/ui/table.jsx'
import { useSortableTable, SortIcon } from '../../hooks/use-sortable-table.js'

function formatDate(iso) {
  if (!iso) return '—'
  return new Date(iso).toLocaleString(undefined, {
    month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit',
  })
}

export default function TaskList() {
  const { user } = useAuth()
  const queryClient = useQueryClient()

  const { data: tasks = [], isLoading, error } = useQuery({
    queryKey: ['tasks', user?.userId],
    queryFn: () => activitiesApi.list({ type: 'Task', ownerId: user?.userId }),
    enabled: !!user?.userId,
  })

  const completeMutation = useMutation({
    mutationFn: (id) => activitiesApi.update(id, { completedAt: new Date().toISOString() }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['tasks', user?.userId] }),
  })

  const incompleteTasks = tasks.filter((t) => !t.completedAt)
  const completedTasks = tasks.filter((t) => t.completedAt)

  const { sortedData: sortedIncomplete, sortKey, sortDir, handleSort } = useSortableTable(incompleteTasks, 'scheduledAt')
  const { sortedData: sortedCompleted, sortKey: compSortKey, sortDir: compSortDir, handleSort: handleCompSort } = useSortableTable(completedTasks, 'completedAt', 'desc')

  if (isLoading) {
    return (
      <div>
        <div className="flex items-center justify-between mb-5">
          <h1 className="text-2xl font-bold text-gray-900">My Tasks</h1>
        </div>
        <Card className="p-4 flex flex-col gap-3">
          {[1, 2, 3].map((i) => <Skeleton key={i} className="h-12 w-full" />)}
        </Card>
      </div>
    )
  }

  if (error) return <p className="text-sm text-red-600 bg-red-50 px-3 py-2 rounded-md">{error.message}</p>

  return (
    <div>
      <div className="flex items-center justify-between mb-5">
        <h1 className="text-2xl font-bold text-gray-900">My Tasks</h1>
        <span className="text-sm text-gray-500">{incompleteTasks.length} open</span>
      </div>

      {incompleteTasks.length === 0 ? (
        <Card>
          <CardContent className="py-6">
            <p className="text-sm text-gray-400">No open tasks. You're all caught up.</p>
          </CardContent>
        </Card>
      ) : (
        <Card className="p-0 overflow-hidden mb-6">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="cursor-pointer select-none" onClick={() => handleSort('subject')}>
                  Subject <SortIcon active={sortKey === 'subject'} dir={sortDir} />
                </TableHead>
                <TableHead className="cursor-pointer select-none" onClick={() => handleSort('scheduledAt')}>
                  Scheduled <SortIcon active={sortKey === 'scheduledAt'} dir={sortDir} />
                </TableHead>
                <TableHead>Linked To</TableHead>
                <TableHead></TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {sortedIncomplete.map((t) => (
                <TableRow key={t.activityId} className="cursor-default">
                  <TableCell className="font-medium">{t.subject}</TableCell>
                  <TableCell>{formatDate(t.scheduledAt)}</TableCell>
                  <TableCell>
                    <span className="flex gap-2 flex-wrap">
                      {t.contactId && <Link to={`/contacts/${t.contactId}`} className="text-blue-600 hover:underline text-xs">Contact</Link>}
                      {t.dealId && <Link to={`/deals/${t.dealId}`} className="text-blue-600 hover:underline text-xs">Deal</Link>}
                      {t.accountId && <Link to={`/accounts/${t.accountId}`} className="text-blue-600 hover:underline text-xs">Account</Link>}
                      {!t.contactId && !t.dealId && !t.accountId && '—'}
                    </span>
                  </TableCell>
                  <TableCell>
                    <Button
                      size="sm"
                      disabled={completeMutation.isPending}
                      onClick={() => completeMutation.mutate(t.activityId)}
                    >
                      Complete
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Card>
      )}

      {completedTasks.length > 0 && (
        <details>
          <summary className="cursor-pointer text-sm text-gray-500 mb-3 select-none">
            {completedTasks.length} completed task{completedTasks.length !== 1 ? 's' : ''}
          </summary>
          <Card className="p-0 overflow-hidden">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="cursor-pointer select-none" onClick={() => handleCompSort('subject')}>
                    Subject <SortIcon active={compSortKey === 'subject'} dir={compSortDir} />
                  </TableHead>
                  <TableHead className="cursor-pointer select-none" onClick={() => handleCompSort('completedAt')}>
                    Completed <SortIcon active={compSortKey === 'completedAt'} dir={compSortDir} />
                  </TableHead>
                  <TableHead>Linked To</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {sortedCompleted.map((t) => (
                  <TableRow key={t.activityId} className="cursor-default opacity-60">
                    <TableCell className="line-through">{t.subject}</TableCell>
                    <TableCell>{formatDate(t.completedAt)}</TableCell>
                    <TableCell>
                      <span className="flex gap-2 flex-wrap">
                        {t.contactId && <Link to={`/contacts/${t.contactId}`} className="text-blue-600 hover:underline text-xs">Contact</Link>}
                        {t.dealId && <Link to={`/deals/${t.dealId}`} className="text-blue-600 hover:underline text-xs">Deal</Link>}
                        {t.accountId && <Link to={`/accounts/${t.accountId}`} className="text-blue-600 hover:underline text-xs">Account</Link>}
                        {!t.contactId && !t.dealId && !t.accountId && '—'}
                      </span>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </Card>
        </details>
      )}
    </div>
  )
}
