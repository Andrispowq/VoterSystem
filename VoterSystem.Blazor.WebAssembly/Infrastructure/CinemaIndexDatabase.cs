using CloudNimble.BlazorEssentials.IndexedDb;
using Microsoft.JSInterop;

namespace ELTE.Cinema.Blazor.WebAssembly.Infrastructure
{
    public class CinemaIndexDatabase(IJSRuntime jsRuntime) : IndexedDbDatabase(jsRuntime)
    {
        [ObjectStore(AutoIncrementKeys = true)]
        public IndexedDbObjectStore Movies { get; set; } = null!;
    }
}
