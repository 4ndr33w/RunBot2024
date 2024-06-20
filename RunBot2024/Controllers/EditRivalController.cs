using Deployf.Botf;
using RunBot2024.Models;
using RunBot2024.Services.Interfaces;
using SQLite;
using System.Threading.Channels;

namespace RunBot2024.Controllers
{
    public class EditRivalController : BotController
    {
        readonly TableQuery<Models.User> _users;
        readonly SQLiteConnection _sqLiteConnection;
        readonly ILogger<EditRivalController> _logger;
        readonly BotfOptions _options;
        readonly IConfiguration _configuration;
        readonly IRivalService _rivalService;
        private readonly MessageSender _messageSender;
        private readonly ILogService _logService;

        //private List<RivalModel> _rivalList;

        public EditRivalController(TableQuery<Models.User> users, SQLiteConnection sqLiteConnection, ILogger<EditRivalController> logger, BotfOptions options, IConfiguration configuration, IRivalService rivalService, MessageSender messageSender, ILogService logService)
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


        #region Редактирование данных участника

        //[Authorize("admin")]
        [Action("/Редактир"), Authorize("admin")]
        public async Task Edit()
        {
            PushL("Редактировать данные профиля участника. Продолжить?");

            RowButton("Да, продолжить", Q(ChooseRivalToEdit));
            RowButton("Отмена", Q(Cancel));
        }

        //[Authorize("admin")]
        [Action]
        private async Task ChooseRivalToEdit()
        {
            try
            {
                var rivalList = await _rivalService.GetAllRivalsAsync();
                var rivalCount = rivalList.Count;

                await Send("Введите имя участника, данные которого требуется отредактировать:");

                string searchRivalName = await AwaitText();

                var selectedRival = rivalList.Where(r => r.Name.ToLower().Contains(searchRivalName.ToLower())).ToList();
                if (selectedRival.Count == 1)
                {
                    await Send($"Редактировать данные участника {selectedRival[0].Name} - {selectedRival[0].Company}");
                    await EditRivalData(selectedRival[0].TelegramId);
                }
                else if (selectedRival.Count > 1)
                {
                    PushL("Найдены следующие совпадения:");

                    foreach (var r in selectedRival)
                    {
                        var currentRival = $"{r.Name} - {r.Company} - {r.TotalResult}";

                        var qRival = Q(EditRivalData, r.TelegramId);

                        RowButton(currentRival, qRival);
                    }
                }
                else
                {
                    PushL("Не найдено ни одного совпадения");
                }
            }
            catch (Exception e)
            {
                PushL("Возникла ошибка");

                await SaveErrorLogMethod(e, FromId, null, null);
            }
        }
        private async Task EditRivalData(long telegramId)
        {
            PushL("Какие данные участника отредактировать?");

            string changeNameStr = "Изменить имя";
            string changeCompanyStr = "Изменить команду";
            string changeResultStr = "Изменить результат";

            var qChangeName = Q(EditRivalName, telegramId);
            var qChangeCompany = Q(EditRivalCompany, telegramId);
            var qChangeResult = Q(EditRivalResult, telegramId);

            RowButton(changeNameStr, qChangeName);
            RowButton(changeCompanyStr, qChangeCompany);
            RowButton(changeResultStr, qChangeResult);
        }

        //
        // ToDo Block
        //---------------------------------------------
        [Action]
        private async Task EditRivalName(long telegramId) // ToDo
        {
            var selectedRival = await _rivalService.GetRivalByIdAsync(telegramId);
            try
            {
                await Send($"Введите новое имя для участника {selectedRival.Name}:");
                string oldName = selectedRival.Name;

                string newName = await AwaitText();

                selectedRival.Name = newName;

                var result = await _rivalService.UpdateRivalAsync(selectedRival);
                if (result)
                {
                    await Send($"Имя участника {oldName} было изменено на {selectedRival.Name}");
                }
                else
                {
                    throw new Exception($"ошибка при иозменении имени {selectedRival.Name}");
                }
            }
            catch (Exception e)
            {

                await SaveErrorLogMethod(e, telegramId, Context.GetUserFullName(), selectedRival.Name);
            }
        }

        [Action]
        private async Task EditRivalCompany(long telegramId) // ToDo
        {

        }

        [Action]
        private async Task EditRivalResult(long telegramId) // ToDo
        {

        }
        //---------------------------------------------
        // ToDo Block
        //

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
            errorLog.ErrorMessage = e.ToString() + firstHalfOfMessage + $"{adminName}";
            errorLog.TelegramId = fromId;
            errorLog.LastUpdated = DateTime.UtcNow;

            await _logService.CreateErrorLogAsync(errorLog);
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
