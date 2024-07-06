using RunBot2024.Models.Enums;
using SQLite;

namespace RunBot2024.Models
{
    public class User
    {
        private long _id;
        private string _userName;
        private string _userFullName;
        public UserRole _role;
        private DateTime _created;
        private DateTime _updated;

        [PrimaryKey]
        public long Id { get => _id; set => _id = value; }
        [Indexed]
        public string UserName { get => _userName; set => _userName = value; }
        public string FullName { get => _userFullName; set => _userFullName = value; }
        public UserRole Role { get => _role; set => _role = value; }
        public DateTime Updated { get => _updated; set => _updated = value; }
        public DateTime Created { get => _created; set => _created = value; }
    }
}
