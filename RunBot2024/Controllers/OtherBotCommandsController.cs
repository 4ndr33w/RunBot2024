using Deployf.Botf;
using RunBot2024.Models;
using RunBot2024.Models.Enums;
using RunBot2024.Services.Interfaces;
using SQLite;
using System.Text;
using Telegram.Bot.Types;

namespace RunBot2024.Controllers
{
    public class OtherBotCommandsController : BotController
    {
        readonly TableQuery<Models.User> _users;
        readonly SQLiteConnection _sqLiteConnection;
        readonly ILogger<OtherBotCommandsController> _logger;
        readonly BotfOptions _options;
        readonly IConfiguration _configuration;
        readonly IRivalService _rivalService;
        private readonly MessageSender _messageSender;
        private readonly ILogService _logService;

        private List<RivalModel> _rivalList;

        public OtherBotCommandsController(TableQuery<Models.User> users, SQLiteConnection sqLiteConnection, ILogger<OtherBotCommandsController> logger, BotfOptions options, IConfiguration configuration, IRivalService rivalService, MessageSender messageSender, ILogService logService)
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

        #region Связь с администратором бота
        [Action("/reply", "Связаться с администратором бота")]
        public async Task Reply()
        {
            var users = _users.ToList();
            List<Models.User> admins = users.Where(u => u.Role.ToString() == UserRole.admin.ToString()).ToList();

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
            await ReportLogSaveMethod(logMessage.ToString(), FromId, string.Empty, name);
            //ReplyLog report = new ReplyLog();
            //report.ReplyMessage = logMessage.ToString();
            //report.TelegramId = FromId;
            //report.LastUpdated = DateTime.UtcNow;

            //await _logService.CreateReplyLogAsync(report);
        }
        #endregion


        #region отправка сообщений конкретному участнику
        [Action("/СообщУчастнику")]
        [Authorize("admin")]
        public async Task SendMessageToRival()
        {
            Push("Отправить сообщение участнику.\nПродолжить?");

            RowButton("Да, отправить сообщение участнику", Q(ChooseRivalToSendMessage));
            RowButton("Отмена", Q(Cancel));
        }

        [Action]
        private async Task ChooseRivalToSendMessage()
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
        private async Task SendMessageToSelectedRivalAsync(long telegramId)
        {
            var admins = _users.ToList().Where(u => u.Role.ToString() == UserRole.admin.ToString());
            var adminWhoTakeMessage = admins.First(c => c.Id == FromId);

            var mainAdmin = new Models.User();
            mainAdmin.FullName = "Я";
            mainAdmin.Id = Convert.ToInt64(_configuration["AdminTelegramId"]);
            var rivalList = await _rivalService.GetAllRivalsAsync();
            var selectedRival = rivalList.FirstOrDefault(c => c.TelegramId == telegramId);

            try
            {
                await Send($"Введите сообщение, которое хотите отправить участнику {selectedRival.Name}:");

                var message = await AwaitText();

                var msg = new MessageBuilder()
                    .SetChatId(selectedRival.TelegramId)
                    .Push("Сообщение от администратора:\n" + message);

                await _messageSender.Send(msg);

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

                await ReportLogSaveMethod(message, selectedRival.TelegramId, adminWhoTakeMessage.FullName, selectedRival.Name);
            }
            catch (Exception e)
            {
                await SaveErrorLogMethod(e, FromId, adminWhoTakeMessage.FullName, selectedRival.Name);
                //_logger.LogError(e, "Unhandled exception");
                //if (Context.Update.Type == Telegram.Bot.Types.Enums.UpdateType.CallbackQuery)
                //{
                //    await AnswerCallback($"Error:\n{e}");
                //}
                //else if (Context.Update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
                //{
                //    Push($"Error");
                //}

                //ErrorLog errorLog = new ErrorLog();
                //errorLog.ErrorMessage = e.ToString() + $"\nОшибка при отправле собщения участнику \n{selectedRival.Name} от {adminWhoTakeMessage.FullName}";
                //errorLog.TelegramId = FromId;
                //errorLog.LastUpdated = DateTime.UtcNow;

                //await _logService.CreateErrorLogAsync(errorLog);
            }
        }

