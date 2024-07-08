using Dapper;
using Npgsql;
using RunBot2024.Models;
using RunBot2024.Services.Interfaces;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;

namespace RunBot2024.Services
{
    public class LogService : ILogService, ILogSaver
    {
        private readonly IConfiguration _configuration;

        private TelegramBotClientOptions _botClientOptions;
        private TelegramBotClient _botClient;
        public LogService(IConfiguration configuration)
        {
            _configuration = configuration;

            _botClientOptions = new TelegramBotClientOptions(_configuration["token"]);
            _botClient = new TelegramBotClient(_botClientOptions);
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

        public async Task SaveErrorLog(Exception e, long fromId, string adminName, string rivalName = null)
        {
            string message = "При выполнении операции возникла ошибка";
            await _botClient.SendTextMessageAsync(fromId, message, ParseMode.Html);

            StringBuilder errorLogMessage = new StringBuilder();

            switch (adminName)
            {
                case null:
                    {
                        if (rivalName != null)
                        {
                            errorLogMessage.Append($"admin: {adminName} - ");
                            errorLogMessage.Append($"Ошибка при отправке собщения участнику {rivalName}: ");
                        }
                        else
                        {
                            errorLogMessage.Append($"admin: {adminName} - ");
                            errorLogMessage.Append($"\nОшибка при отправке собщения всем участникам");
                        }
                        break;
                    }
                default:
                    {
                        if (rivalName != null)
                        {
                            errorLogMessage.Append($"У участника {rivalName} возникла ошибка: ");
                        }
                        else
                        {
                            errorLogMessage.Append($"Ошибка: ");
                        }
                        break;
                    }
            }

           

            errorLogMessage.Append(e.ToString());

            ErrorLog errorLog = new ErrorLog();
            errorLog.ErrorMessage = errorLogMessage.ToString();
            errorLog.TelegramId = fromId;
            errorLog.LastUpdated = DateTime.UtcNow;

            await CreateErrorLogAsync(errorLog);
        }
    }
}
