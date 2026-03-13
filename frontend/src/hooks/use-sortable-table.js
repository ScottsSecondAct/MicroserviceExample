import { useState, useMemo } from 'react'

export function useSortableTable(data, initialKey = null, initialDir = 'asc') {
  const [sortKey, setSortKey] = useState(initialKey)
  const [sortDir, setSortDir] = useState(initialDir)

  const handleSort = (key) => {
    if (sortKey === key) {
      setSortDir((d) => (d === 'asc' ? 'desc' : 'asc'))
    } else {
      setSortKey(key)
      setSortDir('asc')
    }
  }

  const sortedData = useMemo(() => {
    if (!sortKey) return data
    return [...data].sort((a, b) => {
      const aVal = a[sortKey]
      const bVal = b[sortKey]
      if (aVal == null && bVal == null) return 0
      if (aVal == null) return sortDir === 'asc' ? 1 : -1
      if (bVal == null) return sortDir === 'asc' ? -1 : 1
      if (typeof aVal === 'string' && typeof bVal === 'string') {
        const cmp = aVal.localeCompare(bVal)
        return sortDir === 'asc' ? cmp : -cmp
      }
      if (aVal < bVal) return sortDir === 'asc' ? -1 : 1
      if (aVal > bVal) return sortDir === 'asc' ? 1 : -1
      return 0
    })
  }, [data, sortKey, sortDir])

  return { sortedData, sortKey, sortDir, handleSort }
}

export function SortIcon({ active, dir }) {
  if (!active) {
    return (
      <span className="ml-1 inline-flex flex-col text-[8px] leading-none text-gray-300 select-none">
        <span>▲</span>
        <span>▼</span>
      </span>
    )
  }
  return (
    <span className="ml-1 inline-block text-[10px] text-gray-600 select-none">
      {dir === 'asc' ? '▲' : '▼'}
    </span>
  )
}
