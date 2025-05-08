namespace ELTE.Cinema.Blazor.WebAssembly.ViewModels
{
    public class PagedListWrapper<T>
    {
        public List<T> Items { get; init; }
        public int TotalCount { get; init; }

        public PagedListWrapper(List<T> items, int totalCount)
        {
            Items = items;
            TotalCount = totalCount;
        }
    }
}
