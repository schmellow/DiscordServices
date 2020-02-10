using System;
using System.Text.RegularExpressions;

namespace Schmellow.DiscordServices.Tracker
{
    public static class StringExtensions
    {
        public static string ToShortString(this Guid id)
        {
            return id.ToString().ToLowerInvariant().Replace("-", "");
        }

        public static string ShortenTo(this string text, int characters)
        {
            if (text.Length > characters)
                return text.Substring(0, characters - 3).Trim() + "...";
            else
                return text;
        }

        public static string Html(this string text)
        {
            text = text.Replace("\n", "<br/>");
            return text;
        }

        public static string Linkify(this string text)
        {
            text = Regex.Replace(
                text,
                @"((www\.|(http|https|ftp|news|file)+\:\/\/)[&#95;.a-z0-9-]+\.[a-z0-9\/&#95;:@=.+?,##%&~-]*[^.|\'|\# |!|\(|?|,| |>|<|;|\)])",
                "<a href='$1' target='_blank'>$1</a>",
                RegexOptions.IgnoreCase)
                .Replace("href='www", "href='http://www");
            return text;
        }
    }
}
