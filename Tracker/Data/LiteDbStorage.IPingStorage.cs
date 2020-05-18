using LiteDB;
using Schmellow.DiscordServices.Tracker.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Schmellow.DiscordServices.Tracker.Data
{
    public sealed partial class LiteDBStorage : IPingStorage
    {
        public LiteDBStorage()
        {
        }

        private ILiteCollection<Ping> Pings
        {
            get
            {
                return _db.GetCollection<Ping>("pings");
            }
        }

        private ILiteCollection<Link> Links
        {
            get
            {
                return _db.GetCollection<Link>("links");
            }
        }

        private ILiteCollection<LinkAction> Actions
        {
            get
            {
                return _db.GetCollection<LinkAction>("actions");
            }
        }

        private void InitPingStorage()
        {
            Pings.EnsureIndex("Guild");
            Links.EnsureIndex("PingId");
            Links.EnsureIndex("Guild");
            Links.EnsureIndex("User");
            Actions.EnsureIndex("PingId");
            Actions.EnsureIndex("LinkId");
            Actions.EnsureIndex("Guild");
            Actions.EnsureIndex("User");
        }

        public int CreatePing(string guild, string author, string text, IEnumerable<string> users)
        {
            int pingId = Pings.Insert(new Ping()
            {
                Guild = guild,
                Author = author,
                Created = DateTime.Now,
                Text = text
            });
            var links = users.Select(user => new Link()
            {
                PingId = pingId,
                Guild = guild,
                User = user
            });
            Links.InsertBulk(links);
            _db.Checkpoint();
            return pingId;
        }

        public int CreateAction(Link link, string origin, string useragent, string data)
        {
            int actionId = Actions.Insert(new LinkAction()
            {
                LinkId = link.Id,
                PingId = link.PingId,
                Guild = link.Guild,
                User = link.User,
                When = DateTime.Now,
                Origin = origin,
                UserAgent = useragent,
                Data = data
            });
            _db.Checkpoint();
            return actionId;
        }

        public Ping GetPing(int pingId)
        {
            return Pings.FindById(pingId);
        }

        public Ping[] GetPings(HashSet<string> guilds = null)
        {
            if (guilds != null && guilds.Any())
            {
                var arr = new BsonArray();
                foreach (string guild in guilds)
                    arr.Add(guild);
                return Pings
                    .Find(Query.In("Guild", arr))
                    .OrderByDescending(p => p.Id)
                    .ToArray();
            }
            else
            {
                return Pings
                    .FindAll()
                    .OrderByDescending(p => p.Id)
                    .ToArray();
            }
        }

        public Link GetLink(Guid linkId)
        {
            return Links.FindById(linkId);
        }

        public Link[] GetLinks(params int[] pingIds)
        {
            if (pingIds.Length == 0)
            {
                return new Link[0];
            }
            else if(pingIds.Length == 1)
            {
                return Links
                    .Find(Query.EQ("PingId", pingIds[0]))
                    .OrderBy(l => l.User)
                    .ToArray();
            }
            else
            {
                var arr = new BsonArray();
                foreach (int id in pingIds)
                    arr.Add(id);
                return Links
                    .Find(Query.In("PingId", arr))
                    .OrderBy(l => l.User)
                    .ToArray();
            }
        }

        public Link[] GetLinks(string user = null, HashSet<string> guilds = null)
        {
            if(string.IsNullOrEmpty(user))
            {
                if (guilds != null && guilds.Any())
                {
                    var arr = new BsonArray();
                    foreach (string guild in guilds)
                        arr.Add(guild);
                    return Links
                        .Find(Query.In("Guild", arr))
                        .OrderByDescending(l => l.PingId)
                        .ToArray();
                }
                else
                {
                    return Links
                        .FindAll()
                        .OrderByDescending(l => l.PingId)
                        .ToArray();
                }
            }
            else
            {
                if (guilds != null && guilds.Any())
                {
                    return Links
                        .Find(Query.EQ("User", user))
                        .Where(l => guilds.Contains(l.Guild))
                        .OrderByDescending(l => l.PingId)
                        .ToArray();
                }
                else
                {
                    return Links
                        .Find(Query.EQ("User", user))
                        .OrderByDescending(l => l.PingId)
                        .ToArray();
                }
            }
        }

        public LinkAction GetAction(int actionId)
        {
            return Actions.FindById(actionId);
        }

        public LinkAction[] GetActions(params int[] pingIds)
        {
            if (pingIds.Length == 0)
            {
                return new LinkAction[0];
            }
            else if (pingIds.Length == 1)
            {
                return Actions
                    .Find(Query.EQ("PingId", pingIds[0]))
                    .OrderByDescending(a => a.When)
                    .ToArray();
            }
            else
            {
                var arr = new BsonArray();
                foreach (int id in pingIds)
                    arr.Add(id);
                return Actions
                    .Find(Query.In("PingId", arr))
                    .OrderByDescending(a => a.When)
                    .ToArray();
            }
        }

        public LinkAction[] GetActions(params Guid[] linkIds)
        {
            if (linkIds.Length == 0)
            {
                return new LinkAction[0];
            }
            else if (linkIds.Length == 1)
            {
                return Actions
                    .Find(Query.EQ("LinkId", linkIds[0]))
                    .OrderByDescending(a => a.When)
                    .ToArray();
            }
            else
            {
                var arr = new BsonArray();
                foreach (Guid id in linkIds)
                    arr.Add(id);
                return Actions
                    .Find(Query.In("LinkId", arr))
                    .OrderByDescending(a => a.When)
                    .ToArray();
            }
        }

        public LinkAction[] GetActions(string user = null, HashSet<string> guilds = null)
        {
            if(string.IsNullOrEmpty(user))
            {
                if (guilds != null && guilds.Any())
                {
                    var arr = new BsonArray();
                    foreach (string guild in guilds)
                        arr.Add(guild);
                    return Actions
                        .Find(Query.In("Guild", arr))
                        .OrderByDescending(a => a.When)
                        .ToArray();
                }
                else
                {
                    return Actions
                        .FindAll()
                        .OrderByDescending(a => a.When)
                        .ToArray();
                }
            }
            else
            {
                if (guilds != null && guilds.Any())
                {
                    return Actions
                        .Find(Query.EQ("User", user))
                        .Where(a => guilds.Contains(a.Guild))
                        .OrderByDescending(a => a.When)
                        .ToArray();
                }
                else
                {
                    return Actions
                        .Find(Query.EQ("User", user))
                        .OrderByDescending(a => a.When)
                        .ToArray();
                }
            }
        }
    }
}
