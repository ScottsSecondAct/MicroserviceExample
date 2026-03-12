import { useState, useEffect } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { accountsApi } from '../../api/accounts.api.js'
import Breadcrumb from '../../components/Breadcrumb.jsx'

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
    const payload = {
      ...fields,
      industry: fields.industry || null,
      size: fields.size || null,
      website: fields.website || null,
      street: fields.street || null,
      city: fields.city || null,
      state: fields.state || null,
      postalCode: fields.postalCode || null,
      country: fields.country || null,
    }
    mutation.mutate(payload)
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
      <h1>{isEdit ? 'Edit Account' : 'New Account'}</h1>
      <div className="card">
        <form onSubmit={handleSubmit} className="edit-form">
          {error && <p className="form-error">{error}</p>}
          <label>
            Name *
            <input value={fields.name} onChange={(e) => set('name', e.target.value)} required />
          </label>
          <div className="form-row">
            <label>
              Industry
              <select value={fields.industry} onChange={(e) => set('industry', e.target.value)}>
                <option value="">— Select —</option>
                {INDUSTRIES.map((i) => <option key={i} value={i}>{i}</option>)}
              </select>
            </label>
            <label>
              Size
              <select value={fields.size} onChange={(e) => set('size', e.target.value)}>
                <option value="">— Select —</option>
                {SIZES.map((s) => <option key={s} value={s}>{s}</option>)}
              </select>
            </label>
          </div>
          <label>
            Website
            <input type="url" value={fields.website} onChange={(e) => set('website', e.target.value)} placeholder="https://" />
          </label>
          <h3>Address</h3>
          <label>
            Street
            <input value={fields.street} onChange={(e) => set('street', e.target.value)} />
          </label>
          <div className="form-row">
            <label>
              City
              <input value={fields.city} onChange={(e) => set('city', e.target.value)} />
            </label>
            <label>
              State / Region
              <input value={fields.state} onChange={(e) => set('state', e.target.value)} />
            </label>
          </div>
          <div className="form-row">
            <label>
              Postal Code
              <input value={fields.postalCode} onChange={(e) => set('postalCode', e.target.value)} />
            </label>
            <label>
              Country
              <input value={fields.country} onChange={(e) => set('country', e.target.value)} />
            </label>
          </div>
          <div className="form-actions">
            <button type="submit" className="btn btn-primary" disabled={mutation.isPending}>
              {mutation.isPending ? 'Saving…' : isEdit ? 'Save changes' : 'Create account'}
            </button>
            <button type="button" className="btn btn-secondary" onClick={() => navigate(-1)}>
              Cancel
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
