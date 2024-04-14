using System.ComponentModel.DataAnnotations;

namespace RunBot2024.Models
{
    public class City
    {
        [Key] 
        private readonly int _cityId;
        private readonly string _cityName;
        private readonly int _regionId;

        public int CityId { get { return _cityId; } }
        public string CityName { get { return _cityName; } }
        public int RegionId { get { return _regionId; } }
    }
}
