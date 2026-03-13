import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useParams, useNavigate, Link } from 'react-router-dom'
import { contactsApi } from '../../api/contacts.api.js'
import { accountsApi } from '../../api/accounts.api.js'
import { usersApi } from '../../api/users.api.js'
import ActivityTimeline from '../../components/ActivityTimeline.jsx'
import ActivityLogForm from '../../components/ActivityLogForm.jsx'
import Breadcrumb from '../../components/Breadcrumb.jsx'
import ContactForm from './ContactForm.jsx'
import { Button } from '../../components/ui/button.jsx'
import { Badge } from '../../components/ui/badge.jsx'
import { Card, CardContent, CardHeader, CardTitle } from '../../components/ui/card.jsx'
import { Skeleton } from '../../components/ui/skeleton.jsx'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '../../components/ui/dialog.jsx'
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from '../../components/ui/sheet.jsx'
import { toast } from '../../hooks/use-toast.js'

const STATUS_TRANSITIONS = {
  Lead: ['Prospect', 'Churned'],
  Prospect: ['Customer', 'Churned'],
  Customer: ['Churned'],
  Churned: ['Lead'],
}

const STATUS_VARIANT = {
  Lead: 'lead',
  Prospect: 'prospect',
  Customer: 'customer',
  Churned: 'churned',
}

