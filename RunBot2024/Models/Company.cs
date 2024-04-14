using System.ComponentModel.DataAnnotations;

namespace RunBot2024.Models
{
    public class Company
    {
        [Key]
        private int _companyId;
        private string _companyName;
        private int _cityId;

        public int CompanyId { get { return _companyId; } set => _companyId = value; }
        public string CompanyName { get { return _companyName; } set => _companyName = value; }
        public int CityId { get { return _cityId; } set => _cityId = value; }
    }
}
