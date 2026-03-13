import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Link, useNavigate } from 'react-router-dom'
import { accountsApi } from '../../api/accounts.api.js'
import AccountForm from './AccountForm.jsx'
import { Button } from '../../components/ui/button.jsx'
import { Skeleton } from '../../components/ui/skeleton.jsx'
import { Card } from '../../components/ui/card.jsx'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../../components/ui/table.jsx'
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
} from '../../components/ui/sheet.jsx'
import { useSortableTable, SortIcon } from '../../hooks/use-sortable-table.jsx'

export default function AccountList() {
  const navigate = useNavigate()
  const [sheetOpen, setSheetOpen] = useState(false)

  const { data: accounts = [], isLoading, error } = useQuery({
    queryKey: ['accounts'],
    queryFn: accountsApi.list,
  })

  const { sortedData: sortedAccounts, sortKey, sortDir, handleSort } = useSortableTable(accounts, 'name')

  return (
    <div>
      <div className="flex items-center justify-between mb-5">
        <h1 className="text-2xl font-bold text-gray-900">Accounts</h1>
        <Button onClick={() => setSheetOpen(true)}>+ New Account</Button>
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
                <TableHead className="cursor-pointer select-none" onClick={() => handleSort('name')}>
                  Name <SortIcon active={sortKey === 'name'} dir={sortDir} />
                </TableHead>
                <TableHead className="cursor-pointer select-none" onClick={() => handleSort('industry')}>
                  Industry <SortIcon active={sortKey === 'industry'} dir={sortDir} />
                </TableHead>
                <TableHead className="cursor-pointer select-none" onClick={() => handleSort('size')}>
                  Size <SortIcon active={sortKey === 'size'} dir={sortDir} />
                </TableHead>
                <TableHead>Website</TableHead>
                <TableHead className="cursor-pointer select-none" onClick={() => handleSort('createdAt')}>
                  Created <SortIcon active={sortKey === 'createdAt'} dir={sortDir} />
                </TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {sortedAccounts.map((a) => (
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

      <Sheet open={sheetOpen} onOpenChange={setSheetOpen}>
        <SheetContent className="sm:max-w-lg overflow-y-auto">
          <SheetHeader>
            <SheetTitle>New Account</SheetTitle>
          </SheetHeader>
          <div className="mt-5">
            <AccountForm
              onSuccess={(result) => {
                setSheetOpen(false)
                navigate(`/accounts/${result.accountId}`)
              }}
              onClose={() => setSheetOpen(false)}
            />
          </div>
        </SheetContent>
      </Sheet>
    </div>
  )
}
