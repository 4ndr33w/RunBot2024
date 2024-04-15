using Dapper;
using Npgsql;
using RunBot2024.Models;
using RunBot2024.Services.Interfaces;

namespace RunBot2024.Services
{
    public class CompanyService : ICompanyService
    {
        private readonly IConfiguration _configuration;

        public CompanyService (IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<List<City>> GetCityListAsync()
        {
            using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("NpgConnection")))
            {
                var response = await connection.QueryAsync<City>($"SELECT * FROM \"{_configuration["PostgreDefaultSchema"]}\".\"{_configuration["CityListTable"]}\";");
                return response.ToList();
            }
        }

        public async Task<List<Company>> GetCompanyListAsync()
        {
            using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("NpgConnection")))
            {
                var response = await connection.QueryAsync<Company>($"SELECT * FROM \"{_configuration["PostgreDefaultSchema"]}\".\"{_configuration["CompanyListTable"]}\";");
                return response.ToList();
            }
        }

        public async Task<List<Region>> GetRegionListAsync()
        {
            using (var connection = new NpgsqlConnection(_configuration.GetConnectionString("NpgConnection")))
            {
                var response = await connection.QueryAsync<Region>($"SELECT * FROM \"{_configuration["PostgreDefaultSchema"]}\".\"{_configuration["RegionListTable"]}\";");
                return response.ToList();
            }
        }
    }
}
