using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBIGDocumentGenerator.Domain.Models
{
    public class PagedList<T> : IPagedList<T>
    {
        public int PageIndex { get; }
        public int PageSize { get; }
        public int TotalCount { get; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
        public IEnumerable<T> Items { get; }

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        public PagedList(IEnumerable<T> items, int count, int pageIndex, int pageSize)
        {
            if (pageIndex < 1)
                throw new ArgumentOutOfRangeException(nameof(pageIndex), "PageIndex harus 1 atau lebih besar.");
            if (pageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), "PageSize harus 1 atau lebih besar.");

            PageIndex = pageIndex;
            PageSize = pageSize;
            TotalCount = count; // Menyimpan count yang sudah difilter
            Items = items ?? Enumerable.Empty<T>();
        }
    }
}
