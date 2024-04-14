using RunBot2024.Models;
using RunBot2024.Services.Interfaces;

namespace RunBot2024.Services
{
    public class RivalService : IRivalService
    {
        private readonly IConfiguration _configuration;

        public RivalService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task CreateRivalAsync(RivalModel rival)
        {
            throw new NotImplementedException();
        }

        public Task DeleteRivalByIdAsync(long telegramId)
        {
            throw new NotImplementedException();
        }

        public Task DeleteRivalByNameAsync(string name)
        {
            throw new NotImplementedException();
        }

        public Task<List<RivalModel>> GetAllRivalsAsync()
        {
            throw new NotImplementedException();
        }

        public Task<RivalModel> GetRivalByIdAsync(long telegramId)
        {
            throw new NotImplementedException();
        }

        public Task<RivalModel> GetRivalByNamAsync(string name)
        {
            throw new NotImplementedException();
        }

        public Task UpdateRivalAsync(RivalModel rival)
        {
            throw new NotImplementedException();
        }
    }
}
