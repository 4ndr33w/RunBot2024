using Deployf.Botf;
using Microsoft.AspNetCore.Mvc;
using RunBot2024.Models;
using RunBot2024.Models.Enums;
using RunBot2024.Services.Interfaces;
using SQLite;
using System.Text;

namespace RunBot2024.Controllers
{
    public class OtherBotCommandsController : BotController
    {
        readonly TableQuery<User> _users;
        readonly SQLiteConnection _sqLiteConnection;
        readonly ILogger<OtherBotCommandsController> _logger;
        readonly BotfOptions _options;
        readonly IConfiguration _configuration;
        readonly IRivalService _rivalService;
        private readonly MessageSender _messageSender;
        private readonly ILogService _logService;

        private List<RivalModel> _rivalList;

        public OtherBotCommandsController(TableQuery<User> users, SQLiteConnection sqLiteConnection, ILogger<OtherBotCommandsController> logger, BotfOptions options, IConfiguration configuration, IRivalService rivalService, MessageSender messageSender, ILogService logService)
        {
            _users = users;
            _sqLiteConnection = sqLiteConnection;
            _logger = logger;
            _options = options;
            _configuration = configuration;
            _rivalService = rivalService;
            _messageSender = messageSender;
            _logService = logService;
        }

        [Action("/reply", "Связаться с администратором бота")]
        public async Task Reply()
        {
            var users = _users.ToList();
            List<User> admins = users.Where(u => u.Role.ToString() == UserRole.admin.ToString()).ToList();

            await Send("Связь с администратором бота.");
            await Send("Введите сообщение:");

            StringBuilder msgSb = new StringBuilder();
            StringBuilder logMessage = new StringBuilder();

            var message = await AwaitText();

            var rivals = await _rivalService.GetAllRivalsAsync();
            var existingRival = rivals.ToList().Where(r => r.TelegramId == FromId).FirstOrDefault();
            string name = "";

            if (existingRival != null || existingRival != default)
            {
                name = existingRival.Name;
                msgSb.AppendLine($"Сообщение от {FromId}, {existingRival.Name}, {existingRival.Company}:\n");
            }
            else
            {
                name = Context.GetUserFullName == null ? FromId.ToString() : $"{FromId} - {Context.GetUserFullName()}";
                msgSb.AppendLine($"Сообщение от {name}, Участник не зарегистрирован:\n");
            }
            logMessage.Append($"{name}: {message}");
            msgSb.AppendLine(message);

            foreach (var admin in admins)
            {
                var msgBuilder = new MessageBuilder()
                    .SetChatId(admin.Id)
                    .Push(msgSb.ToString());
                await _messageSender.Send(msgBuilder);
            }

            ReplyLog report = new ReplyLog();
            report.ReplyMessage = logMessage.ToString();
            report.TelegramId = FromId;
            report.LastUpdated = DateTime.UtcNow;

            await _logService.CreateReplyLogAsync(report);
        }
    }
}