export default function ContactDetail() {
  const { id } = useParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [editOpen, setEditOpen] = useState(false)

  const { data: contact, isLoading, error } = useQuery({
    queryKey: ['contact', id],
    queryFn: () => contactsApi.get(id),
  })

  const { data: account } = useQuery({
    queryKey: ['account', contact?.accountId],
    queryFn: () => accountsApi.get(contact.accountId),
    enabled: !!contact?.accountId,
  })

  const { data: team = [] } = useQuery({
    queryKey: ['team'],
    queryFn: usersApi.getTeam,
  })

  const updateMutation = useMutation({
    mutationFn: (data) => contactsApi.update(id, data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: ['contact', id] })
      if (variables.status) {
        toast({ title: 'Status updated', description: `Contact moved to ${variables.status}`, variant: 'success' })
      }
    },
  })

  const deleteMutation = useMutation({
    mutationFn: () => contactsApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contacts'] })
      toast({ variant: 'success', title: 'Contact deleted' })
      navigate('/contacts')
    },
    onError: (err) => toast({ variant: 'destructive', title: 'Delete failed', description: err.message }),
  })

  if (isLoading) {
    return (
      <div className="flex flex-col gap-4">
        <Skeleton className="h-4 w-48" />
        <div className="flex items-center justify-between">
          <Skeleton className="h-8 w-56" />
          <div className="flex gap-2">
            <Skeleton className="h-9 w-16 rounded-md" />
            <Skeleton className="h-9 w-16 rounded-md" />
          </div>
        </div>
        <Card>
          <CardContent className="pt-6">
            <div className="flex flex-col gap-4">
              {['Status', 'Email', 'Phone', 'Account', 'Owner', 'Created', 'Updated'].map((field) => (
                <div key={field} className="flex gap-4">
                  <Skeleton className="h-4 w-28 flex-shrink-0" />
                  <Skeleton className="h-4 flex-1 max-w-xs" />
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
        <Card>
          <CardContent className="pt-6">
            <div className="flex flex-col gap-3">
              {[1, 2, 3].map((i) => <Skeleton key={i} className="h-16 w-full rounded-lg" />)}
            </div>
          </CardContent>
        </Card>
      </div>
    )
  }
  if (error) return <p className="text-sm text-red-600 bg-red-50 px-3 py-2 rounded-md">{error.message}</p>
  if (!contact) return null

  const owner = team.find((m) => m.userId === contact.ownerId)
  const nextStatuses = STATUS_TRANSITIONS[contact.status] ?? []

  return (
    <div>
      <Breadcrumb items={[
        { label: 'Contacts', to: '/contacts' },
        { label: `${contact.firstName} ${contact.lastName}` },
      ]} />

      <div className="flex items-center justify-between mb-5">
        <h1 className="text-2xl font-bold text-gray-900">{contact.firstName} {contact.lastName}</h1>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => setEditOpen(true)}>Edit</Button>
          <Dialog>
            <DialogTrigger asChild>
              <Button variant="destructive">Delete</Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>Delete contact?</DialogTitle>
                <DialogDescription>
                  This will permanently delete {contact.firstName} {contact.lastName} and all associated data. This action cannot be undone.
                </DialogDescription>
              </DialogHeader>
              <DialogFooter>
                <Button variant="outline" onClick={(e) => e.currentTarget.closest('[role=dialog]')?.querySelector('[aria-label=Close]')?.click()}>
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
      </div>

      {nextStatuses.length > 0 && (
        <div className="flex items-center gap-2 mb-5 text-sm text-gray-500">
          <span>Move to:</span>
          {nextStatuses.map((s) => (
            <button
              key={s}
              className={`px-3 py-0.5 text-xs font-semibold rounded-full border-[1.5px] cursor-pointer hover:opacity-75 transition-opacity disabled:opacity-50 ${
                s === 'Lead' ? 'border-blue-600 text-blue-600' :
                s === 'Prospect' ? 'border-amber-600 text-amber-800' :
                s === 'Customer' ? 'border-emerald-600 text-emerald-700' :
                'border-gray-400 text-gray-500'
              }`}
              onClick={() => updateMutation.mutate({ status: s })}
              disabled={updateMutation.isPending}
            >
              {s}
            </button>
          ))}
        </div>
      )}

      <Card className="mb-4">
        <CardContent className="pt-6">
          <table className="w-full text-sm">
            <tbody>
              <tr>
                <th className="text-left w-36 py-2 pr-3 text-gray-500 font-medium align-top">Status</th>
                <td className="py-2 text-gray-900">
                  <Badge variant={STATUS_VARIANT[contact.status] ?? 'default'}>{contact.status}</Badge>
                </td>
              </tr>
              <tr>
                <th className="text-left py-2 pr-3 text-gray-500 font-medium align-top">Email</th>
                <td className="py-2 text-gray-900">{contact.email}</td>
              </tr>
              <tr>
                <th className="text-left py-2 pr-3 text-gray-500 font-medium align-top">Phone</th>
                <td className="py-2 text-gray-900">{contact.phone ?? '—'}</td>
              </tr>
              <tr>
                <th className="text-left py-2 pr-3 text-gray-500 font-medium align-top">Account</th>
                <td className="py-2 text-gray-900">
                  {account
                    ? <Link to={`/accounts/${account.accountId}`} className="text-blue-600 hover:underline">{account.name}</Link>
                    : (contact.accountId ? contact.accountId : '—')}
                </td>
              </tr>
              <tr>
                <th className="text-left py-2 pr-3 text-gray-500 font-medium align-top">Owner</th>
                <td className="py-2 text-gray-900">{owner?.displayName ?? (contact.ownerId ? contact.ownerId : '—')}</td>
              </tr>
              <tr>
                <th className="text-left py-2 pr-3 text-gray-500 font-medium align-top">Created</th>
                <td className="py-2 text-gray-900">{new Date(contact.createdAt).toLocaleString()}</td>
              </tr>
              <tr>
                <th className="text-left py-2 pr-3 text-gray-500 font-medium align-top">Updated</th>
                <td className="py-2 text-gray-900">{new Date(contact.updatedAt).toLocaleString()}</td>
              </tr>
            </tbody>
          </table>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle>Activity</CardTitle>
            <ActivityLogForm contactId={id} queryKey="contact-activities" />
          </div>
        </CardHeader>
        <CardContent className="pt-0">
          <ActivityTimeline contactId={id} queryKey="contact-activities" />
        </CardContent>
      </Card>

      <Sheet open={editOpen} onOpenChange={setEditOpen}>
        <SheetContent className="sm:max-w-lg overflow-y-auto">
          <SheetHeader>
            <SheetTitle>Edit Contact</SheetTitle>
          </SheetHeader>
          <div className="mt-5">
            <ContactForm
              id={id}
              onSuccess={() => setEditOpen(false)}
              onClose={() => setEditOpen(false)}
            />
          </div>
        </SheetContent>
      </Sheet>
    </div>
  )
}
