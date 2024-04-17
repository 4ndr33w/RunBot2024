using Deployf.Botf;
using Microsoft.AspNetCore.Mvc;
using RunBot2024.Models;
using RunBot2024.Services.Interfaces;
using SQLite;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;

namespace RunBot2024.Controllers
{
    public class MainBotController : BotController
    {
        readonly TableQuery<Models.User> _users;
        readonly SQLiteConnection _sqLiteConnection;
        readonly ILogger<MainBotController> _logger;
        readonly BotfOptions _options;
        readonly IConfiguration _configuration;
        readonly ILogService _logService;
        readonly IRivalService _rivalService;

        public MainBotController(TableQuery<Models.User> users, SQLiteConnection sqLiteConnection, ILogger<MainBotController> logger, BotfOptions options, IConfiguration configuration, ILogService logService, IRivalService rivalService)
        {
            _users = users;
            _sqLiteConnection = sqLiteConnection;
            _logger = logger;
            _options = options;
            _configuration = configuration;
            _logService = logService;
            _rivalService = rivalService;
        }

        #region start
        [Action("/start", "start bot")]
        public async Task Start()
        {
            KButton("/run"); // Done
            KButton("/stat");
            KButton("/register");  // Done
            KButton("/help"); // Done

            if (_users.First(u => u.Id == FromId).Role == Models.Enums.UserRole.admin)
            {
                MakeKButtonRow();
                KButton("/sendTo"); // Done
                KButton("/findByName");
                KButton("/delete");
                KButton("/send");
                MakeKButtonRow();
                KButton("/edit");
            }

            using (var streamReader = new StreamReader(_configuration["StartMessageTextFile"]))
            {
                StringBuilder helloMessageText = new StringBuilder();
                helloMessageText.Append(await streamReader.ReadToEndAsync());
                streamReader.Close();

                await Send(helloMessageText.ToString());
                helloMessageText.Clear();
            }
            using (var fileStream = new FileStream(_configuration["LogoStaticFilePathl"], FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                await Client.SendPhotoAsync(chatId: FromId, photo: new InputOnlineFile(fileStream));
                fileStream.Close();
            }
            //await Send("HelloMessage");
        }
        #endregion

        #region help

        [Action("/help", "Помощь")]
        public async Task Help()
        {
            using (var streamReader = new StreamReader(_configuration["HelpMessageTextFile"]))
            {
                StringBuilder helpMessageText = new StringBuilder();
                helpMessageText.Append(await streamReader.ReadToEndAsync());
                streamReader.Close();

                await Send(helpMessageText.ToString());
                helpMessageText.Clear();
            }
        }
        #endregion

        #region addResult
        [Action("/run", "Добавить результат тренировки")]
        public async Task Run()
        {
            List<RivalModel> rivalList = new List<RivalModel>();
            try
            {
                rivalList = await _rivalService.GetAllRivalsAsync();
            }
            catch (Exception e)
            {
                await OnException(e);
            }

            var existingRival = rivalList.FirstOrDefault(x => x.TelegramId == FromId);

            DateTime eventStartDate = Convert.ToDateTime(_configuration["AddResultStartDate"]);
            DateTime eventEndDate = Convert.ToDateTime(_configuration["AddResultEndDate"]);
            DateTime nowDate = DateTime.Now;

            TimeSpan timeSpanToEventEnd = eventEndDate - nowDate;
            TimeSpan timeSpanToEventStart = eventStartDate - nowDate;

            

            if (existingRival != null || existingRival != default)
            {
                if (timeSpanToEventStart.TotalMinutes > 0)
                {
                    StringBuilder timeToEventStartString = new StringBuilder();

                    timeToEventStartString
                        .Append(timeSpanToEventStart.TotalDays > 0 ? $"{timeSpanToEventStart.Days} д. " : "")
                        .Append(timeSpanToEventStart.TotalHours > 0 ? $"{timeSpanToEventStart.Hours} ч. " : "")
                        .Append(timeSpanToEventStart.TotalMinutes > 0 ? $"{timeSpanToEventStart.Minutes} м. " : "");

                    PushL($"Соревнование ещё не началось!\nДо начала осталось:\n{timeToEventStartString.ToString()}");

                    timeToEventStartString.Clear();
                }

                if (timeSpanToEventEnd.TotalSeconds < 0)
                {
                    PushL($"Соревнование уже окончено,\nрезультаты вносить нельзя.");
                }

                if (timeSpanToEventStart.TotalSeconds < 0 && timeSpanToEventEnd.TotalSeconds > 0)
                {
                    await Send("Добавление результата тренировки. \nВведите количество километров, которое хотите добавить:");

                    var stringResult = await AwaitText();
                    double currentResult = 0;

                    try
                    {
                        //if (stringResult.IndexOf(',') > 0)
                        //{
                        //    stringResult.Replace(',', '.');
                        //}
                        if (stringResult.IndexOf('.') > -1)
                        {
                            stringResult = stringResult.Replace('.', ',');
                        }

                        if (stringResult.IndexOf('+') > -1)
                        {
                            stringResult = stringResult.Remove(stringResult.IndexOf('+'), 1);
                        }
                        string regularPattern = @"[0-9]{0,5}\,?[0-9]{0,5}";
                        var _regex = new Regex(regularPattern);
                        var filteredString = _regex.Match(stringResult).Value;
                        stringResult = filteredString;
                        currentResult = Convert.ToDouble(stringResult);

                        double minimalAllowedResult = Convert.ToDouble(_configuration["MinimalAllowedResult"]);

                        if (currentResult < minimalAllowedResult)
                        {
                            await Send($"Допускается внесение результата тренировок не менее {minimalAllowedResult} км.");
                        }
                        else if (currentResult >= minimalAllowedResult)
                        {
                            #region добавление и сохранение прогресса

                            DateTime blackoutDate = Convert.ToDateTime(_configuration["BlackoutStartDate"]);
                            TimeSpan blackoutTimeSpan = blackoutDate - DateTime.Now;

                            existingRival.TotalResult += currentResult;
                            existingRival.UpdatedAt = DateTime.UtcNow;

                            rivalList = rivalList.OrderByDescending(x => x.TotalResult).ToList();

                            int index = rivalList.IndexOf(existingRival) + 1;

                            StringBuilder resultString = new StringBuilder();

                            resultString
                                .Append($"{existingRival.Name}, Вы добавили {currentResult} км.\n")
                                .Append($"Ваш общий прогресс: {existingRival.TotalResult} км.\n");

                            if (blackoutTimeSpan.TotalMinutes < 0)
                            {
                                resultString.Append($"\nВы на <b>#######</b> месте из <b>{rivalList.Count}</b>");
                            }
                            else if (blackoutTimeSpan.TotalMinutes >= 0)
                            {
                                resultString.Append($"\nВы на <b>{index}</b> месте из <b>{rivalList.Count}</b>");
                                resultString.Append($"\n\nДля ознакомления с общей статистикой введите команду /stat");
                            }
                            await Send(resultString.ToString());
                            resultString.Clear();

                            ////////////////////////////////////////////////
                            
                            var resultLog = new ResultLog();
                            resultLog.TotalResult = existingRival.TotalResult;
                            resultLog.LastAddedResult = currentResult;
                            resultLog.TelegramId = existingRival.TelegramId;
                            resultLog.LastUpdated = existingRival.UpdatedAt;

                            ////////////////////////////////////////////////

                            await _rivalService.UpdateRivalAsync(existingRival);
                            await _logService.CreateResultLogAsync(resultLog);

                            ////////////////////////////////////////////////
                            #endregion
                        }
                    }
                    catch (Exception)
                    {
                        PushL("Некорректно введён результат.\nПопробуйте снова ввести команду /run и повторно добавить результат.\n" +
                           "Если проблема повторится, обратитесь к администратору бота");

                        //string yesString = "Да, попробовать снова";
                        //string noString = "Отмена";

                        //var qAgain = Q(Run);
                        //var qCancel = Q(CancelEnterResult);

                        //RowButton(yesString, qAgain);
                        //RowButton(noString, qCancel);
                    }
                }
            }
        }

        [Action]
        public async Task CancelEnterResult()
        {
            PushL("Отмена");
        }

        #endregion

        [On(Handle.BeforeAll)]
        public void PreHandle()
        {
            if (!_users.Any(u => u.Id == FromId))
            {
                var user = new Models.User
                {
                    Id = FromId,
                    FullName = Context!.GetUserFullName(),
                    UserName = Context!.GetUsername()!,
                    Role = Models.Enums.UserRole.user
                };
                
                _sqLiteConnection.Insert(user);
                _logger.LogInformation($"Added new user: {FromId}");
            }
        }

        [On(Handle.Exception)]
        public async Task OnException(Exception e)
        {
            _logger.LogError(e, "Unhandled exception");

            ErrorLog errorLog = new ErrorLog();
            errorLog.ErrorMessage = e.ToString();
            errorLog.TelegramId = FromId;
            errorLog.LastUpdated = DateTime.UtcNow;

            await _logService.CreateErrorLogAsync(errorLog);

            if (Context.Update.Type == Telegram.Bot.Types.Enums.UpdateType.CallbackQuery)
            {
                await AnswerCallback($"Error:\n{e}");
            }
            else if (Context.Update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
            {
                Push($"Error");
            }
        }

        [On(Handle.Unauthorized)]
        public void Unauthorized()
        {
            Push("Forbidden");
        }

        #region test
        [Action("/test")]
        [Authorize("admin")]
        public async Task Test()
        {
            await Send("Authorized");
        }

        #endregion

        #region setAdmin

        [Action("/setAdmin")]
        public async Task SetAdmin()
        {
            var mainAdminTelegramId = Convert.ToInt64(_configuration["AdminTelegramId"]);
            if (FromId == mainAdminTelegramId)
            {
                PushL("Кого назначить админом?");

                foreach (var user in _users)
                {
                    string userName = user.Id.ToString();
                    var qFunc = Q(ChangeUserToAdmin, user);

                    RowButton(userName, qFunc);
                }
            }
        }

        [Action]
        public async Task ChangeUserToAdmin(RunBot2024.Models.User user)
        {
            if (user.Role == Models.Enums.UserRole.admin)
            {
                await Send($"User {user.Id} already admin");
            }
            else
            {
                user.Role = Models.Enums.UserRole.admin;

                _sqLiteConnection.Update(user);

                var userName = user.Id.ToString();
                _logger.LogInformation($"User {userName} status changed to admin");
                await Send($"User {userName} status changed to admin");
            }
        }

        #endregion

        #region setToUser

        [Action("/setToUser")]
        public async Task SetToUser()
        {
            var mainAdminTelegramId = Convert.ToInt64(_configuration["AdminTelegramId"]);
            if (FromId == mainAdminTelegramId)
            {
                PushL("Какого холопа вернуть к юзерам");

                foreach (var user in _users)
                {
                    string userName = user.Id.ToString();
                    var qFunc = Q(ChangeAdminToUser, user);

                    RowButton(userName, qFunc);
                }
            }
        }

        [Action]
        public async Task ChangeAdminToUser(RunBot2024.Models.User user)
        {
            if (user.Role == Models.Enums.UserRole.user)
            {
                await Send($"User {user.Id} already auser");
            }
            else
            {
                user.Role = Models.Enums.UserRole.user;

                _sqLiteConnection.Update(user);

                var userName = user.Id.ToString();
                _logger.LogInformation($"User {userName} status changed back to user");
                await Send($"User {userName} status changed back to user");
            }
        }

        #endregion
    }
}
