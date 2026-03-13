import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { accountsApi } from '../../api/accounts.api.js'
import { contactsApi } from '../../api/contacts.api.js'
import ActivityLogForm from '../../components/ActivityLogForm.jsx'
import ActivityTimeline from '../../components/ActivityTimeline.jsx'
import Breadcrumb from '../../components/Breadcrumb.jsx'
import AccountForm from './AccountForm.jsx'
import ContactForm from '../contacts/ContactForm.jsx'
import { Button } from '../../components/ui/button.jsx'
import { Badge } from '../../components/ui/badge.jsx'
import { Card, CardContent, CardHeader, CardTitle } from '../../components/ui/card.jsx'
import { Skeleton } from '../../components/ui/skeleton.jsx'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../../components/ui/table.jsx'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '../../components/ui/dialog.jsx'
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from '../../components/ui/sheet.jsx'
import { toast } from '../../hooks/use-toast.js'

const STATUS_VARIANT = {
  Lead: 'lead',
  Prospect: 'prospect',
  Customer: 'customer',
  Churned: 'churned',
}

export default function AccountDetail() {
  const { id } = useParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [editOpen, setEditOpen] = useState(false)
  const [addContactOpen, setAddContactOpen] = useState(false)
  const [deleteOpen, setDeleteOpen] = useState(false)

  const { data: account, isLoading, error } = useQuery({
    queryKey: ['account', id],
    queryFn: () => accountsApi.get(id),
  })

  const { data: contacts = [] } = useQuery({
    queryKey: ['contacts', { accountId: id }],
    queryFn: () => contactsApi.list({ accountId: id }),
    enabled: !!id,
  })

  const deleteMutation = useMutation({
    mutationFn: () => accountsApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['accounts'] })
      toast({ variant: 'success', title: 'Account deleted' })
      navigate('/accounts')
    },
    onError: (err) => toast({ variant: 'destructive', title: 'Delete failed', description: err.message }),
  })

  if (isLoading) {
    return (
      <div className="flex flex-col gap-4">
        <Skeleton className="h-4 w-44" />
        <div className="flex items-center justify-between">
          <Skeleton className="h-8 w-48" />
          <div className="flex gap-2">
            <Skeleton className="h-9 w-16 rounded-md" />
            <Skeleton className="h-9 w-16 rounded-md" />
          </div>
        </div>
        <Card>
          <CardHeader><Skeleton className="h-5 w-16" /></CardHeader>
          <CardContent className="pt-0">
            <div className="flex flex-col gap-4">
              {['Industry', 'Size', 'Website', 'Address', 'Created', 'Updated'].map((field) => (
                <div key={field} className="flex gap-4">
                  <Skeleton className="h-4 w-28 flex-shrink-0" />
                  <Skeleton className="h-4 flex-1 max-w-xs" />
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardHeader><Skeleton className="h-5 w-28" /></CardHeader>
          <CardContent className="pt-0">
            <div className="flex flex-col gap-3">
              {[1, 2].map((i) => <Skeleton key={i} className="h-10 w-full rounded-md" />)}
            </div>
          </CardContent>
        </Card>
      </div>
    )
  }
  if (error) return <p className="text-sm text-red-600 bg-red-50 px-3 py-2 rounded-md">{error.message}</p>
  if (!account) return null

  const addressParts = [account.street, account.city, account.state, account.postalCode, account.country].filter(Boolean)

  return (
    <div>
      <Breadcrumb items={[
        { label: 'Accounts', to: '/accounts' },
        { label: account.name },
      ]} />

      <div className="flex items-center justify-between mb-5">
        <h1 className="text-2xl font-bold text-gray-900">{account.name}</h1>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => setEditOpen(true)}>Edit</Button>
          <Button variant="destructive" onClick={() => setDeleteOpen(true)}>Delete</Button>
        </div>
      </div>

      <Card className="mb-4">
        <CardHeader><CardTitle>Details</CardTitle></CardHeader>
        <CardContent className="pt-0">
          <table className="w-full text-sm">
            <tbody>
              <tr><th className="text-left w-36 py-2 pr-3 text-gray-500 font-medium">Industry</th><td className="py-2">{account.industry ?? '—'}</td></tr>
              <tr><th className="text-left py-2 pr-3 text-gray-500 font-medium">Size</th><td className="py-2">{account.size ?? '—'}</td></tr>
              <tr><th className="text-left py-2 pr-3 text-gray-500 font-medium">Website</th>
                <td className="py-2">
                  {account.website ? <a href={account.website} target="_blank" rel="noreferrer" className="text-blue-600 hover:underline">{account.website}</a> : '—'}
                </td>
              </tr>
              <tr><th className="text-left py-2 pr-3 text-gray-500 font-medium">Address</th><td className="py-2">{addressParts.length > 0 ? addressParts.join(', ') : '—'}</td></tr>
              <tr><th className="text-left py-2 pr-3 text-gray-500 font-medium">Created</th><td className="py-2">{new Date(account.createdAt).toLocaleString()}</td></tr>
              <tr><th className="text-left py-2 pr-3 text-gray-500 font-medium">Updated</th><td className="py-2">{new Date(account.updatedAt).toLocaleString()}</td></tr>
            </tbody>
          </table>
        </CardContent>
      </Card>

      <Card className="mb-4">
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle>Contacts ({contacts.length})</CardTitle>
            <Button size="sm" variant="outline" onClick={() => setAddContactOpen(true)}>+ Add Contact</Button>
          </div>
        </CardHeader>
        <CardContent className="pt-0">
          {contacts.length === 0 ? (
            <p className="text-sm text-gray-400">No contacts linked to this account.</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Email</TableHead>
                  <TableHead>Status</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {contacts.map((c) => (
                  <TableRow key={c.contactId} className="cursor-pointer" onClick={() => navigate(`/contacts/${c.contactId}`)}>
                    <TableCell>
                      <Link to={`/contacts/${c.contactId}`} className="text-blue-600 hover:underline no-underline" onClick={(e) => e.stopPropagation()}>
                        {c.firstName} {c.lastName}
                      </Link>
                    </TableCell>
                    <TableCell>{c.email}</TableCell>
                    <TableCell><Badge variant={STATUS_VARIANT[c.status] ?? 'default'}>{c.status}</Badge></TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle>Activity</CardTitle>
            <ActivityLogForm accountId={id} queryKey="account-activities" />
          </div>
        </CardHeader>
        <CardContent className="pt-0">
          <ActivityTimeline accountId={id} queryKey="account-activities" />
        </CardContent>
      </Card>

      <Sheet open={editOpen} onOpenChange={setEditOpen}>
        <SheetContent className="sm:max-w-lg overflow-y-auto">
          <SheetHeader>
            <SheetTitle>Edit Account</SheetTitle>
          </SheetHeader>
          <div className="mt-5">
            <AccountForm
              id={id}
              onSuccess={() => setEditOpen(false)}
              onClose={() => setEditOpen(false)}
            />
          </div>
        </SheetContent>
      </Sheet>

      <Sheet open={addContactOpen} onOpenChange={setAddContactOpen}>
        <SheetContent className="sm:max-w-lg overflow-y-auto">
          <SheetHeader>
            <SheetTitle>New Contact</SheetTitle>
          </SheetHeader>
          <div className="mt-5">
            <ContactForm
              onSuccess={(result) => {
                setAddContactOpen(false)
                navigate(`/contacts/${result.contactId}`)
              }}
              onClose={() => setAddContactOpen(false)}
            />
          </div>
        </SheetContent>
      </Sheet>

      {/* Delete Account Confirmation Dialog */}
      <Dialog open={deleteOpen} onOpenChange={setDeleteOpen}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Delete account?</DialogTitle>
            <DialogDescription>
              This will permanently delete <strong>{account.name}</strong> and all associated data. This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDeleteOpen(false)} disabled={deleteMutation.isPending}>
              Cancel
            </Button>
            <Button
              variant="destructive"
              disabled={deleteMutation.isPending}
              onClick={() => deleteMutation.mutate()}
            >
              {deleteMutation.isPending ? 'Deleting…' : 'Delete'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
