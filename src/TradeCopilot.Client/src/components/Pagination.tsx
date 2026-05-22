import { ChevronLeft, ChevronRight } from "lucide-react";

type PaginationProps = {
  currentPage: number;
  itemLabel: string;
  onPageChange: (page: number) => void;
  pageCount: number;
  pageSize: number;
  totalItems: number;
};

export function Pagination({
  currentPage,
  itemLabel,
  onPageChange,
  pageCount,
  pageSize,
  totalItems
}: PaginationProps) {
  if (totalItems <= pageSize) {
    return null;
  }

  const firstItem = (currentPage - 1) * pageSize + 1;
  const lastItem = Math.min(currentPage * pageSize, totalItems);

  return (
    <nav className="pagination" aria-label={`Pagination ${itemLabel}`}>
      <span>{firstItem}-{lastItem} sur {totalItems} {itemLabel}</span>
      <div>
        <button
          aria-label="Page precedente"
          className="paginationButton"
          disabled={currentPage === 1}
          onClick={() => onPageChange(currentPage - 1)}
          type="button"
        >
          <ChevronLeft size={16} />
        </button>
        <strong>Page {currentPage} / {pageCount}</strong>
        <button
          aria-label="Page suivante"
          className="paginationButton"
          disabled={currentPage === pageCount}
          onClick={() => onPageChange(currentPage + 1)}
          type="button"
        >
          <ChevronRight size={16} />
        </button>
      </div>
    </nav>
  );
}
