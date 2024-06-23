using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using RunBot2024.Models;

namespace RunBot2024.Services
{
    public class CompanyStatisticsService
    {

        private readonly IConfiguration _configuration;

        public CompanyStatisticsService(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        public async Task<List<Company>> GetCompanyListAsync()
        {
            using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("NpgConnection")))
            {
                var response = await connection.QueryAsync<Company>($"SELECT * FROM \"{_configuration["PostgreDefaultSchema"]}\".\"{_configuration["CompanyListTable"]}\";");
                return response.ToList();
            }
        }
    }
}
