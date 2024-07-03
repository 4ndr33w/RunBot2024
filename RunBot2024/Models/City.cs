using System.ComponentModel.DataAnnotations;

namespace RunBot2024.Models
{
    public class City
    {
        private int _id;
        private string _name;
        private int _regionId;

        [Key]
        public int Id { get { return _id; } set => _id = value;  }
        public int RegionId { get { return _regionId; } set => _regionId = value; }
        public string Name { get { return _name; } set => _name = value; }
    }
}