        #endregion


        #region отправка сообщения всем участникам

        [Authorize("admin")]
        [Action("/СообщВсем")]
        public async Task SendMessageToAllRivals()
        {
            Push("Отправить сообщение всем участникам.\nПродолжить?");

            RowButton("Да, продолжить", Q(CompilingMessageToAllRivals));
            RowButton("отмена", Q(Cancel));
        }

        [Action]
        private async Task CompilingMessageToAllRivals()
        {
            await Send($"Введите сообщение, которое хотите отправить всем участникам:");
            var message = await AwaitText();

            var rivalList = await _rivalService.GetAllRivalsAsync();

            var admins = _users.ToList().Where(u => u.Role.ToString() == UserRole.admin.ToString());
            var adminWhoSendMessage = admins.First(c => c.Id == FromId);

            try
            {
                foreach (var rival in rivalList)
                {
                    var msg = new MessageBuilder()
                   .SetChatId(rival.TelegramId)
                   .Push("Сообщение всем участникам:\n" + message);

                    await _messageSender.Send(msg);
                }

                await ReportLogSaveMethod(message, FromId, adminWhoSendMessage.FullName);
            }
            catch (Exception e)
            {
                await SaveErrorLogMethod(e, FromId, adminWhoSendMessage.FullName);

                //_logger.LogError(e, "Unhandled exception");
                //if (Context.Update.Type == Telegram.Bot.Types.Enums.UpdateType.CallbackQuery)
                //{
                //    await AnswerCallback($"Error:\n{e}");
                //}
                //else if (Context.Update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
                //{
                //    Push($"Error");
                //}

                //ErrorLog errorLog = new ErrorLog();
                //errorLog.ErrorMessage = e.ToString() + $"\nОшибка при отправле собщения всем участникам \nот {adminWhoSendMessage.FullName}";
                //errorLog.TelegramId = FromId;
                //errorLog.LastUpdated = DateTime.UtcNow;

                //await _logService.CreateErrorLogAsync(errorLog);
            }
        }

        #endregion

        [Action]
        public async Task Cancel()
        {
            await Send("Отмена");
        }

        #region показать список юзеров с ролями

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

        #endregion

        #region Сохранение лога ошибок

        [Action]
        public async Task SaveErrorLogMethod(Exception e, long fromId, string adminName, string selectedRivalName = null)
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
            var firstHalfOfMessage = "";
            if (selectedRivalName == null)
            {
                firstHalfOfMessage = $"\nОшибка при отправке собщения всем участникам \nот ";
            }
            else
            {
                firstHalfOfMessage = $"\nОшибка при отправке собщения участнику {selectedRivalName} \nот ";
            }

            ErrorLog errorLog = new ErrorLog();
            errorLog.ErrorMessage = e.ToString() + firstHalfOfMessage +  $"{adminName}";
            errorLog.TelegramId = fromId;
            errorLog.LastUpdated = DateTime.UtcNow;

            await _logService.CreateErrorLogAsync(errorLog);
        }

        #endregion

        #region Сохранение лога сообщений

        [Action]
        public async Task ReportLogSaveMethod(string message, long fromId, string adminName, string rivalName = null )
        {
            var firstHalfOfMessage = "";
            if (rivalName == null)
            {
                firstHalfOfMessage = $"(admin) {adminName} to All Rivals: ";
            }
            else
            {
                firstHalfOfMessage = $"(admin) {adminName} to {rivalName}: ";
            }

            if (adminName == string.Empty) // ToDo
            {

            }

            ReplyLog report = new ReplyLog();
            report.ReplyMessage = firstHalfOfMessage + message;
            report.TelegramId = fromId;
            report.LastUpdated = DateTime.UtcNow;

            await _logService.CreateReplyLogAsync(report);
        }
        #endregion
    }
}
