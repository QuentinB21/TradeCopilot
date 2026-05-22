import { useState } from "react";

export function usePagination<T>(items: T[], pageSize = 8) {
  const [page, setPage] = useState(1);
  const pageCount = Math.max(1, Math.ceil(items.length / pageSize));
  const currentPage = Math.min(page, pageCount);
  const start = (currentPage - 1) * pageSize;

  return {
    currentPage,
    pageCount,
    pageItems: items.slice(start, start + pageSize),
    pageSize,
    setPage,
    totalItems: items.length
  };
}
