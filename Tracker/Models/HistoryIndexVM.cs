using System;
using System.Collections.Generic;

namespace Schmellow.DiscordServices.Tracker.Models
{
    public class HistoryIndexVM
    {
        public class HistoryPing
        {
            public int Id { get; set; }
            public DateTime Created { get; set; }
            public string Guild { get; set; }
            public string Author { get; set; }
            public string Text { get; set; }
            public int UserCount { get; set; }
            public int ActionCount { get; set; }
            public int ViewsCount { get; set; }
            public int OtherCount { get; set; }
        }

        public int PageNum { get; set; }
        public int TotalPages { get; set; }
        public List<HistoryPing> Pings { get; private set; }

        public HistoryIndexVM()
        {
            Pings = new List<HistoryPing>();
            PageNum = 1;
            TotalPages = 1;
        }

        public bool HasNextPage
        {
            get { return PageNum < TotalPages; }
        }

        public bool HasPreviousPage
        {
            get { return PageNum > 1; }
        }

        public string ParentPageArg
        {
            get
            {
                return PageNum > 1 ? "?parentPage=" + PageNum : "";
            }
        }

        public string NextPageArg
        {
            get
            {
                int next = PageNum + 1;
                return "?page=" + next;
            }
        }

        public string PreviousPageArg
        {
            get
            {
                int prev = PageNum - 1;
                return prev > 1 ? "?page=" + prev : "";
            }
        }
    }
}
