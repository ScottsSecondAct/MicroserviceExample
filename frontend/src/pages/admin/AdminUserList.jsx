import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { Mail, RefreshCw, ShieldCheck, UserCheck, UserPlus, UserX } from 'lucide-react'
import { adminApi } from '../../api/users.api.js'
import { useAuth } from '../../context/AuthContext.jsx'
import { toast } from '../../hooks/use-toast.js'
import { Button } from '../../components/ui/button.jsx'
import { Badge } from '../../components/ui/badge.jsx'
import { Skeleton } from '../../components/ui/skeleton.jsx'
import { Card } from '../../components/ui/card.jsx'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '../../components/ui/table.jsx'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '../../components/ui/select.jsx'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '../../components/ui/dialog.jsx'
import { Input } from '../../components/ui/input.jsx'
import { Label } from '../../components/ui/label.jsx'
import { EmptyState } from '../../components/EmptyState.jsx'

const ROLES = ['Unassigned', 'Member', 'SalesRep', 'Manager', 'Admin']

const ROLE_VARIANT = {
  Admin: 'default',
  Manager: 'default',
  SalesRep: 'secondary',
  Member: 'secondary',
  Unassigned: 'outline',
}

export default function AdminUserList() {
  const { user: currentUser } = useAuth()
  const queryClient = useQueryClient()
  const [confirmAction, setConfirmAction] = useState(null)
  const [inviteOpen, setInviteOpen] = useState(false)
  const [inviteEmail, setInviteEmail] = useState('')

  const { data: users = [], isLoading, error } = useQuery({
    queryKey: ['admin', 'users'],
    queryFn: adminApi.listUsers,
  })

  const roleMutation = useMutation({
    mutationFn: ({ userId, role }) => adminApi.updateRole(userId, role),
    onSuccess: (_, { role }) => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'users'] })
      toast({ variant: 'success', title: `Role updated to ${role}` })
    },
    onError: (err) => toast({ variant: 'destructive', title: 'Role update failed', description: err.message }),
  })

  const activeMutation = useMutation({
    mutationFn: ({ userId, isActive }) => adminApi.setActive(userId, isActive),
    onSuccess: (_, { isActive }) => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'users'] })
      toast({ variant: 'success', title: isActive ? 'Account activated' : 'Account deactivated' })
      setConfirmAction(null)
    },
    onError: (err) => {
      toast({ variant: 'destructive', title: 'Action failed', description: err.message })
      setConfirmAction(null)
    },
  })

  const inviteMutation = useMutation({
    mutationFn: (email) => adminApi.inviteUser(email),
    onSuccess: () => {
      toast({ variant: 'success', title: 'Invite sent', description: `An invite email was sent to ${inviteEmail}.` })
      setInviteOpen(false)
      setInviteEmail('')
      queryClient.invalidateQueries({ queryKey: ['admin', 'users'] })
    },
    onError: (err) => toast({ variant: 'destructive', title: 'Invite failed', description: err.message }),
  })

  const resendInviteMutation = useMutation({
    mutationFn: (userId) => adminApi.resendInvite(userId),
    onSuccess: () => {
      toast({ variant: 'success', title: 'Invite resent' })
      queryClient.invalidateQueries({ queryKey: ['admin', 'users'] })
    },
    onError: (err) => toast({ variant: 'destructive', title: 'Resend failed', description: err.message }),
  })

  function handleRoleChange(userId, role) {
    roleMutation.mutate({ userId, role })
  }

  function handleDeactivate(user) {
    setConfirmAction({ type: 'deactivate', user })
  }

  function handleActivate(userId) {
    activeMutation.mutate({ userId, isActive: true })
  }

  function confirmDeactivate() {
    activeMutation.mutate({ userId: confirmAction.user.userId, isActive: false })
  }

  function handleInviteSubmit(e) {
    e.preventDefault()
    inviteMutation.mutate(inviteEmail)
  }

  return (
    <div>
      <div className="flex items-center justify-between mb-5">
        <div className="flex items-center gap-3">
          <ShieldCheck size={24} className="text-blue-600" />
          <h1 className="text-2xl font-bold text-gray-900">User Management</h1>
        </div>
        <Button size="sm" onClick={() => setInviteOpen(true)}>
          <UserPlus size={14} className="mr-1.5" />
          Invite user
        </Button>
      </div>

      {isLoading ? (
        <Card className="p-0 overflow-hidden">
          <Table>
            <TableHeader>
              <TableRow>
                {['User', 'Email', 'Role', 'Status', 'Joined', ''].map((h, i) => (
                  <TableHead key={i}><Skeleton className="h-4 w-16" /></TableHead>
                ))}
              </TableRow>
            </TableHeader>
            <TableBody>
              {[1, 2, 3].map((i) => (
                <TableRow key={i}>
                  {[32, 44, 20, 16, 24, 20].map((w, j) => (
                    <TableCell key={j}><Skeleton className={`h-4 w-${w}`} /></TableCell>
                  ))}
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </Card>
      ) : error ? (
        <p className="text-sm text-red-600 bg-red-50 px-3 py-2 rounded-md">{error.message}</p>
      ) : users.length === 0 ? (
        <EmptyState
          icon={<ShieldCheck size={28} />}
          heading="No users found"
          description="No user profiles exist yet."
        />
      ) : (
        <Card className="p-0 overflow-hidden">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Display Name</TableHead>
                <TableHead>Email</TableHead>
                <TableHead>Role</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Joined</TableHead>
                <TableHead className="w-48" />
              </TableRow>
            </TableHeader>
            <TableBody>
              {users.map((u) => {
                const isSelf = u.userId === currentUser?.userId
                const isPendingInvite = !!u.invitePendingAt
                return (
                  <TableRow key={u.userId} className={!u.isActive ? 'opacity-50' : ''}>
                    <TableCell className="font-medium">
                      {u.displayName || '—'}
                      {isSelf && (
                        <span className="ml-2 text-xs text-gray-400">(you)</span>
                      )}
                    </TableCell>
                    <TableCell>{u.email}</TableCell>
                    <TableCell>
                      <Select
                        value={u.role}
                        onValueChange={(role) => handleRoleChange(u.userId, role)}
                        disabled={isSelf || roleMutation.isPending}
                      >
                        <SelectTrigger className="h-7 w-32 text-xs">
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          {ROLES.map((r) => (
                            <SelectItem key={r} value={r}>{r}</SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    </TableCell>
                    <TableCell>
                      {isPendingInvite ? (
                        <Badge variant="outline" className="text-amber-600 border-amber-300 bg-amber-50">
                          <Mail size={10} className="mr-1" />
                          Invite pending
                        </Badge>
                      ) : (
                        <Badge variant={u.isActive ? 'customer' : 'churned'}>
                          {u.isActive ? 'Active' : 'Inactive'}
                        </Badge>
                      )}
                    </TableCell>
                    <TableCell>{new Date(u.createdAt).toLocaleDateString()}</TableCell>
                    <TableCell>
                      <div className="flex gap-1 justify-end">
                        {isPendingInvite ? (
                          <Button
                            size="sm"
                            variant="ghost"
                            className="h-7 text-xs text-blue-600 hover:text-blue-700 hover:bg-blue-50"
                            onClick={() => resendInviteMutation.mutate(u.userId)}
                            disabled={resendInviteMutation.isPending}
                            title="Resend invite email"
                          >
                            <RefreshCw size={14} className="mr-1" />
                            Resend invite
                          </Button>
                        ) : u.isActive ? (
                          <Button
                            size="sm"
                            variant="ghost"
                            className="h-7 text-xs text-red-600 hover:text-red-700 hover:bg-red-50"
                            onClick={() => handleDeactivate(u)}
                            disabled={isSelf || activeMutation.isPending}
                            title="Deactivate account"
                          >
                            <UserX size={14} className="mr-1" />
                            Deactivate
                          </Button>
                        ) : (
                          <Button
                            size="sm"
                            variant="ghost"
                            className="h-7 text-xs text-emerald-600 hover:text-emerald-700 hover:bg-emerald-50"
                            onClick={() => handleActivate(u.userId)}
                            disabled={activeMutation.isPending}
                            title="Activate account"
                          >
                            <UserCheck size={14} className="mr-1" />
                            Activate
                          </Button>
                        )}
                      </div>
                    </TableCell>
                  </TableRow>
                )
              })}
            </TableBody>
          </Table>
        </Card>
      )}

      {/* Invite user dialog */}
      <Dialog open={inviteOpen} onOpenChange={(open) => { if (!open) { setInviteOpen(false); setInviteEmail('') } }}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Invite user</DialogTitle>
            <DialogDescription>
              Send an invite email to a new user. They'll receive a link to set their password.
            </DialogDescription>
          </DialogHeader>
          <form onSubmit={handleInviteSubmit}>
            <div className="flex flex-col gap-3 py-2">
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="inviteEmail">Email address</Label>
                <Input
                  id="inviteEmail"
                  type="email"
                  value={inviteEmail}
                  onChange={(e) => setInviteEmail(e.target.value)}
                  required
                  autoComplete="off"
                  placeholder="user@example.com"
                />
              </div>
            </div>
            <DialogFooter className="mt-2">
              <Button
                type="button"
                variant="outline"
                onClick={() => { setInviteOpen(false); setInviteEmail('') }}
                disabled={inviteMutation.isPending}
              >
                Cancel
              </Button>
              <Button type="submit" disabled={inviteMutation.isPending}>
                {inviteMutation.isPending ? 'Sending…' : 'Send invite'}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Deactivate confirmation dialog */}
      <Dialog open={!!confirmAction} onOpenChange={(open) => { if (!open) setConfirmAction(null) }}>
        <DialogContent className="sm:max-w-sm">
          <DialogHeader>
            <DialogTitle>Deactivate Account</DialogTitle>
            <DialogDescription>
              Are you sure you want to deactivate{' '}
              <strong>{confirmAction?.user?.displayName || confirmAction?.user?.email}</strong>?
              They will no longer be able to log in.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setConfirmAction(null)} disabled={activeMutation.isPending}>
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={confirmDeactivate}
              disabled={activeMutation.isPending}
            >
              {activeMutation.isPending ? 'Deactivating…' : 'Deactivate'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
