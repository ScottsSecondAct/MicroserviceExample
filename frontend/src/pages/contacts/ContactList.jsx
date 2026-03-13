import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Link, useNavigate } from 'react-router-dom'
import { Pencil, Trash2 } from 'lucide-react'
import { contactsApi } from '../../api/contacts.api.js'
import { usersApi } from '../../api/users.api.js'
import ContactForm from './ContactForm.jsx'
import { Button } from '../../components/ui/button.jsx'
import { Badge } from '../../components/ui/badge.jsx'
import { Skeleton } from '../../components/ui/skeleton.jsx'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../../components/ui/table.jsx'
import { Card } from '../../components/ui/card.jsx'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '../../components/ui/select.jsx'
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

const STATUSES = ['Lead', 'Prospect', 'Customer', 'Churned']

const STATUS_VARIANT = {
  Lead: 'lead',
  Prospect: 'prospect',
  Customer: 'customer',
  Churned: 'churned',
}

export default function ContactList() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [statusFilter, setStatusFilter] = useState('')
  const [ownerFilter, setOwnerFilter] = useState('')
  const [sheetOpen, setSheetOpen] = useState(false)
  const [editContactId, setEditContactId] = useState(null)
  const [deleteTarget, setDeleteTarget] = useState(null)

  const { data: contacts = [], isLoading, error } = useQuery({
    queryKey: ['contacts', { status: statusFilter, ownerId: ownerFilter }],
    queryFn: () => contactsApi.list({ status: statusFilter || undefined, ownerId: ownerFilter || undefined }),
  })

  const { data: team = [] } = useQuery({
    queryKey: ['team'],
    queryFn: usersApi.getTeam,
  })

  const deleteMutation = useMutation({
    mutationFn: (id) => contactsApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contacts'] })
      setDeleteTarget(null)
    },
  })

  const { sortedData: sortedContacts, sortKey, sortDir, handleSort } = useSortableTable(contacts, 'lastName')
  const pagination = usePagination(sortedContacts)

  return (
    <div>
      <div className="flex items-center justify-between mb-5">
        <h1 className="text-2xl font-bold text-gray-900">Contacts</h1>
        <Button onClick={() => setSheetOpen(true)}>+ New Contact</Button>
      </div>

      {/* Filters */}
      <div className="flex gap-3 mb-4">
        <Select value={statusFilter} onValueChange={(v) => setStatusFilter(v === '_all' ? '' : v)}>
          <SelectTrigger className="w-44">
            <SelectValue placeholder="All statuses" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="_all">All statuses</SelectItem>
            {STATUSES.map((s) => <SelectItem key={s} value={s}>{s}</SelectItem>)}
          </SelectContent>
        </Select>

        <Select value={ownerFilter} onValueChange={(v) => setOwnerFilter(v === '_all' ? '' : v)}>
          <SelectTrigger className="w-44">
            <SelectValue placeholder="All owners" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="_all">All owners</SelectItem>
            {team.map((m) => (
              <SelectItem key={m.userId} value={m.userId}>{m.displayName || m.userId}</SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {isLoading ? (
        <Card className="p-0 overflow-hidden">
          <div className="p-4 flex flex-col gap-3">
            {[1, 2, 3, 4].map((i) => <Skeleton key={i} className="h-10 w-full" />)}
          </div>
        </Card>
      ) : error ? (
        <p className="text-sm text-red-600 bg-red-50 px-3 py-2 rounded-md">{error.message}</p>
      ) : contacts.length === 0 ? (
        <p className="text-sm text-gray-400 py-4">No contacts found.</p>
      ) : (
        <Card className="p-0 overflow-hidden">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="cursor-pointer select-none" onClick={() => handleSort('lastName')}>
                  Name <SortIcon active={sortKey === 'lastName'} dir={sortDir} />
                </TableHead>
                <TableHead className="cursor-pointer select-none" onClick={() => handleSort('email')}>
                  Email <SortIcon active={sortKey === 'email'} dir={sortDir} />
                </TableHead>
                <TableHead className="cursor-pointer select-none" onClick={() => handleSort('status')}>
                  Status <SortIcon active={sortKey === 'status'} dir={sortDir} />
                </TableHead>
                <TableHead>Owner</TableHead>
                <TableHead className="cursor-pointer select-none" onClick={() => handleSort('createdAt')}>
                  Created <SortIcon active={sortKey === 'createdAt'} dir={sortDir} />
                </TableHead>
                <TableHead className="w-20" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {pagination.paginatedData.map((c) => {
                const owner = team.find((m) => m.userId === c.ownerId)
                return (
                  <TableRow
                    key={c.contactId}
                    className="cursor-pointer group"
                    onClick={() => navigate(`/contacts/${c.contactId}`)}
                  >
                    <TableCell className="font-medium">
                      <Link
                        to={`/contacts/${c.contactId}`}
                        className="text-blue-600 hover:underline no-underline"
                        onClick={(e) => e.stopPropagation()}
                      >
                        {c.firstName} {c.lastName}
                      </Link>
                    </TableCell>
                    <TableCell>{c.email}</TableCell>
                    <TableCell>
                      <Badge variant={STATUS_VARIANT[c.status] ?? 'default'}>{c.status}</Badge>
                    </TableCell>
                    <TableCell>
                      {owner?.displayName ?? (c.ownerId ? c.ownerId.slice(0, 8) + '…' : '—')}
                    </TableCell>
                    <TableCell>{new Date(c.createdAt).toLocaleDateString()}</TableCell>
                    <TableCell>
                      <div className="flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity justify-end">
                        <Button
                          size="icon"
                          variant="ghost"
                          className="h-7 w-7"
                          onClick={(e) => { e.stopPropagation(); setEditContactId(c.contactId) }}
                          title="Edit"
                        >
                          <Pencil size={13} />
                        </Button>
                        <Button
                          size="icon"
                          variant="ghost"
                          className="h-7 w-7 text-red-500 hover:text-red-600 hover:bg-red-50"
                          onClick={(e) => { e.stopPropagation(); setDeleteTarget(c) }}
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
            onPageChange={pagination.handlePageChange}
            onPageSizeChange={pagination.handlePageSizeChange}
          />
        </Card>
      )}

      {/* New Contact Sheet */}
      <Sheet open={sheetOpen} onOpenChange={setSheetOpen}>
        <SheetContent className="sm:max-w-lg overflow-y-auto">
          <SheetHeader>
            <SheetTitle>New Contact</SheetTitle>
          </SheetHeader>
          <div className="mt-5">
            <ContactForm
              onSuccess={(result) => {
                setSheetOpen(false)
                navigate(`/contacts/${result.contactId}`)
              }}
              onClose={() => setSheetOpen(false)}
            />
          </div>
        </SheetContent>
      </Sheet>

      {/* Edit Contact Sheet */}
      <Sheet open={!!editContactId} onOpenChange={(open) => { if (!open) setEditContactId(null) }}>
        <SheetContent className="sm:max-w-lg overflow-y-auto">
          <SheetHeader>
            <SheetTitle>Edit Contact</SheetTitle>
          </SheetHeader>
          <div className="mt-5">
            {editContactId && (
              <ContactForm
                id={editContactId}
                onSuccess={() => {
                  setEditContactId(null)
                  queryClient.invalidateQueries({ queryKey: ['contacts'] })
                }}
                onClose={() => setEditContactId(null)}
              />
            )}
          </div>
        </SheetContent>
      </Sheet>

      {/* Delete Confirmation Dialog */}
      <Dialog open={!!deleteTarget} onOpenChange={(open) => { if (!open) setDeleteTarget(null) }}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Delete Contact</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete{' '}
              <strong>{deleteTarget?.firstName} {deleteTarget?.lastName}</strong>?
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
              onClick={() => deleteMutation.mutate(deleteTarget.contactId)}
            >
              {deleteMutation.isPending ? 'Deleting…' : 'Delete'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
