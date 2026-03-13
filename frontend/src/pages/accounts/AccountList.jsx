import { useQuery } from '@tanstack/react-query'
import { Link, useNavigate } from 'react-router-dom'
import { accountsApi } from '../../api/accounts.api.js'
import { Button } from '../../components/ui/button.jsx'
import { Skeleton } from '../../components/ui/skeleton.jsx'
import { Card } from '../../components/ui/card.jsx'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../../components/ui/table.jsx'

export default function AccountList() {
  const navigate = useNavigate()

  const { data: accounts = [], isLoading, error } = useQuery({
    queryKey: ['accounts'],
    queryFn: accountsApi.list,
  })

  return (
    <div>
      <div className="flex items-center justify-between mb-5">
        <h1 className="text-2xl font-bold text-gray-900">Accounts</h1>
        <Button onClick={() => navigate('/accounts/new')}>+ New Account</Button>
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
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Name</TableHead>
                <TableHead>Industry</TableHead>
                <TableHead>Size</TableHead>
                <TableHead>Website</TableHead>
                <TableHead>Created</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {accounts.map((a) => (
                <TableRow
                  key={a.accountId}
                  className="cursor-pointer"
                  onClick={() => navigate(`/accounts/${a.accountId}`)}
                >
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
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Card>
      )}
    </div>
  )
}
