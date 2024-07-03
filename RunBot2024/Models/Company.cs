using System.ComponentModel.DataAnnotations;

namespace RunBot2024.Models
{
    public class Company
    {
        private int _id;
        private string _name;
        private int _cityId;

        [Key]
        public int Id { get { return _id; } set => _id = value; }
        public string Name { get { return _name; } set => _name = value; }
        public int CityId { get { return _cityId; } set => _cityId = value; }
    }
}
