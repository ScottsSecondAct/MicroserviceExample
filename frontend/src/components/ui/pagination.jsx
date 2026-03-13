import { Button } from './button.jsx'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from './select.jsx'
import { PAGE_SIZE_OPTIONS } from '../../hooks/use-pagination.js'

export function Pagination({
  page,
  pageSize,
  totalCount,
  totalPages,
  firstRow,
  lastRow,
  onPageChange,
  onPageSizeChange,
}) {
  return (
    <div className="flex items-center justify-between px-4 py-3 border-t border-gray-100 text-sm text-gray-600">
      <div className="flex items-center gap-2">
        <span>Rows per page:</span>
        <Select value={String(pageSize)} onValueChange={(v) => onPageSizeChange(Number(v))}>
          <SelectTrigger className="h-8 w-20 text-sm">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {PAGE_SIZE_OPTIONS.map((size) => (
              <SelectItem key={size} value={String(size)}>{size}</SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="flex items-center gap-4">
        <span>
          {firstRow}–{lastRow} of {totalCount}
        </span>
        <div className="flex items-center gap-1">
          <Button
            variant="outline"
            size="sm"
            className="h-8 px-2"
            disabled={page <= 1}
            onClick={() => onPageChange(page - 1)}
          >
            ‹ Prev
          </Button>
          <Button
            variant="outline"
            size="sm"
            className="h-8 px-2"
            disabled={page >= totalPages}
            onClick={() => onPageChange(page + 1)}
          >
            Next ›
          </Button>
        </div>
      </div>
    </div>
  )
}
