using ELTE.Cinema.Blazor.WebAssembly.ViewModels;

namespace ELTE.Cinema.Blazor.WebAssembly.Services
{
    public interface IRoomService
    {
        public Task<List<RoomViewModel>> GetRoomsAsync();
        public Task DeleteRoomAsync(int roomId);
        public Task CreateRoomAsync(RoomViewModel room);
        public Task<RoomViewModel> GetRoomByIdAsync(int roomId);
        public Task UpdateRoomAsync(RoomViewModel room);
    }
}
