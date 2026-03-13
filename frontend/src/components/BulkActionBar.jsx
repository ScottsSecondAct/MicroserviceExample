import { X } from 'lucide-react'
import { Button } from './ui/button.jsx'

export function BulkActionBar({ selectedCount, onClearSelection, children }) {
  if (selectedCount === 0) return null

  return (
    <div className="flex items-center gap-3 px-4 py-2 bg-blue-50 border-b border-blue-100">
      <span className="text-sm font-medium text-blue-800 shrink-0">
        {selectedCount} selected
      </span>
      <div className="flex gap-2 flex-1 flex-wrap">
        {children}
      </div>
      <Button
        size="icon"
        variant="ghost"
        className="h-7 w-7 shrink-0 text-blue-700 hover:text-blue-900 hover:bg-blue-100"
        onClick={onClearSelection}
        title="Clear selection"
      >
        <X size={14} />
      </Button>
    </div>
  )
}
