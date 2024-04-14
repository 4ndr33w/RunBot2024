using Deployf.Botf;
using Microsoft.AspNetCore.Mvc;
using RunBot2024.Models;
using RunBot2024.Services.Interfaces;

namespace RunBot2024.Controllers
{
    public class TestController : BotController
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly IRivalService _rivalService;
        private List<RivalModel> _users;

        public TestController(IConfiguration configuration, /*ILogger logger, */IRivalService rivalService/*, List<RivalModel> users*/)
        {
            _configuration = configuration;
            //_logger = logger;
            _rivalService = rivalService;
            //_users = users;
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
    }
}
