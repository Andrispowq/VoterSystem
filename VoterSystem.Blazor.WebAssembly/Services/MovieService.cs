using AutoMapper;
using ELTE.Cinema.Blazor.WebAssembly.Exception;
using ELTE.Cinema.Blazor.WebAssembly.Infrastructure;
using ELTE.Cinema.Blazor.WebAssembly.ViewModels;
using ELTE.Cinema.Shared.Models;

namespace ELTE.Cinema.Blazor.WebAssembly.Services
{
    public class MovieService : BaseService, IMovieService
    {
        private readonly IMapper _mapper;
        private readonly IHttpRequestUtility _httpRequestUtility;
        private readonly CinemaIndexDatabase _cinemaIndexDatabase;

        public MovieService(IMapper mapper, IHttpRequestUtility httpRequestUtility, IToastService toastService,
            CinemaIndexDatabase cinemaIndexDatabase) : base(toastService)
        {
            _mapper = mapper;
            _httpRequestUtility = httpRequestUtility;
            _cinemaIndexDatabase = cinemaIndexDatabase;
        }

        public async Task<List<MovieViewModel>> GetMoviesAsync(bool offline)
        {
            if (offline)
            {
                return await LoadMoviesFromLocalDatabaseAsync();
            }

            try
            {
                var response = await _httpRequestUtility.ExecuteGetHttpRequestAsync<List<MovieResponseDto>>("movies");
                var movieViewModels = _mapper.Map<List<MovieViewModel>>(response.Response);
                await SaveMoviesToDatabase(movieViewModels);
                return movieViewModels;
            }
            catch (HttpRequestErrorException exp)
            {
                await HandleHttpError(exp.Response);
            }
            return new();
        }

        private async Task<List<MovieViewModel>> LoadMoviesFromLocalDatabaseAsync()
        {
            return await _cinemaIndexDatabase.Movies.GetAllAsync<MovieViewModel>();
        }

        private async Task SaveMoviesToDatabase(List<MovieViewModel> movies)
        {
            await _cinemaIndexDatabase.Movies.ClearStoreAsync();
            await _cinemaIndexDatabase.Movies.BatchAddAsync(movies.ToArray());
        }

        public async Task<MovieViewModel> GetMovieByIdAsync(int movieId)
        {
            try
            {
                var response = await _httpRequestUtility.ExecuteGetHttpRequestAsync<MovieResponseDto>($"movies/{movieId}");
                return _mapper.Map<MovieViewModel>(response.Response);
            }
            catch (HttpRequestErrorException exp)
            {
                await HandleHttpError(exp.Response);
            }
            return new();
        }

        public async Task UpdateMovieAsync(MovieViewModel movie)
        {
            try
            {
                var movieRequestDto = _mapper.Map<MovieRequestDto>(movie);
                await _httpRequestUtility.ExecutePutHttpRequestAsync<MovieRequestDto, MovieResponseDto>($"movies/{movie.Id}", movieRequestDto);
            }
            catch (HttpRequestErrorException exp)
            {
                await HandleHttpError(exp.Response);
            }
        }

        public async Task CreateMovieAsync(MovieViewModel movie)
        {
            var movieRequestDto = _mapper.Map<MovieRequestDto>(movie);
            try
            {
                await _httpRequestUtility.ExecutePostHttpRequestAsync<MovieRequestDto, MovieResponseDto>("movies", movieRequestDto);
            }
            catch (HttpRequestErrorException exp)
            {
                await HandleHttpError(exp.Response);
            }
        }

        public async Task DeleteMovieAsync(int movieId)
        {
            try
            {
                await _httpRequestUtility.ExecuteDeleteHttpRequestAsync($"movies/{movieId}");
            }
            catch (HttpRequestErrorException exp)
            {
                await HandleHttpError(exp.Response);
            }
        }
    }
}
