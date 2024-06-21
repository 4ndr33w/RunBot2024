using Deployf.Botf;
using RunBot2024.Services.Interfaces;
using SQLite;

namespace RunBot2024.Controllers
{
    public class RunStatisticController : BotController
    {
        readonly TableQuery<Models.User> _users;
        readonly ILogger<RunStatisticController> _logger;
        readonly BotfOptions _options;
        readonly IConfiguration _configuration;
        readonly ILogService _logService;
        readonly IRivalService _rivalService;

        public RunStatisticController
            (
            TableQuery<Models.User> users,
            ILogger<RunStatisticController> logger,
            BotfOptions options,
            IConfiguration configuration,
            ILogService logService,
            IRivalService rivalService
            )
        {
            _users = users;
            _logger = logger;
            _options = options;
            _configuration = configuration;
            _logService = logService;
            _rivalService = rivalService;
        }

        [Action("/stat", "Вывести статистику по соревнованию")]
        public async Task Stat()
        {
            DateTime blackoutStarts = Convert.ToDateTime(_configuration["BlackoutStartDate"]);
            DateTime blackoutEnds = Convert.ToDateTime(_configuration["BlackoutEndDate"]);
            DateTime now = DateTime.Now;

            TimeSpan TimeMark1 = blackoutStarts - now;
            TimeSpan TimeMark2 = blackoutEnds - now;

            if (TimeMark1.TotalMinutes < 0 && TimeMark2.TotalMinutes > 0)
            {
                await Send("Информация по статистике соревнований в данный момент недоступна.");
            }
            else
            {
                PushL("Вывести сводную статистику по соревнованиям:");
                await StatFilterRequest();
            }
        }

        [Action]
        private async Task StatFilterRequest()
        {
            string all = "все участники";
            string male = "мужчины";
            string female = "женщины";

            var qTotal = Q(FilterStat, "all");
            var qMale = Q(FilterStat, "male");
            var qFemale = Q(FilterStat, "female");

            RowButton(all, qTotal);
            RowButton(male, qMale);
            RowButton(female, qFemale);
        }

        [Action]
        private async Task FilterStat(string gender)
        {
            PushL("Сводка по участникам.");

            var rivalList = await _rivalService.GetAllRivalsAsync();

            if (gender != "all")
            {
                rivalList = rivalList
                    .Where(x => x.Gender == gender[0])
                    .OrderByDescending(x => x.TotalResult)
                    .ToList();
            }
        }

    }
}
