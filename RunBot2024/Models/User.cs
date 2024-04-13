using RunBot2024.Models.Enums;
using SQLite;

namespace RunBot2024.Models
{
    public class User
    {
        [PrimaryKey]
        public long Id { get; set; }

        [Indexed]
        public string UserName { get; set; }
        public string FullName { get; set; }
        public UserRole Role { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
