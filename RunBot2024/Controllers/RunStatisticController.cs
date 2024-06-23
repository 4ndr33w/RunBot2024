using Deployf.Botf;
using RunBot2024.Models;
using RunBot2024.Services.Interfaces;
using System.Text;

namespace RunBot2024.Controllers
{
    public class RunStatisticController : BotController
    {
        readonly ILogger<RunStatisticController> _logger;
        readonly BotfOptions _options;
        readonly IConfiguration _configuration;
        readonly ILogService _logService;
        readonly IRivalService _rivalService;
        private readonly ICompanyService _companyService;

        public RunStatisticController
            (
                ILogger<RunStatisticController> logger,
                BotfOptions options,
                IConfiguration configuration,
                ILogService logService,
                IRivalService rivalService,
                ICompanyService companyService
            )
        {
            _logger = logger;
            _options = options;
            _configuration = configuration;
            _logService = logService;
            _rivalService = rivalService;
            _companyService = companyService;
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

            //---------------------------------------------------------------------
            //Передаём FilteredRivalList для отображения статистики по участникам
            //         FullRivalList - для подсчета статистики по предприятиям
            //---------------------------------------------------------------------

            await RivalStatisticsListViewMethod(rivalList, gender);
        }

        [Action]
        private async Task RivalStatisticsListViewMethod(List<RivalModel> rivalList, string gender)
        {
            StringBuilder statList = new StringBuilder();

            if (rivalList.Count < 1)
            {
                await Send("Участников нет");
            }
            else
            {
                if (gender == "male")
                {
                    await Send("Мужчины: ");
                }
                if (gender == "female")
                {
                    await Send("Женщины: ");
                }
                if (gender == "all")
                {
                    await Send("Все участники: ");
                }

                foreach (var rival in rivalList)
                {
                    int index = rivalList.IndexOf(rival) + 1;
                    statList.AppendLine($"{index} - {rival.Name} - {rival.TotalResult} км\n     •   {rival.Company}");
                }

                await Send($"{statList.ToString()}\n----------------------------------------\n\n\"Сводка по предприятиям:\n");

                statList.Clear();

                await CompanyStatisticsListViewMethod();
            }
        }

        [Action]
        private async Task CompanyStatisticsListViewMethod()
        {
            var companyStatList = await _rivalService.GetCompanyStatisitcs();

            int index = 0;

            StringBuilder statList = new StringBuilder();
            foreach (var item in companyStatList)
            {
                index++;
                statList.AppendLine($"{index} - {item.CompanyName} - {item.Result} км");
                statList.AppendLine($"     •   {item.RivalsCount} участников");
            }
            await Send(statList.ToString());

            statList.Clear();
        }
    }
}
