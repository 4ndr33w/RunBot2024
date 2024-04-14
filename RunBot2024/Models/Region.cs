using System.ComponentModel.DataAnnotations;

namespace RunBot2024.Models
{
    public class Region
    {
        [Key]
        private int _regionId;
        private string _regionName;

        public int RegionId { get { return _regionId; } set => _regionId = value; }
        public string RegionName { get { return _regionName; } set => _regionName = value; }
    }
}
