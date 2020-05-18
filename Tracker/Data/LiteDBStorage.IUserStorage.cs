using LiteDB;
using Schmellow.DiscordServices.Tracker.Models;
using System.Linq;

namespace Schmellow.DiscordServices.Tracker.Data
{
    public sealed partial class LiteDBStorage : IUserStorage
    {
        private ILiteCollection<User> UserCollection
        {
            get
            {
                return _db.GetCollection<User>("users");
            }
        }

        private void InitAuthStorage()
        {
            BsonMapper.Global.Entity<User>().Id(u => u.LocalId, true);
            UserCollection.EnsureIndex("CharacterName");
        }

        public int AddUser(User user)
        {
            if (user == null)
                return 0;
            int id = UserCollection.Insert(user);
            if (id > 0)
                _db.Checkpoint();
            return id;
        }

        public bool UpdateUser(User user)
        {
            if (user == null)
                return false;
            bool isSuccess = UserCollection.Update(user);
            if (isSuccess)
                _db.Checkpoint();
            return isSuccess;
        }

        public bool DeleteUser(string userName)
        {
            var user = GetUser(userName);
            if (user == null)
                return false;
            bool isSuccess = UserCollection.Delete(user.LocalId);
            if (isSuccess)
                _db.Checkpoint();
            return isSuccess;
        }

        public User GetUser(string userName)
        {
            if (string.IsNullOrEmpty(userName))
                return null;
            return UserCollection.FindOne(u => u.CharacterName == userName);
        }

        public User[] GetUsers()
        {
            return UserCollection.FindAll().ToArray();
        }

    }
}
