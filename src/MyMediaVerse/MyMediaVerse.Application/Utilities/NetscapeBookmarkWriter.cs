using System.Net;
using System.Text;
using MyMediaVerse.Domain.Entities;

namespace MyMediaVerse.Application.Utilities
{
    /// <summary>
    /// Writes websites as a Netscape bookmark file so the library can be taken back into any
    /// browser or bookmark manager. Flat list (no folders); topics travel as the <c>TAGS</c>
    /// attribute, which the importers that support tags read back.
    /// </summary>
    public static class NetscapeBookmarkWriter
    {
        public const string ContentType = "text/html";

        public static string Write(IEnumerable<Website> websites, string title = "MyMediaVerse Bookmarks")
        {
            var builder = new StringBuilder();
            builder.AppendLine("<!DOCTYPE NETSCAPE-Bookmark-file-1>");
            builder.AppendLine("<!-- This is an automatically generated file. It will be read and overwritten. DO NOT EDIT! -->");
            builder.AppendLine("<META HTTP-EQUIV=\"Content-Type\" CONTENT=\"text/html; charset=UTF-8\">");
            builder.Append("<TITLE>").Append(WebUtility.HtmlEncode(title)).AppendLine("</TITLE>");
            builder.Append("<H1>").Append(WebUtility.HtmlEncode(title)).AppendLine("</H1>");
            builder.AppendLine("<DL><p>");

            foreach (var website in websites.Where(w => !string.IsNullOrWhiteSpace(w.Link)))
            {
                var addDate = new DateTimeOffset(DateTime.SpecifyKind(website.DateAdded, DateTimeKind.Utc)).ToUnixTimeSeconds();
                var tags = string.Join(",", website.Topics.Select(t => t.Name).Where(n => !string.IsNullOrWhiteSpace(n)));

                builder.Append("    <DT><A HREF=\"").Append(WebUtility.HtmlEncode(website.Link!.Trim()))
                    .Append("\" ADD_DATE=\"").Append(addDate).Append('"');
                if (tags.Length > 0)
                {
                    builder.Append(" TAGS=\"").Append(WebUtility.HtmlEncode(tags)).Append('"');
                }
                builder.Append('>').Append(WebUtility.HtmlEncode(website.Title ?? string.Empty)).AppendLine("</A>");
            }

            builder.AppendLine("</DL><p>");
            return builder.ToString();
        }

        public static string FileName(DateTime utcNow) => $"mymediaverse-bookmarks-{utcNow:yyyyMMdd}.html";
    }
}
