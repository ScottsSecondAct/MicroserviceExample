import { forwardRef } from 'react'
import { cn } from '../../lib/utils'

const Table = forwardRef(function Table({ className, ...props }, ref) {
  return (
    <div className="relative w-full overflow-auto">
      <table
        ref={ref}
        className={cn('w-full caption-bottom text-sm', className)}
        {...props}
      />
    </div>
  )
})
Table.displayName = 'Table'

const TableHeader = forwardRef(function TableHeader({ className, ...props }, ref) {
  return <thead ref={ref} className={cn('[&_tr]:border-b', className)} {...props} />
})
TableHeader.displayName = 'TableHeader'

const TableBody = forwardRef(function TableBody({ className, ...props }, ref) {
  return (
    <tbody ref={ref} className={cn('[&_tr:last-child]:border-0', className)} {...props} />
  )
})
TableBody.displayName = 'TableBody'

const TableRow = forwardRef(function TableRow({ className, ...props }, ref) {
  return (
    <tr
      ref={ref}
      className={cn(
        'border-b transition-colors hover:bg-blue-50/50 data-[state=selected]:bg-muted',
        className
      )}
      {...props}
    />
  )
})
TableRow.displayName = 'TableRow'

const TableHead = forwardRef(function TableHead({ className, ...props }, ref) {
  return (
    <th
      ref={ref}
      className={cn(
        'h-10 px-3.5 text-left align-middle text-xs font-semibold uppercase tracking-wide text-muted-foreground bg-muted/50 [&:has([role=checkbox])]:pr-0',
        className
      )}
      {...props}
    />
  )
})
TableHead.displayName = 'TableHead'

const TableCell = forwardRef(function TableCell({ className, ...props }, ref) {
  return (
    <td
      ref={ref}
      className={cn('px-3.5 py-2.5 align-middle text-sm text-foreground/80 [&:has([role=checkbox])]:pr-0', className)}
      {...props}
    />
  )
})
TableCell.displayName = 'TableCell'

export { Table, TableHeader, TableBody, TableRow, TableHead, TableCell }
