using Deployf.Botf;
using Microsoft.AspNetCore.Mvc;
using RunBot2024.Models;
using RunBot2024.Services.Interfaces;
using System.Xml.Linq;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace RunBot2024.Controllers
{
    public class RegistrationController : BotController
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly MessageSender _messageSender;
        private IRivalService _rivalService;
        private ICompanyService _companyService;

        private RivalModel _rival;
        private List<RivalModel> _rivalList;
        private List<Region> _regionList;
        private List<City> _cityList;
        private List<Company> _companyList;

        private DateTime registrationStartDate;
        private static DateTime registrationEndDate;
        
        public RegistrationController (IConfiguration configuration, /*ILogger logger, */MessageSender messageSender, IRivalService rivalService, ICompanyService companyService)
        {
            _configuration = configuration;
            //_logger = logger;
            _messageSender = messageSender;
            _rivalService = rivalService;
            _companyService = companyService;
            Initial();
        }

        [State]
        string _name;

        [State]
        char _gender;

        [State]
        int _age;

        [State]
        string _region;

        [State]
        string _city;

        [State]
        string _company;

        private async Task Initial()
        {
            _rivalList = await _rivalService.GetAllRivalsAsync();

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
                    await Send("При регистрации необходимо указать ваши фамилию и имя, ваш пол и выбрать команду.\nПриступим\n\nВведите ваши фамилию и имя:");

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

            InlineKeyboardButton[][] buttons = new InlineKeyboardButton[2][];
            buttons[0] = new InlineKeyboardButton[1] { _continueRegistrationButton };
            buttons[1] = new InlineKeyboardButton[1] { _changeNameButton };

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
                //await GenderRequest();
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
            RivalModel newRival = new RivalModel();
            newRival.Name = name;

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

            var callback = await AwaitQuery();

            newRival.Gender = callback[0];

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

            newRival.Age = age;
            #endregion

            #region выбор региона

            _regionList = await _companyService.GetRegionListAsync();

            if (_regionList.Count > 0)
            {
                InlineKeyboardButton[][] regionButtons = new InlineKeyboardButton[_regionList.Count][];

                int i = 0;

                foreach (var eachRegion in _regionList)
                {
                    var regionName = eachRegion.RegionName;

                    var _currentRegionButton = InlineKeyboardButton.WithCallbackData(regionName, $"{eachRegion.RegionId}");

                    regionButtons[i] = new InlineKeyboardButton[1] { _currentRegionButton };
                    i++;
                }
                InlineKeyboardMarkup regionMarkup = new InlineKeyboardMarkup(regionButtons);

                Message regionMessage = await Client
                    .SendTextMessageAsync(FromId, $"{_name}, выберите регион, в котором находится ваше предприятие:", ParseMode.Html, default, default, default, default, 0, true, regionMarkup);

                var regionCallback = await AwaitQuery();

                string region = _regionList.First(r => r.RegionId == Convert.ToInt32(regionCallback)).RegionName;

                await Client.EditMessageTextAsync(
                    chatId: FromId,
                    text: $"{_name}, выбранный регион: {region}",
                    messageId: regionMessage.MessageId,
                    replyMarkup: null
                    );
            }
            else
            {
                PushL("Ошибка при загрузке списка регионов.\nПопробуйте позже. Если ошибка повторится - обратитесь к администратору бота или к организаторам проекта.");
            }
            #endregion

            #region выбор города
            #endregion
        }

        [Action]
        public async Task GenderRequest()
        {
            PushL($"\n{_name}, укажите ваш пол:\n");

            string maleGender = "мужчина";
            string femaleGender = "женщина";

            var qMale = Q(RegionRequest, "male");
            var qFemale = Q(RegionRequest, "female");

            Button(maleGender, qMale);
            Button(femaleGender, qFemale);
        }

        [Action]
        public async Task RegionRequest(string gender)
        {
            _regionList = await _companyService.GetRegionListAsync();
            _gender = gender[0];
            
            if (_regionList.Count > 0)
            {
                InlineKeyboardButton[][] buttons = new InlineKeyboardButton[_regionList.Count][];

                int i = 0;

                foreach (var regionn in _regionList)
                {
                    var regionName = regionn.RegionName;

                    var _currentRegionButton = InlineKeyboardButton.WithCallbackData(regionName, $"{regionn.RegionId}");
                    
                    buttons[i] = new InlineKeyboardButton[1] { _currentRegionButton };
                    i++;
                }
                InlineKeyboardMarkup markup = new InlineKeyboardMarkup(buttons);

                Message sentMessage = await Client
                    .SendTextMessageAsync(FromId, $"{_name}, выберите регион, в котором находится ваше предприятие:", ParseMode.Html, default, default, default, default, 0, true, markup);

                var callback = await AwaitQuery();

                string region = _regionList.First(r => r.RegionId == Convert.ToInt32(callback)).RegionName;

                await Client.EditMessageTextAsync(
                    chatId: FromId,
                    text: $"{_name}, выбранный регион: {region}",
                    messageId: sentMessage.MessageId,
                    replyMarkup: null
                    );

                await CityRequest(region);
            }
            else
            {
                PushL("Ошибка при загрузке списка регионов.\nПопробуйте позже. Если ошибка повторится - обратитесь к администратору бота или к организаторам проекта.");
            }
        }

        [Action]
        public async Task CityRequest(string region)
        {
            _region = region;
            _cityList = await _companyService.GetCityListAsync();
            Region selectedRegion = _regionList.Where(r => r.RegionName == region).First();
            if (_cityList.Count > 0)
            {
                List<City> cityList = _cityList.Where(c => c.RegionId == selectedRegion.RegionId).ToList();
                PushL($"\n{_name}, {region}. выберите город, в котором расположено ваше предприятие:\n");

                foreach (var city in cityList)
                {
                    var cityName = city.CityName;
                    //_city = cityName;
                    var qFunc = Q(CompanyRequest, cityName);
                    RowButton(cityName, qFunc);
                }
            }
            else
            {
                PushL("Ошибка при загрузке списка городов.\nПопробуйте позже. Если ошибка повторится - обратитесь к администратору бота или к организаторам проекта.");
            }
        }

        [Action]
        public async Task CompanyRequest(string city)
        {
            _city = city;
            City selectedCity = _cityList.Where(c => c.CityName == city).First();

            if (_companyList.Count > 0)
            {
                List<Company> companyList = _companyList.Where(c => c.CityId == selectedCity.CityId).ToList();
                PushL($"\n{_name}, выберите ваше предприятие:\n");

                foreach (var company in companyList)
                {
                    var companyName = company.CompanyName;
                    //_company = companyName;
                    var qFunc = Q(CompleteRegistration, companyName);

                    RowButton(companyName, qFunc);
                }
            }
        }
        [Action]
        public async Task CompleteRegistration(string company)
        {
            _company = company;
            await Send($"{_name}, {_company}");
        }
    }
}
