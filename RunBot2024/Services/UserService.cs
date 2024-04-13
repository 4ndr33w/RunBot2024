using Deployf.Botf;
using RunBot2024.Models;
using RunBot2024.Models.Enums;
using SQLite;

namespace RunBot2024.Services
{
    public class UserService : IBotUserService
    {
        readonly TableQuery<User> _users;
        static string[] _zeroRoles = new string[0];

        static UserRole[] _roles = Enum.GetValues<UserRole>();

        public UserService(TableQuery<User> users)
        {
            _users = users;
        }

        public ValueTask<(string? id, string[]? roles)> GetUserIdWithRoles(long tgUserId)
        {
            var user = _users.FirstOrDefault(u => u.Id == tgUserId);
            if (user == null || user == default)
            {
                return new ((null, null));
            }

            var id = user.Id.ToString();
            var role = GetRoles(user.Role);
            return new((id, role));
        }

        private string[] GetRoles(UserRole role)
        {
            if (role == UserRole.none)
            {
                return _zeroRoles;
            }
            return _roles.Where(r => ((int)r & (int)role) == (int)r)
                .Select(r => r.ToString())
                .ToArray();
        }
    }
}
