import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { Pencil, Trash2 } from 'lucide-react'
import { activitiesApi } from '../../api/activities.api.js'
import { useAuth } from '../../context/AuthContext.jsx'
import { Button } from '../../components/ui/button.jsx'
import { Input } from '../../components/ui/input.jsx'
import { Label } from '../../components/ui/label.jsx'
import { Textarea } from '../../components/ui/textarea.jsx'
import { Skeleton } from '../../components/ui/skeleton.jsx'
import { Card, CardContent } from '../../components/ui/card.jsx'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../../components/ui/table.jsx'
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from '../../components/ui/sheet.jsx'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '../../components/ui/dialog.jsx'
import { useSortableTable, SortIcon } from '../../hooks/use-sortable-table.jsx'
import { usePagination } from '../../hooks/use-pagination.js'
import { Pagination } from '../../components/ui/pagination.jsx'
import { useBulkSelect } from '../../hooks/use-bulk-select.js'
import { BulkActionBar } from '../../components/BulkActionBar.jsx'

function formatDate(iso) {
  if (!iso) return '—'
  return new Date(iso).toLocaleString(undefined, {
    month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit',
  })
}

function toLocalDatetimeValue(iso) {
  if (!iso) return ''
  const d = new Date(iso)
  const pad = (n) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`
}

function TaskEditForm({ task, onSuccess, onClose }) {
  const queryClient = useQueryClient()
  const [subject, setSubject] = useState(task.subject ?? '')
  const [notes, setNotes] = useState(task.notes ?? '')
  const [scheduledAt, setScheduledAt] = useState(toLocalDatetimeValue(task.scheduledAt))
  const [error, setError] = useState(null)

  const mutation = useMutation({
    mutationFn: (data) => activitiesApi.update(task.activityId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks'] })
      onSuccess()
    },
    onError: (err) => setError(err.message),
  })

  function handleSubmit(e) {
    e.preventDefault()
    if (!subject.trim()) { setError('Subject is required.'); return }
    setError(null)
    mutation.mutate({
      subject: subject.trim(),
      notes: notes.trim() || null,
      scheduledAt: scheduledAt || null,
    })
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      {error && (
        <p className="text-sm text-red-600 bg-red-50 px-3 py-2 rounded-md border border-red-200">{error}</p>
      )}
      <div className="flex flex-col gap-1.5">
        <Label>Subject *</Label>
        <Input value={subject} onChange={(e) => setSubject(e.target.value)} required autoFocus />
      </div>
      <div className="flex flex-col gap-1.5">
        <Label>Scheduled At</Label>
        <Input
          type="datetime-local"
          value={scheduledAt}
          onChange={(e) => setScheduledAt(e.target.value)}
        />
      </div>
      <div className="flex flex-col gap-1.5">
        <Label>Notes</Label>
        <Textarea
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          rows={3}
          placeholder="Optional notes"
        />
      </div>
      <div className="flex gap-3 pt-2">
        <Button type="submit" disabled={mutation.isPending}>
          {mutation.isPending ? 'Saving…' : 'Save changes'}
        </Button>
        <Button type="button" variant="outline" onClick={onClose}>Cancel</Button>
      </div>
    </form>
  )
}

export default function TaskList() {
  const { user } = useAuth()
  const queryClient = useQueryClient()
  const [editTask, setEditTask] = useState(null)
  const [deleteTarget, setDeleteTarget] = useState(null)
  const [bulkDeleteOpen, setBulkDeleteOpen] = useState(false)

  const { data: tasks = [], isLoading, error } = useQuery({
    queryKey: ['tasks', user?.userId],
    queryFn: () => activitiesApi.list({ type: 'Task', ownerId: user?.userId }),
    enabled: !!user?.userId,
  })

  const completeMutation = useMutation({
    mutationFn: (id) => activitiesApi.update(id, { completedAt: new Date().toISOString() }),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['tasks', user?.userId] }),
  })

  const deleteMutation = useMutation({
    mutationFn: (id) => activitiesApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks', user?.userId] })
      setDeleteTarget(null)
    },
  })

  const bulkDeleteMutation = useMutation({
    mutationFn: (ids) => Promise.all([...ids].map((id) => activitiesApi.delete(id))),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks', user?.userId] })
      clearSelection()
      setBulkDeleteOpen(false)
    },
  })

  const bulkCompleteMutation = useMutation({
    mutationFn: (ids) =>
      Promise.all([...ids].map((id) => activitiesApi.update(id, { completedAt: new Date().toISOString() }))),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks', user?.userId] })
      clearSelection()
    },
  })

  const incompleteTasks = tasks.filter((t) => !t.completedAt)
  const completedTasks = tasks.filter((t) => t.completedAt)

  const { sortedData: sortedIncomplete, sortKey, sortDir, handleSort } = useSortableTable(incompleteTasks, 'scheduledAt')
  const { sortedData: sortedCompleted, sortKey: compSortKey, sortDir: compSortDir, handleSort: handleCompSort } = useSortableTable(completedTasks, 'completedAt', 'desc')
  const incompletePagination = usePagination(sortedIncomplete)
  const completedPagination = usePagination(sortedCompleted)

  const allIncompletePageIds = incompletePagination.paginatedData.map((t) => t.activityId)
  const { selectedIds, selectedCount, toggleRow, toggleAll, clearSelection, isSelected, isAllSelected, isIndeterminate } = useBulkSelect()

  const isBulkBusy = bulkDeleteMutation.isPending || bulkCompleteMutation.isPending

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
          <BulkActionBar selectedCount={selectedCount} onClearSelection={clearSelection}>
            <Button
              size="sm"
              variant="destructive"
              className="h-7 text-xs"
              onClick={() => setBulkDeleteOpen(true)}
              disabled={isBulkBusy}
            >
              Delete selected
            </Button>
            <Button
              size="sm"
              variant="outline"
              className="h-7 text-xs"
              onClick={() => bulkCompleteMutation.mutate(selectedIds)}
              disabled={isBulkBusy}
            >
              Mark complete
            </Button>
          </BulkActionBar>

          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-10">
                  <input
                    type="checkbox"
                    className="h-4 w-4 rounded border-gray-300 accent-blue-600 cursor-pointer"
                    checked={isAllSelected(allIncompletePageIds)}
                    ref={(el) => { if (el) el.indeterminate = isIndeterminate(allIncompletePageIds) }}
                    onChange={() => toggleAll(allIncompletePageIds)}
                    aria-label="Select all"
                  />
                </TableHead>
                <TableHead className="cursor-pointer select-none" onClick={() => handleSort('subject')}>
                  Subject <SortIcon active={sortKey === 'subject'} dir={sortDir} />
                </TableHead>
                <TableHead className="cursor-pointer select-none" onClick={() => handleSort('scheduledAt')}>
                  Scheduled <SortIcon active={sortKey === 'scheduledAt'} dir={sortDir} />
                </TableHead>
                <TableHead>Linked To</TableHead>
                <TableHead></TableHead>
                <TableHead className="w-20" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {incompletePagination.paginatedData.map((t) => {
                const selected = isSelected(t.activityId)
                return (
                  <TableRow
                    key={t.activityId}
                    className={`cursor-default group${selected ? ' bg-blue-50' : ''}`}
                    data-state={selected ? 'selected' : undefined}
                  >
                    <TableCell>
                      <input
                        type="checkbox"
                        className="h-4 w-4 rounded border-gray-300 accent-blue-600 cursor-pointer"
                        checked={selected}
                        onChange={() => toggleRow(t.activityId)}
                        aria-label={`Select ${t.subject}`}
                      />
                    </TableCell>
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
                    <TableCell>
                      <div className="flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity justify-end">
                        <Button
                          size="icon"
                          variant="ghost"
                          className="h-7 w-7"
                          onClick={() => setEditTask(t)}
                          title="Edit"
                        >
                          <Pencil size={13} />
                        </Button>
                        <Button
                          size="icon"
                          variant="ghost"
                          className="h-7 w-7 text-red-500 hover:text-red-600 hover:bg-red-50"
                          onClick={() => setDeleteTarget(t)}
                          title="Delete"
                        >
                          <Trash2 size={13} />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                )
              })}
            </TableBody>
          </Table>
          <Pagination
            {...incompletePagination}
            onPageChange={(page) => { incompletePagination.handlePageChange(page); clearSelection() }}
            onPageSizeChange={(size) => { incompletePagination.handlePageSizeChange(size); clearSelection() }}
          />
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
                  <TableHead className="w-12" />
                </TableRow>
              </TableHeader>
              <TableBody>
                {completedPagination.paginatedData.map((t) => (
                  <TableRow key={t.activityId} className="cursor-default opacity-60 group">
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
                    <TableCell>
                      <div className="flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity justify-end">
                        <Button
                          size="icon"
                          variant="ghost"
                          className="h-7 w-7 text-red-500 hover:text-red-600 hover:bg-red-50"
                          onClick={() => setDeleteTarget(t)}
                          title="Delete"
                        >
                          <Trash2 size={13} />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
            <Pagination
              {...completedPagination}
              onPageChange={completedPagination.handlePageChange}
              onPageSizeChange={completedPagination.handlePageSizeChange}
            />
          </Card>
        </details>
      )}

      {/* Edit Task Sheet */}
      <Sheet open={!!editTask} onOpenChange={(open) => { if (!open) setEditTask(null) }}>
        <SheetContent className="sm:max-w-md overflow-y-auto">
          <SheetHeader>
            <SheetTitle>Edit Task</SheetTitle>
          </SheetHeader>
          <div className="mt-5">
            {editTask && (
              <TaskEditForm
                task={editTask}
                onSuccess={() => setEditTask(null)}
                onClose={() => setEditTask(null)}
              />
            )}
          </div>
        </SheetContent>
      </Sheet>

      {/* Single Delete Confirmation Dialog */}
      <Dialog open={!!deleteTarget} onOpenChange={(open) => { if (!open) setDeleteTarget(null) }}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Delete Task</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete <strong>"{deleteTarget?.subject}"</strong>?
              This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDeleteTarget(null)} disabled={deleteMutation.isPending}>
              Cancel
            </Button>
            <Button
              variant="destructive"
              disabled={deleteMutation.isPending}
              onClick={() => deleteMutation.mutate(deleteTarget.activityId)}
            >
              {deleteMutation.isPending ? 'Deleting…' : 'Delete'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Bulk Delete Confirmation Dialog */}
      <Dialog open={bulkDeleteOpen} onOpenChange={(open) => { if (!open) setBulkDeleteOpen(false) }}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Delete {selectedCount} Task{selectedCount !== 1 ? 's' : ''}</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete <strong>{selectedCount}</strong> selected task{selectedCount !== 1 ? 's' : ''}?
              This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setBulkDeleteOpen(false)} disabled={bulkDeleteMutation.isPending}>
              Cancel
            </Button>
            <Button
              variant="destructive"
              disabled={bulkDeleteMutation.isPending}
              onClick={() => bulkDeleteMutation.mutate(selectedIds)}
            >
              {bulkDeleteMutation.isPending ? 'Deleting…' : `Delete ${selectedCount}`}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
