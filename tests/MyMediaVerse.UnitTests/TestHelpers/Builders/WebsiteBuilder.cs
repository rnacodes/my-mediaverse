using MyMediaVerse.Domain.Entities;
using MyMediaVerse.UnitTests.TestData;

namespace MyMediaVerse.UnitTests.TestHelpers.Builders
{
    public class WebsiteBuilder : MediaItemBuilder<Website>
    {
        public WebsiteBuilder() : base(TestDataFactory.CreateWebsite())
        {
        }

        public WebsiteBuilder WithDomain(string domain)
        {
            Item.Domain = domain;
            return this;
        }

        public WebsiteBuilder WithRssFeedUrl(string rssFeedUrl)
        {
            Item.RssFeedUrl = rssFeedUrl;
            return this;
        }

        public WebsiteBuilder WithAuthor(string author)
        {
            Item.Author = author;
            return this;
        }

        public WebsiteBuilder WithPublication(string publication)
        {
            Item.Publication = publication;
            return this;
        }

        public WebsiteBuilder WithLastCheckedDate(DateTime lastCheckedDate)
        {
            Item.LastCheckedDate = lastCheckedDate;
            return this;
        }
    }
}
