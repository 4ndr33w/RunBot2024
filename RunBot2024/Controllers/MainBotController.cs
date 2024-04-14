using Deployf.Botf;
using Microsoft.AspNetCore.Mvc;
using RunBot2024.Models;
using SQLite;
using Telegram.Bot;

namespace RunBot2024.Controllers
{
    public class MainBotController : BotController
    {
        readonly TableQuery<User> _users;
        readonly SQLiteConnection _sqLiteConnection;
        readonly ILogger<MainBotController> _logger;
        readonly BotfOptions _options;
        readonly IConfiguration _configuration;

        public MainBotController(TableQuery<User> users, SQLiteConnection sqLiteConnection, ILogger<MainBotController> logger, BotfOptions options, IConfiguration configuration)
        {
            _users = users;
            _sqLiteConnection = sqLiteConnection;
            _logger = logger;
            _options = options;
            _configuration = configuration;
        }
        [Action("/start", "start bot")]
        public async Task Start()
        {
            KButton("/run");
            KButton("/stat");
            KButton("/register");
            KButton("/help");

            if (_users.First(u => u.Id == FromId).Role == Models.Enums.UserRole.admin)
            {
                MakeKButtonRow();
                KButton("/sendTo");
                KButton("/findByName");
                KButton("/delete");
                KButton("/send");
                MakeKButtonRow();
                KButton("/edit");
            }
            await Send("HelloMessage");
            //await Send($"{_users.Count()} \n{_users.Where(c => c.Id == FromId).FirstOrDefault().Role}");
            //await Send($"{_users.Where(c => c.Id == FromId).FirstOrDefault().FullName}");
        }

        [On(Handle.BeforeAll)]
        public void PreHandle()
        {
            if (!_users.Any(u => u.Id == FromId))
            {
                var user = new User
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

        [Action("/test")]
        [Authorize("admin")]
        public async Task Test()
        {
            await Send("Authorized");
        }

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
    }
}
