using RunBot2024.Models;

namespace RunBot2024.Services.Interfaces
{
    public interface ICompanyService
    {
        Task<List<Region>> GetRegionListAsync();
        Task<List<City>> GetCityListAsync();
        Task<List<Company>> GetCompanyListAsync();
    }
}
