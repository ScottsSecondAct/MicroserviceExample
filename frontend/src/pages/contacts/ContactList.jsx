import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link, useNavigate } from 'react-router-dom'
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
  const [statusFilter, setStatusFilter] = useState('')
  const [ownerFilter, setOwnerFilter] = useState('')
  const [sheetOpen, setSheetOpen] = useState(false)

  const { data: contacts = [], isLoading, error } = useQuery({
    queryKey: ['contacts', { status: statusFilter, ownerId: ownerFilter }],
    queryFn: () => contactsApi.list({ status: statusFilter || undefined, ownerId: ownerFilter || undefined }),
  })

  const { data: team = [] } = useQuery({
    queryKey: ['team'],
    queryFn: usersApi.getTeam,
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
              </TableRow>
            </TableHeader>
            <TableBody>
              {pagination.paginatedData.map((c) => {
                const owner = team.find((m) => m.userId === c.ownerId)
                return (
                  <TableRow
                    key={c.contactId}
                    className="cursor-pointer"
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
    </div>
  )
}
