using RunBot2024.Models;
using RunBot2024.Services.Interfaces;
using Npgsql;
using System.Text;
using Dapper;

namespace RunBot2024.Services
{
    public class RivalService : IRivalService
    {
        private readonly IConfiguration _configuration;

        public RivalService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<bool> CreateRivalAsync(RivalModel rival)
        {
            using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("NpgConnection")))
            {
                StringBuilder query = new StringBuilder();

                query.Append($"INSERT INTO \"{_configuration["PostgreDefaultSchema"]}\".\"{_configuration["RivalTable"]}\" ");
                query.Append($"(\"TelegramId\", \"Name\", \"Company\", \"Gender\", \"Age\", \"TotalResult\", \"CreatedAt\", \"UpdatedAt\") ");
                query.Append($"VALUES ({rival.TelegramId}, \'{rival.Name}\', \'{rival.Company}\', \'{rival.Gender}\', {rival.Age}, {rival.TotalResult}, \'{rival.CreatedAt}\', \'{rival.UpdatedAt}\');");

                await connection.OpenAsync();
               var rows = await connection.ExecuteAsync(query.ToString());
                await connection.CloseAsync();
                query.Clear();

                if (rows > 0)
                { return true; }

                return false;
            }
        }

        public async Task<bool> DeleteRivalByIdAsync(long telegramId)
        {
            using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("NpgConnection")))
            {
                StringBuilder query = new StringBuilder();
                query.Append($"DELETE FROM \"{_configuration["PostgreDefaultSchema"]}\".\"{_configuration["RivalTable"]}\" ");
                query.Append($"WHERE \"TelegramId\" = {telegramId};");

                await connection.OpenAsync();
                var rows = await connection.ExecuteAsync(query.ToString());
                await connection.CloseAsync();

                query.Clear();

                if (rows > 0)
                { return true; }

                return false;
            }
        }

        public async Task<bool> DeleteRivalByNameAsync(string name)
        {
            using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("NpgConnection")))
            {
                StringBuilder query = new StringBuilder();
                query.Append($"DELETE FROM \"{_configuration["PostgreDefaultSchema"]}\".\"{_configuration["RivalTable"]}\" ");
                query.Append($"WHERE \"Name\" = '{name}' LIMIT 1;");

                await connection.OpenAsync();
                var rows = await connection.ExecuteAsync(query.ToString());
                await connection.CloseAsync();

                query.Clear();
                if (rows > 0)
                { return true; }

                return false;
            }
        }

        public async Task<List<RivalModel>> GetAllRivalsAsync()
        {
            using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("NpgConnection")))
            {
                await connection.OpenAsync();
                var response = await connection.QueryAsync<RivalModel>($"SELECT * FROM \"{_configuration["PostgreDefaultSchema"]}\".\"{_configuration["RivalTable"]}\"");
                await connection.CloseAsync();
                return response.ToList();
            }
        }

        public async Task<RivalModel> GetRivalByIdAsync(long telegramId)
        {
            using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("NpgConnection")))
            {
                await connection.OpenAsync();
                var response = await connection
                    .QueryAsync<RivalModel>
                    ($"SELECT * FROM \"{_configuration["PostgreDefaultSchema"]}\".\"{_configuration["RivalTable"]}\" WHERE \"TelegramId\" = {telegramId}");
                await connection.CloseAsync();
                return response.FirstOrDefault();
            }
        }

        public async Task<RivalModel> GetRivalByNamAsync(string name)
        {
            using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("NpgConnection")))
            {
                await connection.OpenAsync();
                var response = await connection
                    .QueryAsync<RivalModel>
                    ($"SELECT * FROM \"{_configuration["PostgreDefaultSchema"]}\".\"{_configuration["RivalTable"]}\" WHERE \"Name\" = {name}");
                await connection.CloseAsync();
                return response.FirstOrDefault();
            }
        }

        public async Task<bool> UpdateRivalAsync(RivalModel rival)
        {
            using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("NpgConnection")))
            {
                StringBuilder query = new StringBuilder();
                query
                    .Append($"UPDATE \"{_configuration["PostgreDefaultSchema"]}\".\"{_configuration["RivalTable"]}\" ")
                    .Append( $"SET \"Name\" = '{rival.Name}', \"TotalResult\" = {rival.TotalResult.ToString().Replace(',', '.')}, " )
                    .Append($"\"Company\" = '{rival.Company}', \"UpdatedAt\"  = '{rival.UpdatedAt}' ")
                    .Append($"WHERE \"TelegramId\" = {rival.TelegramId}");

                await connection.OpenAsync();
                var rows = await connection.ExecuteAsync(query.ToString(), rival);
                await connection.CloseAsync();

                if (rows > 0)
                { return true; }

                return false;
            }
        }
    }
}
