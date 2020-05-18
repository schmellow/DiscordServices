using Schmellow.DiscordServices.Tracker.Models;
using System.Collections.Generic;

namespace Schmellow.DiscordServices.Tracker.Data
{
    public interface IUserStorage
    {
        int AddUser(User user);
        bool UpdateUser(User user);
        bool DeleteUser(string userName);
        User GetUser(string userName);
        User[] GetUsers();
    }
}
