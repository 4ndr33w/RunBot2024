using System.ComponentModel.DataAnnotations;

namespace RunBot2024.Models
{
    public class Region
    {
        [Key]
        private readonly int _regionId;
        private readonly string _regionName;

        public int RegionId { get { return _regionId; } }
        public string RegionName { get { return _regionName; } }
    }
}
