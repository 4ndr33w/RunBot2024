using Deployf.Botf;
using RunBot2024.Models;
using RunBot2024.Models.Enums;
using RunBot2024.Services.Interfaces;
using SQLite;
using System.IO;
using System.Text;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;

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
            await Send("Отправить сообщение администратору бота. Продолжить?");

            RowButton("Да, продолжить", Q(ReplyMessage));
            RowButton("Отмена", Q(Cancel));
        }

        [Action]
        private async Task ReplyMessage()
        {
            var users = _users.ToList();
            List<Models.User> admins = users.Where(u => u.Role.ToString() == UserRole.admin.ToString()).ToList();

            await Send("Введите сообщение:");

            StringBuilder msgSb = new StringBuilder();
            StringBuilder logMessage = new StringBuilder();

            var message = await AwaitText();

            //var rivals = await _rivalService.GetAllRivalsAsync();
            var existingRival = await _rivalService.GetRivalByIdAsync(FromId); //rivals.ToList().Where(r => r.TelegramId == FromId).FirstOrDefault();
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
            var logMessageShort = $"{message}";
            logMessage.Append($"{name}: {message}");
            msgSb.AppendLine(message);

            foreach (var admin in admins)
            {
                var msgBuilder = new MessageBuilder()
                    .SetChatId(admin.Id)
                    .Push(msgSb.ToString());
                await _messageSender.Send(msgBuilder);
            }
            var userData = $"{name} - {existingRival.Company}";
            await ReportLogSaveMethod(logMessageShort, FromId, null, userData);
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
                // --------------------------------------------------------------------
                // Снова при использовании Button(Q) кнопки сползали вверх по чату над текстом. Снова пришлось использовать
                // InlineKeyboardMarkup как в контроллере регистрации
                // ----------------------------------------------------------------------
                InlineKeyboardButton[][] buttons = new InlineKeyboardButton[selectedRivals.Count][];

                for (int i = 0; i < selectedRivals.Count; i++)
                {
                    var currentRivalNameButton = InlineKeyboardButton.WithCallbackData(selectedRivals[i].Name, selectedRivals[i].TelegramId.ToString());
                    buttons[i] = new InlineKeyboardButton[1] { currentRivalNameButton };
                }
                InlineKeyboardMarkup markup = new InlineKeyboardMarkup(buttons);

                Message sentMessage = await Client
                    .SendTextMessageAsync(FromId, "Найдены следующие совпадения:", ParseMode.Html, default, default, default, default, 0, true, markup);

                var callback = await AwaitQuery();

                var selectedRival = selectedRivals.First(x => x.TelegramId == Convert.ToInt64(callback));

                await Client.EditMessageTextAsync(
                    chatId: FromId,
                    text: $"Введите сообщение, которое хотите отправить участнику {selectedRival.Name}:",
                    messageId: sentMessage.MessageId,
                    replyMarkup: null
                    );
                await SendMessageToSelectedRivalAsync(selectedRival.TelegramId);
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
            mainAdmin.FullName = _configuration["MainAdminName"];
            mainAdmin.Id = Convert.ToInt64(_configuration["AdminTelegramId"]);

            var selectedRival = await _rivalService.GetRivalByIdAsync(telegramId);//rivalList.FirstOrDefault(c => c.TelegramId == telegramId);

            try
            {
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
                   .Push(message);

                    await _messageSender.Send(msg);
                }

                await ReportLogSaveMethod(message, FromId, adminWhoSendMessage.FullName);
            }
            catch (Exception e)
            {
                await SaveErrorLogMethod(e, FromId, adminWhoSendMessage.FullName);
            }
        }

        #endregion

        #region удаление участника

        [Authorize("admin")]
        [Action("/Удалить")]
        public async Task Delete()
        {
            Push("Удаление данных участника.\nПродолжить?");

            RowButton("Да, продолжить", Q(ChooseRivalToDelete));
            RowButton("Отмена", Q(Cancel));
        }

        [Action]
        private async Task ChooseRivalToDelete()
        {
            await Send("Кого удалить?");
            var removingName = await AwaitText();

            var rivalList = await _rivalService.GetAllRivalsAsync();
            var matchedRivals = rivalList.Where(x => x.Name.ToLower().Contains(removingName.ToLower())).ToList();

            if (matchedRivals.Count == 1)
            {
                await Send($"Удаляются данные участника {matchedRivals[0].Name} - {matchedRivals[0].Company}");
                await FinishDeleting(matchedRivals[0]);
            }
            else if (matchedRivals.Count > 1)
            {
                PushL("Найдены следующие совпадения:");

                foreach (var rival in matchedRivals)
                {
                    string currentRival = $"{rival.Name} - {rival.Company} - {rival.TotalResult}";

                    var qRival = Q(FinishDeleting, rival);

                    RowButton(currentRival, qRival);
                }
            }
        }

        [Action]
        private async Task FinishDeleting(RivalModel rival)
        {
            try
            {
                var result = await _rivalService.DeleteRivalByIdAsync(rival.TelegramId);

                if (result)
                {
                    await Send($"Участник {rival.Name} - {rival.Company} успешно удалён из списка.");
                }
                else
                {
                    throw new Exception($"ошибка при удалении участника {rival.Name} - {rival.Company}");   
                }
            }
            catch (Exception e)
            {
                var admin = _users.First(x => x.Id == FromId);
                await SaveErrorLogMethod(e, rival.TelegramId, admin.FullName, rival.Name);
            }
        }

        #endregion

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
        public async Task ReportLogSaveMethod(string message, long fromId, string adminName = null, string rivalName = null )
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

            if (adminName == null)
            {
                firstHalfOfMessage = $"Сообщение администратору от {rivalName}: ";
            }

            ReplyLog report = new ReplyLog();
            report.ReplyMessage = firstHalfOfMessage + message;
            report.TelegramId = fromId;
            report.LastUpdated = DateTime.UtcNow;

            await _logService.CreateReplyLogAsync(report);
        }
        #endregion

        #region Cancel Method

        [Action]
        public async Task Cancel()
        {
            await Send("Отмена");
        }
        #endregion
    }
}
