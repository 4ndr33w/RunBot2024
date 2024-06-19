using Dapper;
using Npgsql;
using RunBot2024.Models;
using RunBot2024.Services.Interfaces;
using System.Text;

namespace RunBot2024.Services
{
    public class LogService : ILogService
    {
        private readonly IConfiguration _configuration;
        public LogService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task CreateErrorLogAsync(ErrorLog errorLog)
        {
            using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("NpgConnection")))
            {
                StringBuilder query = new StringBuilder();

                query.Append($"INSERT INTO \"{_configuration["PostgreDefaultSchema"]}\".\"{_configuration["ErrorLogTable"]}\" ");
                query.Append($"(\"TelegramId\", \"ErrorMessage\", \"LastUpdated\") ");
                query.Append($"VALUES ({errorLog.TelegramId}, \'{errorLog.ErrorMessage}\', \'{errorLog.LastUpdated}\');");

                await connection.OpenAsync();
                await connection.ExecuteAsync(query.ToString());
                await connection.CloseAsync();
                query.Clear();
            }
        }

        public async Task CreateReplyLogAsync(ReplyLog replyLog)
        {
            using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("NpgConnection")))
            {
                StringBuilder query = new StringBuilder();

                query.Append($"INSERT INTO \"{_configuration["PostgreDefaultSchema"]}\".\"{_configuration["ReportLogTable"]}\" ");
                query.Append($"(\"TelegramId\", \"ReplyMessage\", \"LastUpdated\") ");
                query.Append($"VALUES ({replyLog.TelegramId}, \'{replyLog.ReplyMessage}\', \'{replyLog.LastUpdated}\');");

                await connection.OpenAsync();
                await connection.ExecuteAsync(query.ToString());
                await connection.CloseAsync();
                query.Clear();
            }
        }

        public async Task CreateResultLogAsync(ResultLog resultLog)
        {
            using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("NpgConnection")))
            {
                StringBuilder query = new StringBuilder();

                query.Append($"INSERT INTO \"{_configuration["PostgreDefaultSchema"]}\".\"{_configuration["ResultLogTable"]}\" ");
                query.Append($"(\"TelegramId\", \"TotalResult\", \"LastAddedResult\", \"Message\", \"LastUpdated\") ");
                query.Append($"VALUES ({resultLog.TelegramId}, {resultLog.TotalResult.ToString().Replace(',', '.')}, {resultLog.LastAddedResult.ToString().Replace(',', '.')}, \'{resultLog.Message}\', \'{resultLog.LastUpdated}\');");

                await connection.OpenAsync();
                await connection.ExecuteAsync(query.ToString());
                await connection.CloseAsync();
                query.Clear();
            }
        }

        public async Task DeleteResultLogAsync(long telegramId)
        {
            using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("NpgConnection")))
            {
                StringBuilder query = new StringBuilder();
                query.Append($"DELETE FROM \"{_configuration["PostgreDefaultSchema"]}\".\"{_configuration["ResultLogTable"]}\" ");
                query.Append($"WHERE \"TelegramId\" = {telegramId};");

                await connection.OpenAsync();
                await connection.ExecuteAsync(query.ToString());
                await connection.CloseAsync();
                query.Clear();
            }
        }

        public async Task<List<ReplyLog>> GetReplyLogAsync(long telegramId)
        {
            using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("NpgConnection")))
            {
                await connection.OpenAsync();
                var response = await connection.QueryAsync<ReplyLog>($"SELECT * FROM \"{_configuration["PostgreDefaultSchema"]}\".\"{_configuration["ReportLogTable"]}\" WHERE \"TelegramId\" = {telegramId};");
                await connection.CloseAsync();

                return response.ToList();
            }
        }

        public async Task<List<ResultLog>> GetResultLogAsync(long telegramId)
        {
            using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("NpgConnection")))
            {
                await connection.OpenAsync();
                var response = await connection.QueryAsync<ResultLog>($"SELECT * FROM \"{_configuration["PostgreDefaultSchema"]}\".\"{_configuration["ResultLogTable"]}\" WHERE \"TelegramId\" = {telegramId};");
                await connection.CloseAsync();

                return response.ToList();
            }
        }
    }
}
