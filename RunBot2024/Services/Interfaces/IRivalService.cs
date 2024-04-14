using RunBot2024.Models;

namespace RunBot2024.Services.Interfaces
{
    public interface IRivalService
    {
        Task CreateRivalAsync(RivalModel rival);
        Task<RivalModel> GetRivalByIdAsync(long telegramId);
        Task<RivalModel> GetRivalByNamAsync(string name);
        Task<List<RivalModel>> GetAllRivalsAsync();
        Task UpdateRivalAsync(RivalModel rival);
        Task DeleteRivalByIdAsync(long telegramId);
        Task DeleteRivalByNameAsync(string name);
    }
}
