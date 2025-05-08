using ELTE.Cinema.Blazor.WebAssembly.ViewModels;

namespace ELTE.Cinema.Blazor.WebAssembly.Services
{
    public interface IMovieService
    {
        public Task<List<MovieViewModel>> GetMoviesAsync(bool offline = false);
        public Task<MovieViewModel> GetMovieByIdAsync(int movieId);
        public Task UpdateMovieAsync(MovieViewModel movie);
        public Task CreateMovieAsync(MovieViewModel movie);
        public Task DeleteMovieAsync(int movieId);
    }
}
