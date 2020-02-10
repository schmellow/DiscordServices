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
            Actions.EnsureIndex("PingId");
            Actions.EnsureIndex("LinkId");
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

        public List<Ping> GetPings(int offset = 0, int limit = 0)
        {
            var pings = Pings.Query()
                .OrderByDescending(p => p.Id)
                .Skip(offset);
            if (limit > 0)
                pings = pings.Limit(limit);
            return pings.ToList();
        }

        public int GetPingCount()
        {
            return Pings.Count();
        }

        public Link GetLink(Guid linkId)
        {
            return Links.FindById(linkId);
        }

        public List<Link> GetLinks(int pingId)
        {
            return Links.Query()
                .Where(l => l.PingId == pingId)
                .OrderBy(l => l.User)
                .ToList();
        }

        public LinkAction GetAction(int actionId)
        {
            return Actions.FindById(actionId);
        }

        public List<LinkAction> GetActions(int pingId)
        {
            return Actions.Query()
                .Where(a => a.PingId == pingId)
                .OrderByDescending(a => a.When)
                .ToList();
        }

        public List<LinkAction> GetActions(Guid linkId)
        {
            return Actions.Query()
                .Where(a => a.LinkId == linkId)
                .OrderByDescending(a => a.When)
                .ToList();
        }
    }
}
