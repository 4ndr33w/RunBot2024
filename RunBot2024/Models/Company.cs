using System.ComponentModel.DataAnnotations;

namespace RunBot2024.Models
{
    public class Company
    {
        [Key]
        private readonly int _companyId;
        private readonly string _companyName;
        private readonly int _cityId;

        public int CompanyId { get { return _companyId; } }
        public string CompanyNamer { get { return _companyName; } }
        public int CityId { get { return _cityId; } }
    }
}
