import { useState, useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { contactsApi } from '../../api/contacts.api.js'
import { accountsApi } from '../../api/accounts.api.js'
import { usersApi } from '../../api/users.api.js'
import Breadcrumb from '../../components/Breadcrumb.jsx'
import { Button } from '../../components/ui/button.jsx'
import { Input } from '../../components/ui/input.jsx'
import { Label } from '../../components/ui/label.jsx'
import { Card, CardContent } from '../../components/ui/card.jsx'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '../../components/ui/select.jsx'
import { toast } from '../../hooks/use-toast.js'

const STATUSES = ['Lead', 'Prospect', 'Customer', 'Churned']
const EMPTY = { firstName: '', lastName: '', email: '', phone: '', status: 'Lead', accountId: '', ownerId: '' }

export default function ContactForm({ onSuccess, onClose, id: idProp }) {
  const { id: idParam } = useParams()
  const id = onClose !== undefined ? idProp : idParam
  const isEdit = !!id
  const isSheet = onClose !== undefined
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [fields, setFields] = useState(EMPTY)

  const { data: existing } = useQuery({
    queryKey: ['contact', id],
    queryFn: () => contactsApi.get(id),
    enabled: isEdit,
  })

  const { data: accounts = [] } = useQuery({
    queryKey: ['accounts'],
    queryFn: accountsApi.list,
  })

  const { data: team = [] } = useQuery({
    queryKey: ['team'],
    queryFn: usersApi.getTeam,
  })

  useEffect(() => {
    if (existing) {
      setFields({
        firstName: existing.firstName ?? '',
        lastName: existing.lastName ?? '',
        email: existing.email ?? '',
        phone: existing.phone ?? '',
        status: existing.status ?? 'Lead',
        accountId: existing.accountId ?? '',
        ownerId: existing.ownerId ?? '',
      })
    }
  }, [existing])

  const mutation = useMutation({
    mutationFn: (data) =>
      isEdit ? contactsApi.update(id, data) : contactsApi.create(data),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: ['contacts'] })
      if (isEdit) queryClient.invalidateQueries({ queryKey: ['contact', id] })
      toast({ variant: 'success', title: isEdit ? 'Contact updated' : 'Contact created' })
      if (onSuccess) {
        onSuccess(result)
      } else {
        navigate(isEdit ? `/contacts/${id}` : `/contacts/${result.contactId}`)
      }
    },
    onError: (err) => toast({ variant: 'destructive', title: 'Save failed', description: err.message }),
  })

  function set(field, value) {
    setFields((prev) => ({ ...prev, [field]: value }))
  }

  function handleSubmit(e) {
    e.preventDefault()
    mutation.mutate({
      ...fields,
      accountId: fields.accountId || null,
      ownerId: fields.ownerId || null,
    })
  }

  const breadcrumbItems = isEdit && existing
    ? [
        { label: 'Contacts', to: '/contacts' },
        { label: `${existing.firstName} ${existing.lastName}`, to: `/contacts/${id}` },
        { label: 'Edit' },
      ]
    : [
        { label: 'Contacts', to: '/contacts' },
        { label: 'New Contact' },
      ]

  const formFields = (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      <div className="grid grid-cols-2 gap-4">
        <div className="flex flex-col gap-1.5">
          <Label>First Name *</Label>
          <Input value={fields.firstName} onChange={(e) => set('firstName', e.target.value)} required />
        </div>
        <div className="flex flex-col gap-1.5">
          <Label>Last Name *</Label>
          <Input value={fields.lastName} onChange={(e) => set('lastName', e.target.value)} required />
        </div>
      </div>
      <div className="grid grid-cols-2 gap-4">
        <div className="flex flex-col gap-1.5">
          <Label>Email *</Label>
          <Input type="email" value={fields.email} onChange={(e) => set('email', e.target.value)} required />
        </div>
        <div className="flex flex-col gap-1.5">
          <Label>Phone</Label>
          <Input type="tel" value={fields.phone} onChange={(e) => set('phone', e.target.value)} />
        </div>
      </div>
      <div className="grid grid-cols-2 gap-4">
        <div className="flex flex-col gap-1.5">
          <Label>Status</Label>
          <Select value={fields.status} onValueChange={(v) => set('status', v)}>
            <SelectTrigger><SelectValue /></SelectTrigger>
            <SelectContent>
              {STATUSES.map((s) => <SelectItem key={s} value={s}>{s}</SelectItem>)}
            </SelectContent>
          </Select>
        </div>
        <div className="flex flex-col gap-1.5">
          <Label>Account</Label>
          <Select value={fields.accountId || '_none'} onValueChange={(v) => set('accountId', v === '_none' ? '' : v)}>
            <SelectTrigger><SelectValue placeholder="— None —" /></SelectTrigger>
            <SelectContent>
              <SelectItem value="_none">— None —</SelectItem>
              {accounts.map((a) => <SelectItem key={a.accountId} value={a.accountId}>{a.name}</SelectItem>)}
            </SelectContent>
          </Select>
        </div>
      </div>
      <div className="flex flex-col gap-1.5">
        <Label>Owner</Label>
        <Select value={fields.ownerId || '_unassigned'} onValueChange={(v) => set('ownerId', v === '_unassigned' ? '' : v)}>
          <SelectTrigger><SelectValue placeholder="— Unassigned —" /></SelectTrigger>
          <SelectContent>
            <SelectItem value="_unassigned">— Unassigned —</SelectItem>
            {team.map((m) => <SelectItem key={m.userId} value={m.userId}>{m.displayName || m.userId}</SelectItem>)}
          </SelectContent>
        </Select>
      </div>
      <div className="flex gap-3 pt-2">
        <Button type="submit" disabled={mutation.isPending}>
          {mutation.isPending ? 'Saving…' : isEdit ? 'Save changes' : 'Create contact'}
        </Button>
        <Button type="button" variant="outline" onClick={() => onClose ? onClose() : navigate(-1)}>Cancel</Button>
      </div>
    </form>
  )

  if (isSheet) {
    return formFields
  }

  return (
    <div>
      <Breadcrumb items={breadcrumbItems} />
      <h1 className="text-2xl font-bold text-gray-900 mb-5">
        {isEdit ? 'Edit Contact' : 'New Contact'}
      </h1>
      <Card>
        <CardContent className="pt-6">
          {formFields}
        </CardContent>
      </Card>
    </div>
  )
}
