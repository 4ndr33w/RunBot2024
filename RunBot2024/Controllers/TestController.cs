using Deployf.Botf;
using RunBot2024.Services.Interfaces;

namespace RunBot2024.Controllers
{
    public class TestController : BotController
    {
        private readonly IConfiguration _configuration;
        private readonly IRivalService _rivalService;
        private readonly ILogger<TestController> _logger;

        public TestController(IConfiguration configuration, IRivalService rivalService, ILogger<TestController> logger)
        {
            _configuration = configuration;
            _rivalService = rivalService;
            _logger = logger;
        }

        [Action("/getAll")]
        public async Task GetAll()
        {
            //await Send("Hello Message");
            var userList = await _rivalService.GetAllRivalsAsync();

            foreach (var user in userList)
            {
                PushL($"{user.Name} - {user.Company}");
            }
        }

        [Action]
        public async Task Test()
        {
        }
    }
}
