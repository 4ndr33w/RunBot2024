using Deployf.Botf;
using RunBot2024.Models;
using RunBot2024.Services;
using RunBot2024.Services.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace RunBot2024.Controllers
{
    public class RegistrationController : BotController
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RegistrationController> _logger;
        private readonly MessageSender _messageSender;
        private IRivalService _rivalService;
        private ICompanyService _companyService;
        private ILogService _logService;

        private RivalModel _rival;
        private List<RivalModel> _rivalList;
        private List<Region> _regionList;
        private List<City> _cityList;
        private List<Company> _companyList;

        private DateTime registrationStartDate;
        private static DateTime registrationEndDate;
        
        public RegistrationController (IConfiguration configuration, ILogger<RegistrationController> logger, MessageSender messageSender, IRivalService rivalService, ICompanyService companyService, ILogService logService)
        {
            _configuration = configuration;
            _logger = logger;
            _messageSender = messageSender;
            _rivalService = rivalService;
            _companyService = companyService;
            _logService = logService;
            //Initial();
        }

        [State]
        string _name;
        private async Task Initial()
        {
            _rivalList = await _rivalService.GetAllRivalsAsync();
            _cityList = await _companyService.GetCityListAsync();
            _regionList = await _companyService.GetRegionListAsync();

            registrationStartDate = Convert.ToDateTime(_configuration["RegistrationStartDate"]);
            registrationEndDate = Convert.ToDateTime(_configuration["RegistrationEndDate"]);
        }

        [Action("/register", "Регистрация")]
        public async Task Register()
        {
            registrationEndDate = Convert.ToDateTime(_configuration["RegistrationEndDate"]);
            DateTime now = DateTime.Now;
            TimeSpan timeSpanToEndRegistration = registrationEndDate - now;
            TimeSpan timeSpanToStartRegistration = registrationStartDate - now;

            if (timeSpanToEndRegistration.TotalMinutes < 0)
            {
                await Send("Регистрация на соревнования уже окончена.");
            }
            else
            {
                await Send("Секундочку... идёт поиск...");
                _rivalList = await _rivalService.GetAllRivalsAsync();

                var existingRival = _rivalList.FirstOrDefault(u => u.TelegramId == FromId);

                if (existingRival != null || existingRival != default)
                {
                    await Send($"Вы уже зарегистрированы как {existingRival.Name}, {existingRival.Company}");
                }
                else
                {
                    await Send("\nВведите ваши фамилию и имя:");

                    var name = await AwaitText();
                    _name = name;

                    await Send($"\nПродолжить регистрацию как {name}?\n");

                    await ConfirmNameRequest();
                }
            }
        }

        [Action]
        public async Task ConfirmNameRequest()
        {
            string continueRegistrationString = "Да, продолжить регистрацию";
            string changeNameString = "Нет, изменить имя";

            var _continueRegistrationButton = InlineKeyboardButton.WithCallbackData(continueRegistrationString, "continue");
            var _changeNameButton = InlineKeyboardButton.WithCallbackData(changeNameString, "changeName");

            //-----------------------------------------------------------------------------------
            //  Пришлось обращаться к TelegramBotClient'у, а не делать через Botf, 
            //  отрисовывая кнопки через InlineKeyboardMarkup
            //  так как при выборе кнопки "изменить имя" - кнопки, отрисованные через Botf (Button(Q))
            //  съезжали вверх и отрисовывались над сообщениями с каждым нажатием на кнопку "изменить имя"
            //  Проблему удалось исправить через InlineKeyboardMarkup
            //-----------------------------------------------------------------------------------

            //-----------------------------------------------------------------------------------
            //  В дальнейшем коде этого контроллера решил продолжить использовать InlineKeyboardMarkup
            //  просто для практики.
            //  При выборе предприятия, региона и города в целом можно использовать
            //  стандартную для Botf 'Button(Q)'
            //-----------------------------------------------------------------------------------

            InlineKeyboardButton[][] buttons = new InlineKeyboardButton[2][];
            buttons[1] = new InlineKeyboardButton[1] { _continueRegistrationButton };
            buttons[0] = new InlineKeyboardButton[1] { _changeNameButton };

            InlineKeyboardMarkup markup = new InlineKeyboardMarkup(buttons);

            Message sentMessage = await Client
                .SendTextMessageAsync(FromId, $"\n{_name} - имя введено верно?\n", ParseMode.Html, default, default, default, default, 0, true, markup);

            var callback = await AwaitQuery();

            await Client.EditMessageTextAsync(
                chatId: FromId,
                text: "Продолжить регистрацию",
                messageId: sentMessage.MessageId,
                replyMarkup: null
                );

            if (callback.ToString() == "continue")
            {
                await ContinueRegistration(_name);
            }
            else if (callback.ToString() == "changeName")
            {
                await ChangeName();
            }
        }

        [Action]
        public async Task ChangeName()
        {
            await Send("\nВведите ваши фамилию и имя заново:\n");

            _name = await AwaitText();
            await ConfirmNameRequest();
        }

        [Action]
        public async Task ContinueRegistration(string name)
        {
            try
            {
                _cityList = await _companyService.GetCityListAsync();
                _regionList = await _companyService.GetRegionListAsync();
                
                _companyList = await _companyService.GetCompanyListAsync();
            }
            catch (Exception)
            {
                PushL("Ошибка при загрузке списка регионов.\nПопробуйте позже. Если ошибка повторится - обратитесь к администратору бота или к организаторам проекта.");
            }

            #region выбор пола

            string maleGenderString = "мужчина";
            string femaleGenderString = "женщина";

            var _maleGenderButton = InlineKeyboardButton.WithCallbackData(maleGenderString, "male");
            var _femaleGenderButton = InlineKeyboardButton.WithCallbackData(femaleGenderString, "female");

            InlineKeyboardButton[] buttons = new InlineKeyboardButton[]
            {
                InlineKeyboardButton.WithCallbackData(maleGenderString, "male"),
                InlineKeyboardButton.WithCallbackData(femaleGenderString, "female")
            };

            InlineKeyboardMarkup markup = new InlineKeyboardMarkup(buttons);

            Message genderRequestMessage = await Client
               .SendTextMessageAsync(FromId, $"\n{_name}, укажите ваш пол:\n", ParseMode.Html, default, default, default, default, 0, true, markup);

            var genderCallback = await AwaitQuery();
            char gender = genderCallback[0];

            await Client.EditMessageTextAsync(
                chatId: FromId,
                text: "Продолжаем",
                messageId: genderRequestMessage.MessageId,
                replyMarkup: null
                );
            #endregion

            #region Ввод возраста

            await Client
              .SendTextMessageAsync(FromId, $"\n{_name}, введите ваш возраст:\n", ParseMode.Html, default, default, default, default, 0, true, null);

            bool ageEnterAction = true;
            int age = 0;

            while (ageEnterAction)
            {
                try
                {
                    age = Convert.ToInt32(await AwaitText());
                    ageEnterAction = false;
                }
                catch (Exception)
                {
                    await Client
             .SendTextMessageAsync(FromId, $"Возраст введён некорректно. Введите ваш возраст ещё раз (только цифры)", ParseMode.Html, default, default, default, default, 0, true, null);
                }
            }
            #endregion

            #region выбор региона, города, предприятия и сохранение

            if (_regionList.Count > 0)
            {
                InlineKeyboardButton[][] regionButtons = new InlineKeyboardButton[_regionList.Count][];

                int i = 0;

                foreach (var eachRegion in _regionList)
                {
                    var _currentRegionButton = InlineKeyboardButton.WithCallbackData(eachRegion.RegionName, $"{eachRegion.RegionId}");

                    regionButtons[i] = new InlineKeyboardButton[1] { _currentRegionButton };
                    i++;
                }
                InlineKeyboardMarkup regionMarkup = new InlineKeyboardMarkup(regionButtons);

                Message regionMessage = await Client
                    .SendTextMessageAsync(FromId, $"{_name}, выберите регион, в котором находится ваше предприятие:", ParseMode.Html, default, default, default, default, 0, true, regionMarkup);

                var regionCallback = await AwaitQuery();

                Region selectedRegion = _regionList.First(r => r.RegionId == Convert.ToInt32(regionCallback));

                string region = selectedRegion.RegionName;

                await Client.EditMessageTextAsync(
                    chatId: FromId,
                    text: $"{_name}, вы выбрали {region}",
                    messageId: regionMessage.MessageId,
                    replyMarkup: null
                    );

                #region выбор города

                int y = 0;

                List<City> selectedRegionCityList = new List<City>(_cityList.Where(c => c.RegionId == selectedRegion.RegionId).ToList());

                InlineKeyboardButton[][] cityButtons = new InlineKeyboardButton[selectedRegionCityList.Count][];

                foreach (var city in selectedRegionCityList)
                {
                    var _currentCityButton = InlineKeyboardButton.WithCallbackData(city.CityName, $"{city.CityId}");
                    cityButtons[y] = new InlineKeyboardButton[1] { _currentCityButton };
                    y++;
                }
                InlineKeyboardMarkup cityMarkup = new InlineKeyboardMarkup(cityButtons);

                Message cityMessage = await Client
                    .SendTextMessageAsync(FromId, $"{_name}, выберите город, в котором находится ваше предприятие:", ParseMode.Html, default, default, default, default, 0, true, cityMarkup);

                var cityCallback = await AwaitQuery();

                City selectedCity = _cityList.First(c => c.CityId == Convert.ToInt32(cityCallback));

                await Client.EditMessageTextAsync(
                    chatId: FromId,
                    text: $"{_name}, вы выбрали {selectedCity.CityName}",
                    messageId: cityMessage.MessageId,
                    replyMarkup: null
                    );
                #endregion

                #region выбор предприятия

                int j = 0;

                List<Company> selectedCityCompanies = new List<Company>(_companyList.Where(c => c.CityId == selectedCity.CityId).ToList());
                InlineKeyboardButton[][] companyButtons = new InlineKeyboardButton[selectedCityCompanies.Count][];

                foreach (var company in selectedCityCompanies)
                {
                    var _currentCompanyButton = InlineKeyboardButton.WithCallbackData(company.CompanyName, $"{company.CompanyId}");
                    companyButtons[j] = new InlineKeyboardButton[1] { _currentCompanyButton };
                    j++;
                }
                InlineKeyboardMarkup companyMarkup = new InlineKeyboardMarkup(companyButtons);

                Message companyMessage = await Client
                    .SendTextMessageAsync(FromId, $"{_name}, выберите ваше предприятие:", ParseMode.Html, default, default, default, default, 0, true, companyMarkup);

                var companyCallback = await AwaitQuery();

                Company selectedCompany = _companyList.First(c => c.CompanyId == Convert.ToInt32(companyCallback));

                await Client.EditMessageTextAsync(
                    chatId: FromId,
                    text: $"{_name}, вы выбрали {selectedCompany.CompanyName}",
                    messageId: companyMessage.MessageId,
                    replyMarkup: null
                    );

                #endregion

                #region сохранение анкеты участника

                RivalModel newRival = new RivalModel();

                newRival.Name = name;
                newRival.Gender = gender;
                newRival.Age = age;
                newRival.Company = selectedCompany.CompanyName;
                newRival.CreatedAt = DateTime.UtcNow;
                newRival.TelegramId = FromId;
                newRival.TotalResult = 0;
                newRival.UpdatedAt = DateTime.UtcNow;

                await SaveNewRival(newRival);

                #endregion
            }
            else
            {
                PushL("Ошибка при загрузке списка регионов.\nПопробуйте позже. Если ошибка повторится - обратитесь к администратору бота или к организаторам проекта.");

                //await SaveErrorLogMethod(null, FromId, null, null);
            }
            #endregion
        }

        [Action]
        public async Task SaveNewRival(RivalModel rival)
        {
            var newRival = rival;
            //_rivalList = await _rivalService.GetAllRivalsAsync();

            var existingUser = await _rivalService.GetRivalByIdAsync(rival.TelegramId);//_rivalList.FirstOrDefault(r => r.TelegramId == rival.TelegramId);
            if (existingUser != null || existingUser != default)
            {
                PushL($"Вы уже зарегистрированы как {existingUser.Name}, {existingUser.Company}!");
            }
            else
            {
                try
                {
                    var answer = await _rivalService.CreateRivalAsync(rival);

                    if (answer)
                    {
                        await Send($"{rival.Name}, {rival.Company} - вы успешно зарегистрированы!");
                    }
                    else
                    {
                        //await Send("Возникла ошибка при сохранении анкеты");
                        throw new Exception("Возникла ошибка при сохранении анкеты");
                    }
                    
                }
                catch (Exception e)
                {
                    await Send("Возникла ошибка при сохранении анкеты");

                    await SaveErrorLogMethod(e, FromId, null, existingUser.Name);
                }
            }
        }

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
    }
}
