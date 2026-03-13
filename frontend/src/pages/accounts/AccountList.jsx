import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Link, useNavigate } from 'react-router-dom'
import { Pencil, Trash2 } from 'lucide-react'
import { accountsApi } from '../../api/accounts.api.js'
import { toast } from '../../hooks/use-toast.js'
import AccountForm from './AccountForm.jsx'
import { Button } from '../../components/ui/button.jsx'
import { Skeleton } from '../../components/ui/skeleton.jsx'
import { Card } from '../../components/ui/card.jsx'
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

export default function AccountList() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [sheetOpen, setSheetOpen] = useState(false)
  const [editAccountId, setEditAccountId] = useState(null)
  const [deleteTarget, setDeleteTarget] = useState(null)
  const [bulkDeleteOpen, setBulkDeleteOpen] = useState(false)

  const { data: accounts = [], isLoading, error } = useQuery({
    queryKey: ['accounts'],
    queryFn: accountsApi.list,
  })

  const deleteMutation = useMutation({
    mutationFn: (id) => accountsApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['accounts'] })
      toast({ variant: 'success', title: 'Account deleted' })
      setDeleteTarget(null)
    },
    onError: (err) => toast({ variant: 'destructive', title: 'Delete failed', description: err.message }),
  })

  const bulkDeleteMutation = useMutation({
    mutationFn: (ids) => Promise.all([...ids].map((id) => accountsApi.delete(id))),
    onSuccess: (_, ids) => {
      queryClient.invalidateQueries({ queryKey: ['accounts'] })
      toast({ variant: 'success', title: `${[...ids].length} account${[...ids].length !== 1 ? 's' : ''} deleted` })
      clearSelection()
      setBulkDeleteOpen(false)
    },
    onError: (err) => toast({ variant: 'destructive', title: 'Bulk delete failed', description: err.message }),
  })

  const { sortedData: sortedAccounts, sortKey, sortDir, handleSort } = useSortableTable(accounts, 'name')
  const pagination = usePagination(sortedAccounts)

  const allPageIds = pagination.paginatedData.map((a) => a.accountId)
  const { selectedIds, selectedCount, toggleRow, toggleAll, clearSelection, isSelected, isAllSelected, isIndeterminate } = useBulkSelect()

  return (
    <div>
      <div className="flex items-center justify-between mb-5">
        <h1 className="text-2xl font-bold text-gray-900">Accounts</h1>
        <Button onClick={() => setSheetOpen(true)}>+ New Account</Button>
      </div>

      {isLoading ? (
        <Card className="p-4 flex flex-col gap-3">
          {[1, 2, 3].map((i) => <Skeleton key={i} className="h-10 w-full" />)}
        </Card>
      ) : error ? (
        <p className="text-sm text-red-600 bg-red-50 px-3 py-2 rounded-md">{error.message}</p>
      ) : accounts.length === 0 ? (
        <p className="text-sm text-gray-400 py-4">No accounts found.</p>
      ) : (
        <Card className="p-0 overflow-hidden">
          <BulkActionBar selectedCount={selectedCount} onClearSelection={clearSelection}>
            <Button
              size="sm"
              variant="destructive"
              className="h-7 text-xs"
              onClick={() => setBulkDeleteOpen(true)}
              disabled={bulkDeleteMutation.isPending}
            >
              Delete selected
            </Button>
          </BulkActionBar>

          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-10">
                  <input
                    type="checkbox"
                    className="h-4 w-4 rounded border-gray-300 accent-blue-600 cursor-pointer"
                    checked={isAllSelected(allPageIds)}
                    ref={(el) => { if (el) el.indeterminate = isIndeterminate(allPageIds) }}
                    onChange={() => toggleAll(allPageIds)}
                    aria-label="Select all"
                  />
                </TableHead>
                <TableHead className="cursor-pointer select-none" onClick={() => handleSort('name')}>
                  Name <SortIcon active={sortKey === 'name'} dir={sortDir} />
                </TableHead>
                <TableHead className="cursor-pointer select-none" onClick={() => handleSort('industry')}>
                  Industry <SortIcon active={sortKey === 'industry'} dir={sortDir} />
                </TableHead>
                <TableHead className="cursor-pointer select-none" onClick={() => handleSort('size')}>
                  Size <SortIcon active={sortKey === 'size'} dir={sortDir} />
                </TableHead>
                <TableHead>Website</TableHead>
                <TableHead className="cursor-pointer select-none" onClick={() => handleSort('createdAt')}>
                  Created <SortIcon active={sortKey === 'createdAt'} dir={sortDir} />
                </TableHead>
                <TableHead className="w-20" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {pagination.paginatedData.map((a) => {
                const selected = isSelected(a.accountId)
                return (
                  <TableRow
                    key={a.accountId}
                    className={`cursor-pointer group${selected ? ' bg-blue-50' : ''}`}
                    onClick={() => navigate(`/accounts/${a.accountId}`)}
                    data-state={selected ? 'selected' : undefined}
                  >
                    <TableCell onClick={(e) => e.stopPropagation()}>
                      <input
                        type="checkbox"
                        className="h-4 w-4 rounded border-gray-300 accent-blue-600 cursor-pointer"
                        checked={selected}
                        onChange={() => toggleRow(a.accountId)}
                        aria-label={`Select ${a.name}`}
                      />
                    </TableCell>
                    <TableCell className="font-medium">
                      <Link
                        to={`/accounts/${a.accountId}`}
                        className="text-blue-600 hover:underline no-underline"
                        onClick={(e) => e.stopPropagation()}
                      >
                        {a.name}
                      </Link>
                    </TableCell>
                    <TableCell>{a.industry ?? '—'}</TableCell>
                    <TableCell>{a.size ?? '—'}</TableCell>
                    <TableCell>
                      {a.website
                        ? <a href={a.website} target="_blank" rel="noreferrer" onClick={(e) => e.stopPropagation()} className="text-blue-600 hover:underline">{a.website}</a>
                        : '—'}
                    </TableCell>
                    <TableCell>{new Date(a.createdAt).toLocaleDateString()}</TableCell>
                    <TableCell>
                      <div className="flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity justify-end">
                        <Button
                          size="icon"
                          variant="ghost"
                          className="h-7 w-7"
                          onClick={(e) => { e.stopPropagation(); setEditAccountId(a.accountId) }}
                          title="Edit"
                        >
                          <Pencil size={13} />
                        </Button>
                        <Button
                          size="icon"
                          variant="ghost"
                          className="h-7 w-7 text-red-500 hover:text-red-600 hover:bg-red-50"
                          onClick={(e) => { e.stopPropagation(); setDeleteTarget(a) }}
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
            {...pagination}
            onPageChange={(page) => { pagination.handlePageChange(page); clearSelection() }}
            onPageSizeChange={(size) => { pagination.handlePageSizeChange(size); clearSelection() }}
          />
        </Card>
      )}

      {/* New Account Sheet */}
      <Sheet open={sheetOpen} onOpenChange={setSheetOpen}>
        <SheetContent className="sm:max-w-lg overflow-y-auto">
          <SheetHeader>
            <SheetTitle>New Account</SheetTitle>
          </SheetHeader>
          <div className="mt-5">
            <AccountForm
              onSuccess={(result) => {
                setSheetOpen(false)
                navigate(`/accounts/${result.accountId}`)
              }}
              onClose={() => setSheetOpen(false)}
            />
          </div>
        </SheetContent>
      </Sheet>

      {/* Edit Account Sheet */}
      <Sheet open={!!editAccountId} onOpenChange={(open) => { if (!open) setEditAccountId(null) }}>
        <SheetContent className="sm:max-w-lg overflow-y-auto">
          <SheetHeader>
            <SheetTitle>Edit Account</SheetTitle>
          </SheetHeader>
          <div className="mt-5">
            {editAccountId && (
              <AccountForm
                id={editAccountId}
                onSuccess={() => {
                  setEditAccountId(null)
                  queryClient.invalidateQueries({ queryKey: ['accounts'] })
                }}
                onClose={() => setEditAccountId(null)}
              />
            )}
          </div>
        </SheetContent>
      </Sheet>

      {/* Single Delete Confirmation Dialog */}
      <Dialog open={!!deleteTarget} onOpenChange={(open) => { if (!open) setDeleteTarget(null) }}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Delete Account</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete <strong>{deleteTarget?.name}</strong>?
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
              onClick={() => deleteMutation.mutate(deleteTarget.accountId)}
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
            <DialogTitle>Delete {selectedCount} Account{selectedCount !== 1 ? 's' : ''}</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete <strong>{selectedCount}</strong> selected account{selectedCount !== 1 ? 's' : ''}?
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
