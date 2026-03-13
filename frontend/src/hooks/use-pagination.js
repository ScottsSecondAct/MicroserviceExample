import { useState, useMemo } from 'react'

export const PAGE_SIZE_OPTIONS = [10, 25, 50]

export function usePagination(data, initialPageSize = 10) {
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(initialPageSize)

  const totalCount = data.length
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))

  const safePage = Math.min(page, totalPages)

  const paginatedData = useMemo(() => {
    const start = (safePage - 1) * pageSize
    return data.slice(start, start + pageSize)
  }, [data, safePage, pageSize])

  const handlePageSizeChange = (newSize) => {
    setPageSize(newSize)
    setPage(1)
  }

  const handlePageChange = (newPage) => {
    setPage(Math.max(1, Math.min(newPage, totalPages)))
  }

  const firstRow = totalCount === 0 ? 0 : (safePage - 1) * pageSize + 1
  const lastRow = Math.min(safePage * pageSize, totalCount)

  return {
    paginatedData,
    page: safePage,
    pageSize,
    totalCount,
    totalPages,
    firstRow,
    lastRow,
    handlePageChange,
    handlePageSizeChange,
  }
}
