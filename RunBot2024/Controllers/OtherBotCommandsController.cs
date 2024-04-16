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
                name = Context.GetUserFullName == null ? FromId.ToString() : Context.GetUserFullName();
                msgSb.AppendLine($"Сообщение от {name} - {FromId}, Участник не зарегистрирован:\n");
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

        
        [Action("/sendTo")]
        [Authorize("admin")]
        public async Task Send()
        {
            Push("Отправить сообщение участнику.\nПродолжить?");

            RowButton("Нет, не отправлять", Q(Cancel));
            RowButton("Да, отправить сообщение участнику", Q(SendMessageToRival));
        }

        [Action]
        public async Task SendMessageToRival()
        {
            _rivalList = await _rivalService.GetAllRivalsAsync();

            await Send("Кому отправить сообщение?\n");

            var rivalName = await AwaitText();

            var selectedRivals = _rivalList.Where(r => r.Name.ToLower().Contains(rivalName.ToLower())).ToList();

            if (selectedRivals.Count == 1)
            {
                await SendMessageToSelectedRivalAsync(selectedRivals[0].TelegramId);
            }
            else if (selectedRivals.Count > 1)
            {
                PushL("Найдены следующие совпадения:");

                foreach (var rival in selectedRivals)
                {
                    string currentRival = $"{rival.Name} - {rival.Company}";

                    var qFunc = Q(SendMessageToSelectedRivalAsync, rival.TelegramId);

                    RowButton(currentRival, qFunc);
                }
            }

            else
            {
                PushL("Не найдено ни одного совпадения по имени");
            }
        }

        [Action]
        public async Task SendMessageToSelectedRivalAsync(long telegramId)
        {
            var admins = _users.ToList().Where(u => u.Role.ToString() == UserRole.admin.ToString());

            var mainAdmin = new User();
            mainAdmin.FullName = "Я";
            mainAdmin.Id = Convert.ToInt64(_configuration["AdminTelegramId"]);

            try
            {
                var rivalList = await _rivalService.GetAllRivalsAsync();
                var selectedRival = rivalList.FirstOrDefault(c => c.TelegramId == telegramId);
                await Send($"Введите сообщение, которое хотите отправить участнику {selectedRival.Name}:");

                var message = await AwaitText();

                var msg = new MessageBuilder()
                    .SetChatId(selectedRival.TelegramId)
                    .Push("Сообщение от администратора:\n" + message);

                await _messageSender.Send(msg);

                var adminWhoTakeMessage = admins.First(c => c.Id == FromId);

                foreach (var admin in admins)
                {
                    if (admin.Id != FromId)
                    {
                        
                        var adminAnswer = new MessageBuilder()
                         .SetChatId(admin.Id)
                         .Push($"(admin) {adminWhoTakeMessage.FullName} to {selectedRival.Name}: " + message);

                        await _messageSender.Send(adminAnswer);
                    }
                }
                if (FromId != mainAdmin.Id)
                {
                    var mainAdminAnswer = new MessageBuilder()
                 .SetChatId(mainAdmin.Id)
                 .Push($"(admin) {adminWhoTakeMessage.FullName} to {selectedRival.Name}: " + message);

                    await _messageSender.Send(mainAdminAnswer);
                }

                ReplyLog report = new ReplyLog();
                report.ReplyMessage = $"(admin) {adminWhoTakeMessage.FullName} to {selectedRival.Name}: " + message;
                report.TelegramId = selectedRival.TelegramId;
                report.LastUpdated = DateTime.UtcNow;

                await _logService.CreateReplyLogAsync(report);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unhandled exception");
                if (Context.Update.Type == Telegram.Bot.Types.Enums.UpdateType.CallbackQuery)
                {
                    await AnswerCallback($"Error:\n{e}");
                }
                else if (Context.Update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
                {
                    Push($"Error");
                }

                ErrorLog errorLog = new ErrorLog();
                errorLog.ErrorMessage = e.ToString() + "\nОшибка при отправле собщения участнику";
                errorLog.TelegramId = FromId;
                errorLog.LastUpdated = DateTime.UtcNow;

                await _logService.CreateErrorLogAsync(errorLog);
            }
        }

        [Action]
        public async Task Cancel()
        {
            await Send("Отмена");
        }

        [Action("/getUsers")]
        [Authorize("admin")]
        public async Task GetUsers()
        {
            PushL($"кол-во юзеров: {_users.Count()}\n------------------------");
            foreach (var user in _users) 
            {
                PushL($"{user.FullName} - {user.Role.ToString()}");
            }
        }

    }
}
