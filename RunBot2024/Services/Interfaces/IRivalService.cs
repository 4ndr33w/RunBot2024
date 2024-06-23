using RunBot2024.Models;

namespace RunBot2024.Services.Interfaces
{
    public interface IRivalService
    {
        Task<bool> CreateRivalAsync(RivalModel rival);
        Task<RivalModel> GetRivalByIdAsync(long telegramId);
        Task<RivalModel> GetRivalByNamAsync(string name);
        Task<List<RivalModel>> GetAllRivalsAsync();
        Task<bool> UpdateRivalAsync(RivalModel rival);
        Task<bool> DeleteRivalByIdAsync(long telegramId);
        Task<bool> DeleteRivalByNameAsync(string name);

        Task<IEnumerable<CompanyStatisticModel>> GetCompanyStatisitcs();
    }
}
