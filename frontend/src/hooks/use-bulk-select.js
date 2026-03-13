import { useState, useCallback } from 'react'

export function useBulkSelect() {
  const [selectedIds, setSelectedIds] = useState(new Set())

  const toggleRow = useCallback((id) => {
    setSelectedIds((prev) => {
      const next = new Set(prev)
      if (next.has(id)) {
        next.delete(id)
      } else {
        next.add(id)
      }
      return next
    })
  }, [])

  const toggleAll = useCallback((ids) => {
    setSelectedIds((prev) => {
      const allSelected = ids.length > 0 && ids.every((id) => prev.has(id))
      if (allSelected) return new Set()
      return new Set(ids)
    })
  }, [])

  const clearSelection = useCallback(() => {
    setSelectedIds(new Set())
  }, [])

  const isSelected = useCallback((id) => selectedIds.has(id), [selectedIds])

  const isAllSelected = useCallback(
    (ids) => ids.length > 0 && ids.every((id) => selectedIds.has(id)),
    [selectedIds]
  )

  const isIndeterminate = useCallback(
    (ids) => ids.some((id) => selectedIds.has(id)) && !ids.every((id) => selectedIds.has(id)),
    [selectedIds]
  )

  return {
    selectedIds,
    selectedCount: selectedIds.size,
    toggleRow,
    toggleAll,
    clearSelection,
    isSelected,
    isAllSelected,
    isIndeterminate,
  }
}
