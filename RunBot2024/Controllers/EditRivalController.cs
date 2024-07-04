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
                    await ChooseParameterToEdit(selectedRivals[0]);
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

                    await ChooseParameterToEdit(selectedRival);
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
        private async Task ChooseParameterToEdit(RivalModel rival)
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
                    throw new Exception($"ошибка при изменении имени {rival.Name}");
                }
            }
            catch (Exception e)
            {

                await SaveErrorLogMethod(e, rival.TelegramId, Context.GetUserFullName(), rival.Name);
            }
        }

        #endregion

        #region Редактирование предприятия

        [Action]
        private async Task EditRivalCompany(RivalModel rival) 
        {
            try
            {
                string oldCompanyName = new string(rival.Company);

                Region newRegion = await ChooseNewRegion();

                City newCity = await ChooseNewCity(newRegion);

                Company newCompany = await ChooseNewCompany(newCity);

                rival.Company = newCompany.Name;


                var result = await _rivalService.UpdateRivalAsync(rival);
                if (result)
                {
                    await Send($"Предприятие участника {rival.Name} изменено:\nс {oldCompanyName} на {rival.Company}");
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

        [Action]  // ToDo Throw in Catch
        private async Task<Region> ChooseNewRegion()
        {
            Region newRegion = new Region();
            try
            {
                var regionList = await _companyService.GetRegionListAsync();

                InlineKeyboardMarkup regionMarkup = GetKeyboardMarkupFromCollection(regionList);

                Message regionMessage = await Client
                    .SendTextMessageAsync(FromId, $"Выберите регион, в котором находится требуемое предприятие:", ParseMode.Html, default, default, default, default, 0, true, regionMarkup);

                var regionCallback = await AwaitQuery();

                newRegion = regionList.First(r => r.Id == Convert.ToInt32(regionCallback));

                string regionName = newRegion.Name;

                await Client.EditMessageTextAsync(
                chatId: FromId,
                    text: $"{regionName}",
                    messageId: regionMessage.MessageId,
                    replyMarkup: null
                    );
            }
            catch (Exception) // ToDo
            {

                throw;
            }
            return newRegion;
        }

        [Action] // ToDo Throw in Catch
        private async Task<City> ChooseNewCity(Region newRegion)
        {
            City newCity = new City();

            try
            {
                var cityList = await _companyService.GetCityListAsync();

                List<City> citiesInSelectedRegion = new List<City>(cityList.Where(c => c.RegionId == newRegion.Id).ToList());

                InlineKeyboardMarkup cityMarkup = GetKeyboardMarkupFromCollection(citiesInSelectedRegion);

                Message cityMessage = await Client
                    .SendTextMessageAsync(FromId, $"Выберите город, в котором находится требуемое предприятие:", ParseMode.Html, default, default, default, default, 0, true, cityMarkup);

                var cityCallback = await AwaitQuery();

                newCity = cityList.First(c => c.Id == Convert.ToInt32(cityCallback));

                await Client.EditMessageTextAsync(
                chatId: FromId,
                    text: $"{newCity.Name}",
                    messageId: cityMessage.MessageId,
                    replyMarkup: null
                    );
            }
            catch (Exception)
            {

                throw;
            }

            return newCity;
        }

        [Action] // ToDo Throw in Catch
        private async Task<Company> ChooseNewCompany(City newCity)
        {
            Company newCompany = new Company();

            var companyList = await _companyService.GetCompanyListAsync();
            List<Company> companiesInSelectedCity = new List<Company>(companyList.Where(c => c.CityId == newCity.Id).ToList());

            InlineKeyboardMarkup companyMarkup = GetKeyboardMarkupFromCollection(companiesInSelectedCity);

            Message companyMessage = await Client
                   .SendTextMessageAsync(FromId, $"Выберите требуемое предприятие:", ParseMode.Html, default, default, default, default, 0, true, companyMarkup);

            var companyCallback = await AwaitQuery();

            newCompany = companyList.First(c => c.Id == Convert.ToInt32(companyCallback));

            await Client.EditMessageTextAsync(
                chatId: FromId,
                text: $"{newCompany.Name}",
                messageId: companyMessage.MessageId,
                replyMarkup: null
                );

            return newCompany;
        }

        //[Action]
        private InlineKeyboardMarkup GetKeyboardMarkupFromCollection<T>(List<T> collection)
        {
            InlineKeyboardButton[][] inlineButtons = new InlineKeyboardButton[collection.Count][];

            Type templateType = typeof(T);
            int i = 0;
            foreach (T t in collection)
            {
                switch(t)
                {
                    case Region region:
                        inlineButtons[i] = new InlineKeyboardButton[1] { InlineKeyboardButton.WithCallbackData(region.Name, $"{region.Id}") };
                        break;

                    case City city:
                        inlineButtons[i] = new InlineKeyboardButton[1] { InlineKeyboardButton.WithCallbackData(city.Name, $"{city.Id}") };
                        break;

                    case Company company:
                        inlineButtons[i] = new InlineKeyboardButton[1] { InlineKeyboardButton.WithCallbackData(company.Name, $"{company.Id}") };
                        break;
                }
                i++;
            }
            return new InlineKeyboardMarkup(inlineButtons);
        }

        #endregion

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
