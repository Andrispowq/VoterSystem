using AutoMapper;
using ELTE.Cinema.Blazor.WebAssembly.ViewModels;
using ELTE.Cinema.Shared.Models;

namespace ELTE.Cinema.Blazor.WebAssembly.Infrastructure
{
    public class BlazorMappingProfile : Profile
    {
        public BlazorMappingProfile()
        {
            CreateMap<MovieResponseDto, MovieViewModel>(MemberList.Source);
            CreateMap<MovieViewModel, MovieRequestDto>(MemberList.Destination);

            CreateMap<LoginViewModel, LoginRequestDto>(MemberList.Source);

            CreateMap<RoomResponseDto, RoomViewModel>(MemberList.Source);
            CreateMap<RoomViewModel, RoomRequestDto>(MemberList.Destination);

            CreateMap<ScreeningResponseDto, ScreeningViewModel>(MemberList.Source);
            CreateMap<ScreeningViewModel, ScreeningRequestDto>(MemberList.Destination)
                .ForMember(dest => dest.RoomId, opt => opt.MapFrom(src => src.Room!.Id))
                .ForMember(dest => dest.MovieId, opt => opt.MapFrom(src => src.Movie!.Id));

            CreateMap<ReservationResponseDto, ReservationViewModel>(MemberList.Destination);
            CreateMap<SeatResponseDto, SeatViewModel>(MemberList.Destination)
                .ForMember(dest => dest.IsSelected, opt => opt.Ignore());

            CreateMap<SeatViewModel, SeatRequestDto>(MemberList.Destination);
        }
    }
}
