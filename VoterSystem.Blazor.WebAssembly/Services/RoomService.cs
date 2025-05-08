using AutoMapper;
using ELTE.Cinema.Blazor.WebAssembly.Exception;
using ELTE.Cinema.Blazor.WebAssembly.Infrastructure;
using ELTE.Cinema.Blazor.WebAssembly.ViewModels;
using ELTE.Cinema.Shared.Models;

namespace ELTE.Cinema.Blazor.WebAssembly.Services
{
    public class RoomService : BaseService, IRoomService
    {
        private readonly IMapper _mapper;
        private readonly IHttpRequestUtility _httpRequestUtility;

        public RoomService(IMapper mapper, IHttpRequestUtility httpRequestUtility, IToastService toastService) : base(toastService)
        {
            _mapper = mapper;
            _httpRequestUtility = httpRequestUtility;
        }

        public async Task<List<RoomViewModel>> GetRoomsAsync()
        {
            try
            {
                var response = await _httpRequestUtility.ExecuteGetHttpRequestAsync<List<RoomResponseDto>>("rooms");
                return _mapper.Map<List<RoomViewModel>>(response.Response);
            }
            catch (HttpRequestErrorException exp)
            {
                await HandleHttpError(exp.Response);
            }
            return new();
        }

        public async Task DeleteRoomAsync(int roomId)
        {
            try
            {
                await _httpRequestUtility.ExecuteDeleteHttpRequestAsync($"rooms/{roomId}");
            }
            catch (HttpRequestErrorException exp)
            {
                await HandleHttpError(exp.Response);
            }
        }

        public async Task CreateRoomAsync(RoomViewModel room)
        {
            var roomRequestDto = _mapper.Map<RoomRequestDto>(room);
            try
            {
                await _httpRequestUtility.ExecutePostHttpRequestAsync<RoomRequestDto, RoomResponseDto>("rooms", roomRequestDto);
            }
            catch (HttpRequestErrorException exp)
            {
                await HandleHttpError(exp.Response);
            }
        }

        public async Task<RoomViewModel> GetRoomByIdAsync(int roomId)
        {
            try
            {
                var response = await _httpRequestUtility.ExecuteGetHttpRequestAsync<RoomResponseDto>($"rooms/{roomId}");
                return _mapper.Map<RoomViewModel>(response.Response);
            }
            catch (HttpRequestErrorException exp)
            {
                await HandleHttpError(exp.Response);
            }

            return new();
        }

        public async Task UpdateRoomAsync(RoomViewModel room)
        {
            var roomRequestDto = _mapper.Map<RoomRequestDto>(room);
            try
            {
                await _httpRequestUtility.ExecutePutHttpRequestAsync<RoomRequestDto, RoomResponseDto>($"rooms/{room.Id}", roomRequestDto);
            }
            catch (HttpRequestErrorException exp)
            {
                await HandleHttpError(exp.Response);
            }
        }
    }
}
