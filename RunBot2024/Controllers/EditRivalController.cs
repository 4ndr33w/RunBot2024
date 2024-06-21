using Deployf.Botf;
using RunBot2024.Models;
using RunBot2024.Services.Interfaces;
using SQLite;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types;
using Telegram.Bot;
using System.Text.RegularExpressions;

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
        readonly ICompanyService _companyService;
        private readonly MessageSender _messageSender;
        private readonly ILogService _logService;

        public EditRivalController
            (
            TableQuery<Models.User> users, 
            SQLiteConnection sqLiteConnection, 
            ILogger<EditRivalController> logger, 
            BotfOptions options, 
            IConfiguration configuration, 
            IRivalService rivalService, 
            MessageSender messageSender, 
            ILogService logService, 
            ICompanyService companyService
            )
        {
            _users = users;
            _sqLiteConnection = sqLiteConnection;
            _logger = logger;
            _options = options;
            _configuration = configuration;
            _rivalService = rivalService;
            _messageSender = messageSender;
            _logService = logService;
            _companyService = companyService;
        }

        #region Редактирование данных участника

        [Action("/Редактир"), Authorize("admin")]
        public async Task Edit()
        {
            PushL("Редактировать данные профиля участника. Продолжить?");

            RowButton("Да, продолжить", Q(ChooseRivalToEdit));
            RowButton("Отмена", Q(Cancel));
        }

        [Action]
        private async Task ChooseRivalToEdit()
        {
            try
            {
                var rivalList = await _rivalService.GetAllRivalsAsync();
                var rivalCount = rivalList.Count;

                await Send("Введите имя участника, данные которого требуется отредактировать:");

                string searchRivalName = await AwaitText();

                var selectedRivals = rivalList.Where(r => r.Name.ToLower().Contains(searchRivalName.ToLower())).ToList();
                if (selectedRivals.Count == 1)
                {
                    await Send($"Редактировать данные участника {selectedRivals[0].Name} - {selectedRivals[0].Company}");
                    await EditRivalData(selectedRivals[0]);
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
                        text: $"Удаление данных профиля участника {selectedRival.Name}:",
                        messageId: sentMessage.MessageId,
                        replyMarkup: null
                        );

                    await EditRivalData(selectedRival);
                }
                else
                {
                    await Send("Не найдено ни одного совпадения");
                }
            }
            catch (Exception e)
            {
                PushL("Возникла ошибка");

                await SaveErrorLogMethod(e, FromId, null, null);
            }
        }

        [Action]
        private async Task EditRivalData(RivalModel rival)
        {
            // --------------------------------------------------------------------
            // Снова при использовании Button(Q) кнопки сползали вверх по чату над текстом. Снова пришлось использовать
            // InlineKeyboardMarkup как в контроллере регистрации
            // ----------------------------------------------------------------------

            InlineKeyboardButton[][] buttons = new InlineKeyboardButton[4][];

            var changeNameButton = InlineKeyboardButton.WithCallbackData("Изменить имя", "changeName");
            var changeCompanyButton = InlineKeyboardButton.WithCallbackData("Изменить предприятие", "changeCompany");
            var changeResultButton = InlineKeyboardButton.WithCallbackData("Изменить результат", "changeResult");
            var cancelButton = InlineKeyboardButton.WithCallbackData("Отмена", "cancel");

            buttons[0] = new InlineKeyboardButton[1] { changeNameButton };
            buttons[1] = new InlineKeyboardButton[1] { changeCompanyButton };
            buttons[2] = new InlineKeyboardButton[1] { changeResultButton };
            buttons[3] = new InlineKeyboardButton[1] { cancelButton };

            InlineKeyboardMarkup markup = new InlineKeyboardMarkup(buttons);

            Message sentMessage = await Client
                .SendTextMessageAsync(FromId, "Какие данные участника отредактировать?", ParseMode.Html, default, default, default, default, 0, true, markup);

            var callback = await AwaitQuery();

            await Client.EditMessageTextAsync(
                chatId: FromId,
                text: $"Режим редактирования",
                messageId: sentMessage.MessageId,
                replyMarkup: null
                );

            if (callback.ToString() == "changeName")
            {
                await EditRivalName(rival);
            }
            if (callback.ToString() == "changeCompany")
            {
                await EditRivalCompany(rival);
            }
            if (callback.ToString() == "changeResult")
            {
                await EditRivalResult(rival);
            }
            if (callback.ToString() == "cancel")
            {
                await Cancel();
            }
        }

        #region Редактирование имени

        [Action]
        private async Task EditRivalName(RivalModel rival) 
        {
            try
            {
                await Send($"Введите новое имя для участника {rival.Name}:");
                string oldName = rival.Name;

                string newName = await AwaitText();

                rival.Name = newName;

                var result = await _rivalService.UpdateRivalAsync(rival);
                if (result)
                {
                    await Send($"Имя участника {oldName} было изменено на {rival.Name}");
                }
                else
                {
                    throw new Exception($"ошибка при иозменении имени {rival.Name}");
                }
            }
            catch (Exception e)
            {

                await SaveErrorLogMethod(e, rival.TelegramId, Context.GetUserFullName(), rival.Name);
            }
        }

        #endregion

        [Action]
        private async Task EditRivalCompany(RivalModel rival) 
        {
            try
            {
                string oldCompanyName = new string(rival.Company);

                #region Выбор региона

                var regionList = await _companyService.GetRegionListAsync();
                InlineKeyboardButton[][] regionButtons = new InlineKeyboardButton[regionList.Count][];

                int i = 0;

                foreach (var eachRegion in regionList)
                {
                    var _currentRegionButton = InlineKeyboardButton.WithCallbackData(eachRegion.RegionName, $"{eachRegion.RegionId}");

                    regionButtons[i] = new InlineKeyboardButton[1] { _currentRegionButton };
                    i++;
                }
                InlineKeyboardMarkup regionMarkup = new InlineKeyboardMarkup(regionButtons);

                Message regionMessage = await Client
                    .SendTextMessageAsync(FromId, $"Выберите регион, в котором находится требуемое предприятие:", ParseMode.Html, default, default, default, default, 0, true, regionMarkup);

                var regionCallback = await AwaitQuery();

                Region selectedRegion = regionList.First(r => r.RegionId == Convert.ToInt32(regionCallback));

                string region = selectedRegion.RegionName;

                await Client.EditMessageTextAsync(
                chatId: FromId,
                    text: $"{region}",
                    messageId: regionMessage.MessageId,
                    replyMarkup: null
                    );

                #endregion

                #region выбор города

                var cityList = await _companyService.GetCityListAsync();

                List<City> selectedRegionCityList = new List<City>(cityList.Where(c => c.RegionId == selectedRegion.RegionId).ToList());

                InlineKeyboardButton[][] cityButtons = new InlineKeyboardButton[selectedRegionCityList.Count][];

                int y = 0;

                foreach (var city in selectedRegionCityList)
                {
                    var _currentCityButton = InlineKeyboardButton.WithCallbackData(city.CityName, $"{city.CityId}");
                    cityButtons[y] = new InlineKeyboardButton[1] { _currentCityButton };
                    y++;
                }
                InlineKeyboardMarkup cityMarkup = new InlineKeyboardMarkup(cityButtons);

                Message cityMessage = await Client
                    .SendTextMessageAsync(FromId, $"Выберите город, в котором находится требуемое предприятие:", ParseMode.Html, default, default, default, default, 0, true, cityMarkup);

                var cityCallback = await AwaitQuery();

                City selectedCity = cityList.First(c => c.CityId == Convert.ToInt32(cityCallback));

                await Client.EditMessageTextAsync(
                chatId: FromId,
                    text: $"{selectedCity.CityName}",
                    messageId: cityMessage.MessageId,
                    replyMarkup: null
                    );

                #endregion

                #region выбор предприятия

                var companyList = await _companyService.GetCompanyListAsync();

                List<Company> selectedCityCompanies = new List<Company>(companyList.Where(c => c.CityId == selectedCity.CityId).ToList());
                InlineKeyboardButton[][] companyButtons = new InlineKeyboardButton[selectedCityCompanies.Count][];

                int j = 0;

                foreach (var company in selectedCityCompanies)
                {
                    var _currentCompanyButton = InlineKeyboardButton.WithCallbackData(company.CompanyName, $"{company.CompanyId}");
                    companyButtons[j] = new InlineKeyboardButton[1] { _currentCompanyButton };
                    j++;
                }
                InlineKeyboardMarkup companyMarkup = new InlineKeyboardMarkup(companyButtons);

                Message companyMessage = await Client
                    .SendTextMessageAsync(FromId, $"Выберите требуемое предприятие:", ParseMode.Html, default, default, default, default, 0, true, companyMarkup);

                var companyCallback = await AwaitQuery();

                Company selectedCompany = companyList.First(c => c.CompanyId == Convert.ToInt32(companyCallback));
                rival.Company = selectedCompany.CompanyName;

                await Client.EditMessageTextAsync(
                    chatId: FromId,
                    text: $"Смена предприятия с {oldCompanyName} на {selectedCompany.CompanyName}\nдля участника: {rival.Name}",
                    messageId: companyMessage.MessageId,
                    replyMarkup: null
                    );

                #endregion

                var result = await _rivalService.UpdateRivalAsync(rival);
                if (result)
                {
                    await Send($"Предприятие участника {rival.Name} изменено на {rival.Company}");
                }
                else
                {
                    throw new Exception($"ошибка при иозменении имени {rival.Name}");
                }
            }
            catch (Exception e)
            {
                await SaveErrorLogMethod(e, rival.TelegramId, Context.GetUserFullName(), rival.Name);
            }
        }

        #region Редактирование результата бега
        [Action]
        private async Task EditRivalResult(RivalModel rival)
        {
            try
            {
                await Send($"Введите новый результат для участника {rival.Name}:");
                var oldResult = rival.TotalResult;

                string newStringResult = await AwaitText();

                #region парсинг введённого результата в дабл

                double currentResult = 0;

                //if (newStringResult.IndexOf(',') > 0)
                //{
                //    newStringResult.Replace(',', '.');
                //}
                if (newStringResult.IndexOf('.') > -1)
                {
                    newStringResult = newStringResult.Replace('.', ',');
                }

                if (newStringResult.IndexOf('+') > -1)
                {
                    newStringResult = newStringResult.Remove(newStringResult.IndexOf('+'), 1);
                }
                string regularPattern = @"[0-9]{0,5}\,?[0-9]{0,5}";
                var _regex = new Regex(regularPattern);
                var filteredString = _regex.Match(newStringResult).Value;
                newStringResult = filteredString;
                currentResult = Convert.ToDouble(newStringResult);

                #endregion

                rival.TotalResult = currentResult;

                var result = await _rivalService.UpdateRivalAsync(rival);
                if (result)
                {
                    await Send($"Результат участника {rival.Name} был изменён с {oldResult} на {rival.TotalResult}");
                }
                else
                {
                    throw new Exception($"ошибка при иозменении результата бега {rival.Name}");
                }
            }
            catch (Exception e)
            {

                await SaveErrorLogMethod(e, rival.TelegramId, Context.GetUserFullName(), rival.Name);
            }
        }

        #endregion

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
