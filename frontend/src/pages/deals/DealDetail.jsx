import { useParams, useNavigate, Link } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { dealsApi } from '../../api/deals.api.js'
import { contactsApi } from '../../api/contacts.api.js'
import { accountsApi } from '../../api/accounts.api.js'
import ActivityTimeline from '../../components/ActivityTimeline.jsx'
import ActivityLogForm from '../../components/ActivityLogForm.jsx'
import Breadcrumb from '../../components/Breadcrumb.jsx'
import DealForm from './DealForm.jsx'
import { Button } from '../../components/ui/button.jsx'
import { Badge } from '../../components/ui/badge.jsx'
import { Card, CardContent, CardHeader, CardTitle } from '../../components/ui/card.jsx'
import { Skeleton } from '../../components/ui/skeleton.jsx'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../../components/ui/table.jsx'
import { Combobox } from '../../components/ui/combobox.jsx'
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
import { toast } from '../../hooks/use-toast.js'

const STAGES = ['Prospecting', 'Proposal', 'Negotiation', 'ClosedWon', 'ClosedLost']
const ROLES = ['DecisionMaker', 'Influencer', 'Champion']

export default function DealDetail() {
  const { id } = useParams()
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [contactId, setContactId] = useState('')
  const [contactRole, setContactRole] = useState('Influencer')
  const [editOpen, setEditOpen] = useState(false)

  const { data: deal, isLoading, error } = useQuery({
    queryKey: ['deal', id],
    queryFn: () => dealsApi.get(id),
  })

  const { data: allContacts = [] } = useQuery({
    queryKey: ['contacts'],
    queryFn: () => contactsApi.list(),
  })

  const { data: account } = useQuery({
    queryKey: ['account', deal?.accountId],
    queryFn: () => accountsApi.get(deal.accountId),
    enabled: !!deal?.accountId,
  })

  const stageUpdate = useMutation({
    mutationFn: (stage) => dealsApi.update(id, { stage }),
    onSuccess: (_, stage) => {
      queryClient.invalidateQueries({ queryKey: ['deal', id] })
      toast({ variant: 'success', title: 'Stage updated', description: `Deal moved to ${stage}` })
    },
    onError: (err) => toast({ variant: 'destructive', title: 'Update failed', description: err.message }),
  })

  const deleteDeal = useMutation({
    mutationFn: () => dealsApi.delete(id),
    onSuccess: () => {
      toast({ variant: 'success', title: 'Deal deleted' })
      navigate('/deals')
    },
    onError: (err) => toast({ variant: 'destructive', title: 'Delete failed', description: err.message }),
  })

  const addContact = useMutation({
    mutationFn: () => dealsApi.addContact(id, { contactId, role: contactRole }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['deal', id] })
      toast({ variant: 'success', title: 'Contact added' })
      setContactId('')
    },
    onError: (err) => toast({ variant: 'destructive', title: 'Failed to add contact', description: err.message }),
  })

  const removeContact = useMutation({
    mutationFn: (cid) => dealsApi.removeContact(id, cid),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['deal', id] })
      toast({ variant: 'success', title: 'Contact removed' })
    },
    onError: (err) => toast({ variant: 'destructive', title: 'Failed to remove contact', description: err.message }),
  })

  if (isLoading) {
    return (
      <div className="flex flex-col gap-4 max-w-2xl">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-32 w-full rounded-lg" />
        <Skeleton className="h-48 w-full rounded-lg" />
      </div>
    )
  }
  if (error) return <p className="text-sm text-red-600 bg-red-50 px-3 py-2 rounded-md">{error.message}</p>
  if (!deal) return <p className="text-sm text-red-600 bg-red-50 px-3 py-2 rounded-md">Deal not found.</p>

  const isClosed = deal.stage === 'ClosedWon' || deal.stage === 'ClosedLost'
  const associatedContactIds = new Set(deal.contacts?.map((c) => c.contactId))
  const availableContacts = allContacts.filter((c) => !associatedContactIds.has(c.contactId))

  const contactOptions = availableContacts.map((c) => ({
    value: c.contactId,
    label: `${c.firstName} ${c.lastName}`,
  }))

  return (
    <div className="max-w-2xl">
      <Breadcrumb items={[
        { label: 'Pipeline', to: '/deals' },
        { label: deal.title },
      ]} />

      <div className="flex items-start justify-between mb-5">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">{deal.title}</h1>
          {account && <p className="text-sm text-gray-500 mt-0.5">{account.name}</p>}
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => setEditOpen(true)}>Edit</Button>
          <Dialog>
            <DialogTrigger asChild>
              <Button variant="destructive">Delete</Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>Delete deal?</DialogTitle>
                <DialogDescription>
                  This will permanently delete <strong>{deal.title}</strong>. This action cannot be undone.
                </DialogDescription>
              </DialogHeader>
              <DialogFooter>
                <Button variant="outline" onClick={(e) => e.currentTarget.closest('[role=dialog]')?.querySelector('[aria-label=Close]')?.click()}>
                  Cancel
                </Button>
                <Button variant="destructive" onClick={() => deleteDeal.mutate()} disabled={deleteDeal.isPending}>
                  {deleteDeal.isPending ? 'Deleting…' : 'Delete'}
                </Button>
              </DialogFooter>
            </DialogContent>
          </Dialog>
        </div>
      </div>

      {/* Stats grid */}
      <div className="grid grid-cols-2 gap-4 mb-4">
        {[
          { label: 'Value', value: `$${deal.value.toLocaleString()}` },
          { label: 'Probability', value: deal.probability != null ? `${deal.probability}%` : '—' },
          { label: 'Expected Close', value: deal.expectedCloseDate ? new Date(deal.expectedCloseDate).toLocaleDateString() : '—' },
          { label: 'Stage', value: <Badge variant="secondary">{deal.stage}</Badge> },
        ].map(({ label, value }) => (
          <Card key={label}>
            <CardContent className="pt-4 pb-4">
              <p className="text-xs text-gray-500 font-medium mb-1">{label}</p>
              <div className="text-sm font-semibold text-gray-900">{value}</div>
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Stage transitions */}
      {!isClosed && (
        <Card className="mb-4">
          <CardHeader><CardTitle className="text-sm font-semibold text-gray-600">Move to Stage</CardTitle></CardHeader>
          <CardContent className="pt-0 flex flex-wrap gap-2">
            {STAGES.filter((s) => s !== deal.stage).map((s) => (
              <Button
                key={s}
                variant="outline"
                size="sm"
                disabled={stageUpdate.isPending}
                onClick={() => stageUpdate.mutate(s)}
              >
                {s}
              </Button>
            ))}
          </CardContent>
        </Card>
      )}

      {/* Associated Contacts */}
      <Card className="mb-4">
        <CardHeader><CardTitle>Associated Contacts</CardTitle></CardHeader>
        <CardContent className="pt-0">
          {deal.contacts?.length > 0 ? (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Contact</TableHead>
                  <TableHead>Role</TableHead>
                  <TableHead></TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {deal.contacts.map((dc) => {
                  const contact = allContacts.find((c) => c.contactId === dc.contactId)
                  return (
                    <TableRow key={dc.dealContactId}>
                      <TableCell>
                        {contact
                          ? <Link to={`/contacts/${dc.contactId}`} className="text-blue-600 hover:underline no-underline">{contact.firstName} {contact.lastName}</Link>
                          : dc.contactId.slice(0, 8) + '…'}
                      </TableCell>
                      <TableCell>{dc.role}</TableCell>
                      <TableCell>
                        <Button size="sm" variant="destructive" onClick={() => removeContact.mutate(dc.contactId)}>
                          Remove
                        </Button>
                      </TableCell>
                    </TableRow>
                  )
                })}
              </TableBody>
            </Table>
          ) : (
            <p className="text-sm text-gray-400">No contacts associated.</p>
          )}

          {availableContacts.length > 0 && (
            <div className="flex gap-2 items-center flex-wrap mt-4 pt-4 border-t">
              <div className="flex-1 min-w-0">
                <Combobox
                  options={contactOptions}
                  value={contactId}
                  onChange={setContactId}
                  placeholder="Select contact…"
                  searchPlaceholder="Search contacts…"
                />
              </div>
              <Select value={contactRole} onValueChange={setContactRole}>
                <SelectTrigger className="w-40"><SelectValue /></SelectTrigger>
                <SelectContent>
                  {ROLES.map((r) => <SelectItem key={r} value={r}>{r}</SelectItem>)}
                </SelectContent>
              </Select>
              <Button
                disabled={!contactId || addContact.isPending}
                onClick={() => addContact.mutate()}
              >
                Add
              </Button>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Activity */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle>Activity</CardTitle>
            <ActivityLogForm dealId={id} queryKey="deal-activities" />
          </div>
        </CardHeader>
        <CardContent className="pt-0">
          <ActivityTimeline dealId={id} queryKey="deal-activities" />
        </CardContent>
      </Card>

      <Sheet open={editOpen} onOpenChange={setEditOpen}>
        <SheetContent className="sm:max-w-lg overflow-y-auto">
          <SheetHeader>
            <SheetTitle>Edit Deal</SheetTitle>
          </SheetHeader>
          <div className="mt-5">
            <DealForm
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
