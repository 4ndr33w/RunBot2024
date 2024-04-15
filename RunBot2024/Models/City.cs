using System.ComponentModel.DataAnnotations;

namespace RunBot2024.Models
{
    public class City
    {
        [Key] 
        private int _cityId;
        private string _cityName;
        private int _regionId;

        public int CityId { get { return _cityId; } set => _cityId = value;  }
        public int RegionId { get { return _regionId; } set => _regionId = value; }
        public string CityName { get { return _cityName; } set => _cityName = value; }
    }
}
