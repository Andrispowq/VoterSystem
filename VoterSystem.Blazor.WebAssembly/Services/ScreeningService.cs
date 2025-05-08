using AutoMapper;
using ELTE.Cinema.Blazor.WebAssembly.Exception;
using ELTE.Cinema.Blazor.WebAssembly.Infrastructure;
using ELTE.Cinema.Blazor.WebAssembly.ViewModels;
using ELTE.Cinema.Shared.Models;

namespace ELTE.Cinema.Blazor.WebAssembly.Services
{
    public class ScreeningService : BaseService, IScreeningService
    {
        private readonly IMapper _mapper;
        private readonly IHttpRequestUtility _httpRequestUtility;


        public ScreeningService(IMapper mapper, IHttpRequestUtility httpRequestUtility, IToastService toastService) : base(toastService)
        {
            _mapper = mapper;
            _httpRequestUtility = httpRequestUtility;
        }

        public async Task CreateScreeningAsync(ScreeningViewModel screening)
        {
            var screeningRequestDto = _mapper.Map<ScreeningRequestDto>(screening);
            try
            {
                await _httpRequestUtility.ExecutePostHttpRequestAsync<ScreeningRequestDto, ScreeningResponseDto>("screenings", screeningRequestDto);
            }
            catch (HttpRequestErrorException exp)
            {
                await HandleHttpError(exp.Response);
            }
        }

        public async Task DeleteScreeningAsync(int screeningId)
        {
            try
            {
                await _httpRequestUtility.ExecuteDeleteHttpRequestAsync($"screenings/{screeningId}");
            }
            catch (HttpRequestErrorException exp)
            {
                await HandleHttpError(exp.Response);
            }
        }

        public async Task<ScreeningViewModel> GeScreeningByIdAsync(int screeningId)
        {
            try
            {
                var response = await _httpRequestUtility.ExecuteGetHttpRequestAsync<ScreeningResponseDto>($"screenings/{screeningId}");
                return _mapper.Map<ScreeningViewModel>(response.Response);
            }
            catch (HttpRequestErrorException exp)
            {
                await HandleHttpError(exp.Response);
            }

            return new();
        }

        public async Task<PagedListWrapper<ScreeningViewModel>?> GetScreeningsAsync(int page, int size, int roomId, int movieId, DateTime? startsAfter, DateTime? startsBefore)
        {
            try
            {
                var response = await _httpRequestUtility.ExecuteGetHttpRequestAsync<List<ScreeningResponseDto>>(
                    $"screenings?page={page}&size={size}{(roomId > 0 ? ("&roomId=" + roomId) : "")}" +
                    $"{(movieId > 0 ? ("&movieId=" + movieId) : "")}" +
                    $"{(startsAfter != null ? ("&startsAfter=" + startsAfter) : "")}" +
                    $"{(startsBefore != null ? ("&startsBefore=" + startsBefore) : "")}");

                var screeningItems = _mapper.Map<List<ScreeningViewModel>>(response.Response);
                var totalCount = GetPagedListTotalCount(response.Headers);

                return new PagedListWrapper<ScreeningViewModel>(screeningItems, totalCount);
            }
            catch (HttpRequestErrorException exp)
            {
                await HandleHttpError(exp.Response);
            }
            return null;
        }

        public async Task UpdateScreeningAsync(ScreeningViewModel screening)
        {
            try
            {
                var screeningDto = _mapper.Map<ScreeningRequestDto>(screening);
                await _httpRequestUtility.ExecutePutHttpRequestAsync<ScreeningRequestDto, ScreeningResponseDto>($"screenings/{screening.Id}", screeningDto);
            }
            catch (HttpRequestErrorException exp)
            {
                await HandleHttpError(exp.Response);
            }
        }
    }
}
