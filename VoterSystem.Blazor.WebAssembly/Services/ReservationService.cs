using AutoMapper;
using ELTE.Cinema.Blazor.WebAssembly.Exception;
using ELTE.Cinema.Blazor.WebAssembly.Infrastructure;
using ELTE.Cinema.Blazor.WebAssembly.ViewModels;
using ELTE.Cinema.Shared.Models;

namespace ELTE.Cinema.Blazor.WebAssembly.Services
{
    public class ReservationService : BaseService, IReservationService
    {
        private readonly IHttpRequestUtility _httpRequestUtility;
        private readonly IRoomService _roomService;
        private readonly IMapper _mapper;


        public ReservationService(IToastService toastService, IHttpRequestUtility httpRequestUtility, IRoomService roomService, IMapper mapper) : base(toastService)
        {
            _httpRequestUtility = httpRequestUtility;
            _roomService = roomService;
            _mapper = mapper;
        }

        public async Task<(RoomViewModel, List<SeatViewModel>)> GetSeatsByScreeningAsync(int screeningId, int roomId)
        {
            HttpResponseWrapper<List<SeatResponseDto>> seatResponse;
            try
            {
                seatResponse = await _httpRequestUtility.ExecuteGetHttpRequestAsync<List<SeatResponseDto>>($"screenings/{screeningId}/seats");
            }
            catch (HttpRequestErrorException exp)
            {
                await HandleHttpError(exp.Response);
                return new();
            }

            var room = await _roomService.GetRoomByIdAsync(roomId);

            int rows = room.Rows;
            int columns = room.Columns;

            var seats = new List<SeatViewModel>();
            SeatResponseDto? seatInfo;
            for (int i = 1; i <= rows; i++)
            {
                for (int j = 1; j <= columns; j++)
                {
                    seatInfo = seatResponse.Response.FirstOrDefault(s => s.Column == j && s.Row == i);
                    seats.Add(new SeatViewModel
                    {
                        Row = i,
                        Column = j,
                        Status = ConvertToSeatStatus(seatInfo?.Status),
                        ReservationId = seatInfo?.ReservationId
                    });
                }
            }

            return (room, seats);
        }

        public async Task<SeatDetailViewModel> LoadSelectedSeatDataAsync(SeatViewModel seat)
        {
            ReservationViewModel? reservation = null;
            if (seat.ReservationId != null)
            {
                try
                {
                    var response = await _httpRequestUtility.ExecuteGetHttpRequestAsync<ReservationResponseDto>($"reservations/{seat.ReservationId}");
                    reservation = _mapper.Map<ReservationViewModel>(response.Response);
                }
                catch (HttpRequestErrorException exp)
                {
                    await HandleHttpError(exp.Response);
                }
                
            }

            return new SeatDetailViewModel()
            {
                Reservation = reservation,
                Seat = seat
            };
        }

        public async Task<SeatViewModel?> SellSeatAsync(int screeningId, SeatViewModel seat)
        {
            var seatRequestDto = _mapper.Map<SeatRequestDto>(seat);
            SeatResponseDto? soldSeat;

            try
            {
                soldSeat = await _httpRequestUtility.ExecutePutHttpRequestAsync<SeatRequestDto, SeatResponseDto>
                        ($"screenings/{screeningId}/seats/sell", seatRequestDto);
            }
            catch (HttpRequestErrorException exp)
            {
                await HandleHttpError(exp.Response);
                return null;
            }

            return _mapper.Map<SeatViewModel>(soldSeat);
        }

        private SeatStatusViewModel ConvertToSeatStatus(SeatStatusDto? status)
        {
            switch (status)
            {
                case SeatStatusDto.Reserved:
                    return SeatStatusViewModel.Reserved;
                case SeatStatusDto.Sold:
                    return SeatStatusViewModel.Sold;
                case null:
                    return SeatStatusViewModel.Free;
            }
            return SeatStatusViewModel.Free;
        }

        public async Task DeleteReservationAsync(int reservationId)
        {
            try
            {
                await _httpRequestUtility.ExecuteDeleteHttpRequestAsync($"reservations/{reservationId}");
            }
            catch (HttpRequestErrorException exp)
            {
                await HandleHttpError(exp.Response);
            }
        }
    }
}
