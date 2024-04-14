using RunBot2024.Models;
using RunBot2024.Services.Interfaces;
using Npgsql;
using System.Text;
using Telegram.Bot.Types;
using Dapper;
using System.Xml.Linq;

namespace RunBot2024.Services
{
    public class RivalService : IRivalService
    {
        private readonly IConfiguration _configuration;

        public RivalService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task CreateRivalAsync(RivalModel rival)
        {
            using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("NpgConnection")))
            {
                StringBuilder query = new StringBuilder();

                query.Append($"INSERT INTO \"{_configuration["PostgreDefaultSchema"]}\".\"{_configuration["RivalTable"]}\" ");
                query.Append($"(\"TelegramId\", \"Name\", \"Company\", \"Gender\", \"Age\", \"TotalResult\", \"CreatedAt\", \"UpdatedAt\") ");
                query.Append($"VALUES ({rival.TelegramId}, \'{rival.Name}\', \'{rival.Company}\', \'{rival.Gender}\', {rival.Age}, {rival.TotalResult}, \'{rival.CreatedAt}\', {rival.UpdatedAt})");

                await connection.OpenAsync();
                await connection.ExecuteAsync(query.ToString());
                await connection.CloseAsync();
                query.Clear();
            }
        }

        public async Task DeleteRivalByIdAsync(long telegramId)
        {
            using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("NpgConnection")))
            {
                StringBuilder query = new StringBuilder();
                query.Append($"DELETE FROM \"{_configuration["PostgreDefaultSchema"]}\".\"{_configuration["RivalTable"]}\" ");
                query.Append($"WHERE \"TelegramId\" = {telegramId} LIMIT 1;");

                await connection.ExecuteAsync(query.ToString());

                query.Clear();
            }
        }

        public async Task DeleteRivalByNameAsync(string name)
        {
            using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("NpgConnection")))
            {
                StringBuilder query = new StringBuilder();
                query.Append($"DELETE FROM \"{_configuration["PostgreDefaultSchema"]}\".\"{_configuration["RivalTable"]}\" ");
                query.Append($"WHERE \"Name\" = '{name}' LIMIT 1;");

                await connection.ExecuteAsync(query.ToString());

                query.Clear();
            }
        }

        public async Task<List<RivalModel>> GetAllRivalsAsync()
        {
            using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("NpgConnection")))
            {
                var response = await connection.QueryAsync<RivalModel>($"SELECT * FROM \"{_configuration["PostgreDefaultSchema"]}\".\"{_configuration["RivalTable"]}\"");
                return response.ToList();
            }
        }

        public async Task<RivalModel> GetRivalByIdAsync(long telegramId)
        {
            using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("NpgConnection")))
            {
                var response = await connection
                    .QueryAsync<RivalModel>
                    ($"SELECT * FROM \"{_configuration["PostgreDefaultSchema"]}\".\"{_configuration["RivalTable"]}\" WHERE \"Id\" = {telegramId}");
                return response.FirstOrDefault();
            }
        }

        public async Task<RivalModel> GetRivalByNamAsync(string name)
        {
            using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("NpgConnection")))
            {
                var response = await connection
                    .QueryAsync<RivalModel>
                    ($"SELECT * FROM \"{_configuration["PostgreDefaultSchema"]}\".\"{_configuration["RivalTable"]}\" WHERE \"Name\" = {name}");
                return response.FirstOrDefault();
            }
        }

        public async Task UpdateRivalAsync(RivalModel rival)
        {
            using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("NpgConnection")))
            {
                StringBuilder query = new StringBuilder();
                query.Append($"UPDATE \"{_configuration["PostgreDefaultSchema"]}\".\"{_configuration["RivalTable"]}\" ");
                query.Append( $"SET \"Name\" = '{rival.Name}', \"TotalResult\" = {rival.TotalResult}, \"Company\" = '{rival.Company}', \"UpdatedAt\"  = '{rival.UpdatedAt}' " );
                query.Append($"WHERE \"TelegramId\" = {rival.TelegramId}");

                await connection.ExecuteAsync(query.ToString(), rival);
            }
        }
    }
}
