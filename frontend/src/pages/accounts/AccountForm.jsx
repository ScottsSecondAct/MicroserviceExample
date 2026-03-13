import { useState, useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { accountsApi } from '../../api/accounts.api.js'
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

const INDUSTRIES = ['Technology', 'Finance', 'Healthcare', 'Retail', 'Manufacturing', 'Education', 'Other']
const SIZES = ['Small', 'Medium', 'Large', 'Enterprise']

const EMPTY = {
  name: '', industry: '', size: '', website: '',
  street: '', city: '', state: '', postalCode: '', country: '',
}

export default function AccountForm() {
  const { id } = useParams()
  const isEdit = !!id
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [fields, setFields] = useState(EMPTY)
  const [error, setError] = useState(null)

  const { data: existing } = useQuery({
    queryKey: ['account', id],
    queryFn: () => accountsApi.get(id),
    enabled: isEdit,
  })

  useEffect(() => {
    if (existing) {
      setFields({
        name: existing.name ?? '',
        industry: existing.industry ?? '',
        size: existing.size ?? '',
        website: existing.website ?? '',
        street: existing.street ?? '',
        city: existing.city ?? '',
        state: existing.state ?? '',
        postalCode: existing.postalCode ?? '',
        country: existing.country ?? '',
      })
    }
  }, [existing])

  const mutation = useMutation({
    mutationFn: (data) =>
      isEdit ? accountsApi.update(id, data) : accountsApi.create(data),
    onSuccess: (result) => {
      queryClient.invalidateQueries({ queryKey: ['accounts'] })
      if (isEdit) queryClient.invalidateQueries({ queryKey: ['account', id] })
      navigate(isEdit ? `/accounts/${id}` : `/accounts/${result.accountId}`)
    },
    onError: (err) => setError(err.message),
  })

  function set(field, value) {
    setFields((prev) => ({ ...prev, [field]: value }))
  }

  function handleSubmit(e) {
    e.preventDefault()
    setError(null)
    mutation.mutate({
      ...fields,
      industry: fields.industry || null,
      size: fields.size || null,
      website: fields.website || null,
      street: fields.street || null,
      city: fields.city || null,
      state: fields.state || null,
      postalCode: fields.postalCode || null,
      country: fields.country || null,
    })
  }

  const breadcrumbItems = isEdit && existing
    ? [
        { label: 'Accounts', to: '/accounts' },
        { label: existing.name, to: `/accounts/${id}` },
        { label: 'Edit' },
      ]
    : [
        { label: 'Accounts', to: '/accounts' },
        { label: 'New Account' },
      ]

  return (
    <div>
      <Breadcrumb items={breadcrumbItems} />
      <h1 className="text-2xl font-bold text-gray-900 mb-5">
        {isEdit ? 'Edit Account' : 'New Account'}
      </h1>
      <Card>
        <CardContent className="pt-6">
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            {error && (
              <p className="text-sm text-red-600 bg-red-50 px-3 py-2 rounded-md border border-red-200">{error}</p>
            )}
            <div className="flex flex-col gap-1.5">
              <Label>Name *</Label>
              <Input value={fields.name} onChange={(e) => set('name', e.target.value)} required />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="flex flex-col gap-1.5">
                <Label>Industry</Label>
                <Select value={fields.industry || '_none'} onValueChange={(v) => set('industry', v === '_none' ? '' : v)}>
                  <SelectTrigger><SelectValue placeholder="— Select —" /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="_none">— Select —</SelectItem>
                    {INDUSTRIES.map((i) => <SelectItem key={i} value={i}>{i}</SelectItem>)}
                  </SelectContent>
                </Select>
              </div>
              <div className="flex flex-col gap-1.5">
                <Label>Size</Label>
                <Select value={fields.size || '_none'} onValueChange={(v) => set('size', v === '_none' ? '' : v)}>
                  <SelectTrigger><SelectValue placeholder="— Select —" /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="_none">— Select —</SelectItem>
                    {SIZES.map((s) => <SelectItem key={s} value={s}>{s}</SelectItem>)}
                  </SelectContent>
                </Select>
              </div>
            </div>
            <div className="flex flex-col gap-1.5">
              <Label>Website</Label>
              <Input type="url" value={fields.website} onChange={(e) => set('website', e.target.value)} placeholder="https://" />
            </div>
            <p className="text-xs font-semibold text-gray-500 uppercase tracking-wider mt-2">Address</p>
            <div className="flex flex-col gap-1.5">
              <Label>Street</Label>
              <Input value={fields.street} onChange={(e) => set('street', e.target.value)} />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="flex flex-col gap-1.5">
                <Label>City</Label>
                <Input value={fields.city} onChange={(e) => set('city', e.target.value)} />
              </div>
              <div className="flex flex-col gap-1.5">
                <Label>State / Region</Label>
                <Input value={fields.state} onChange={(e) => set('state', e.target.value)} />
              </div>
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div className="flex flex-col gap-1.5">
                <Label>Postal Code</Label>
                <Input value={fields.postalCode} onChange={(e) => set('postalCode', e.target.value)} />
              </div>
              <div className="flex flex-col gap-1.5">
                <Label>Country</Label>
                <Input value={fields.country} onChange={(e) => set('country', e.target.value)} />
              </div>
            </div>
            <div className="flex gap-3 pt-2">
              <Button type="submit" disabled={mutation.isPending}>
                {mutation.isPending ? 'Saving…' : isEdit ? 'Save changes' : 'Create account'}
              </Button>
              <Button type="button" variant="outline" onClick={() => navigate(-1)}>Cancel</Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
