import { useState, useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { dealsApi } from '../../api/deals.api.js'
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

const STAGES = ['Prospecting', 'Proposal', 'Negotiation', 'ClosedWon', 'ClosedLost']

export default function DealForm() {
  const { id } = useParams()
  const isEdit = !!id
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const [form, setForm] = useState({
    title: '',
    accountId: '',
    stage: 'Prospecting',
    value: '',
    probability: '',
    expectedCloseDate: '',
    ownerId: '',
  })
  const [error, setError] = useState('')

  const { data: existing } = useQuery({
    queryKey: ['deal', id],
    queryFn: () => dealsApi.get(id),
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
      setForm({
        title: existing.title ?? '',
        accountId: existing.accountId ?? '',
        stage: existing.stage ?? 'Prospecting',
        value: existing.value ?? '',
        probability: existing.probability ?? '',
        expectedCloseDate: existing.expectedCloseDate ? existing.expectedCloseDate.slice(0, 10) : '',
        ownerId: existing.ownerId ?? '',
      })
    }
  }, [existing])

  const mutation = useMutation({
    mutationFn: (data) => isEdit ? dealsApi.update(id, data) : dealsApi.create(data),
    onSuccess: (deal) => {
      queryClient.invalidateQueries({ queryKey: ['pipeline'] })
      queryClient.invalidateQueries({ queryKey: ['deals'] })
      navigate(isEdit ? `/deals/${id}` : `/deals/${deal.dealId}`)
    },
    onError: (err) => setError(err.message),
  })

  const set = (field) => (e) => setForm((f) => ({ ...f, [field]: e.target.value }))
  const setSelect = (field) => (value) => setForm((f) => ({ ...f, [field]: value === '_none' ? '' : value }))

  const handleSubmit = (e) => {
    e.preventDefault()
    if (!form.title.trim()) { setError('Title is required.'); return }
    setError('')
    mutation.mutate({
      title: form.title,
      accountId: form.accountId || undefined,
      stage: form.stage,
      value: parseFloat(form.value) || 0,
      probability: form.probability !== '' ? parseInt(form.probability) : undefined,
      expectedCloseDate: form.expectedCloseDate || undefined,
      ownerId: form.ownerId || undefined,
    })
  }

  const breadcrumbItems = isEdit && existing
    ? [
        { label: 'Pipeline', to: '/deals' },
        { label: existing.title, to: `/deals/${id}` },
        { label: 'Edit' },
      ]
    : [
        { label: 'Pipeline', to: '/deals' },
        { label: 'New Deal' },
      ]

  return (
    <div className="max-w-lg">
      <Breadcrumb items={breadcrumbItems} />
      <h1 className="text-2xl font-bold text-gray-900 mb-5">{isEdit ? 'Edit Deal' : 'New Deal'}</h1>

      <Card>
        <CardContent className="pt-6">
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            {error && (
              <p className="text-sm text-red-600 bg-red-50 px-3 py-2 rounded-md border border-red-200">{error}</p>
            )}

            <div className="flex flex-col gap-1.5">
              <Label>Title *</Label>
              <Input value={form.title} onChange={set('title')} required />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label>Account</Label>
              <Select value={form.accountId || '_none'} onValueChange={setSelect('accountId')}>
                <SelectTrigger><SelectValue placeholder="— None —" /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="_none">— None —</SelectItem>
                  {accounts.map((a) => <SelectItem key={a.accountId} value={a.accountId}>{a.name}</SelectItem>)}
                </SelectContent>
              </Select>
            </div>

            <div className="flex flex-col gap-1.5">
              <Label>Stage</Label>
              <Select value={form.stage} onValueChange={(v) => setForm((f) => ({ ...f, stage: v }))}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  {STAGES.map((s) => <SelectItem key={s} value={s}>{s}</SelectItem>)}
                </SelectContent>
              </Select>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div className="flex flex-col gap-1.5">
                <Label>Value ($)</Label>
                <Input type="number" min="0" step="0.01" value={form.value} onChange={set('value')} />
              </div>
              <div className="flex flex-col gap-1.5">
                <Label>Probability (%)</Label>
                <Input type="number" min="0" max="100" value={form.probability} onChange={set('probability')} />
              </div>
            </div>

            <div className="flex flex-col gap-1.5">
              <Label>Expected Close Date</Label>
              <Input type="date" value={form.expectedCloseDate} onChange={set('expectedCloseDate')} />
            </div>

            <div className="flex flex-col gap-1.5">
              <Label>Owner</Label>
              <Select value={form.ownerId || '_unassigned'} onValueChange={setSelect('ownerId')}>
                <SelectTrigger><SelectValue placeholder="— Unassigned —" /></SelectTrigger>
                <SelectContent>
                  <SelectItem value="_unassigned">— Unassigned —</SelectItem>
                  {team.map((m) => <SelectItem key={m.userId} value={m.userId}>{m.displayName || m.userId}</SelectItem>)}
                </SelectContent>
              </Select>
            </div>

            <div className="flex gap-3 pt-2">
              <Button type="submit" disabled={mutation.isPending}>
                {isEdit ? 'Save Changes' : 'Create Deal'}
              </Button>
              <Button type="button" variant="outline" onClick={() => navigate(-1)}>Cancel</Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  )
}
