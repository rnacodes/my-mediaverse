using AwesomeAssertions;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class VideoDuplicateFinderTests : InMemoryDbTestBase
    {
        private const string VideoId = "abc123DEF45";
        private const string OtherVideoId = "zzz999YYY88";

        private Video AddVideo(string title, string platform = "YouTube", string? externalId = null,
            string? link = null, Guid? channelId = null)
        {
            var video = new Video
            {
                Id = Guid.NewGuid(),
                Title = title,
                Platform = platform,
                ExternalId = externalId,
                Link = link,
                ChannelId = channelId,
                MediaType = MediaType.Video
            };
            Context.Videos.Add(video);
            return video;
        }

        private Task<Video?> Find(VideoIdentity identity) =>
            VideoDuplicateFinder.FindExistingAsync(Context.Videos, identity);

        #region Probe order

        [Fact]
        public async Task FindExistingAsync_TheIdWinsOverEveryWeakerKey()
        {
            var channelId = Guid.NewGuid();
            var byId = AddVideo("Renamed On YouTube", externalId: VideoId);
            AddVideo("Same Title", link: "https://vimeo.com/76979871", channelId: channelId);
            await Context.SaveChangesAsync();

            var match = await Find(new VideoIdentity
            {
                Platform = "YouTube",
                ExternalId = VideoId,
                Link = "https://vimeo.com/76979871",
                Title = "Same Title",
                ChannelId = channelId
            });

            match!.Id.Should().Be(byId.Id);
        }

        [Fact]
        public async Task FindExistingAsync_TheLinkWinsOverTheTitle()
        {
            var channelId = Guid.NewGuid();
            var byLink = AddVideo("Another Title", platform: "Vimeo", link: "https://vimeo.com/76979871");
            AddVideo("Same Title", platform: "Vimeo", channelId: channelId);
            await Context.SaveChangesAsync();

            var match = await Find(new VideoIdentity
            {
                Platform = "Vimeo",
                Link = "https://vimeo.com/76979871",
                Title = "Same Title",
                ChannelId = channelId
            });

            match!.Id.Should().Be(byLink.Id);
        }

        #endregion

        #region Id

        [Theory]
        [InlineData("YouTube")]
        [InlineData("youtube")]
        [InlineData(" YOUTUBE ")]
        public async Task FindExistingAsync_MatchesTheIdOnItsPlatform_WhateverThePlatformCasing(string platform)
        {
            var stored = AddVideo("Stored", externalId: VideoId);
            await Context.SaveChangesAsync();

            var match = await Find(new VideoIdentity { Platform = platform, ExternalId = $" {VideoId} " });

            match!.Id.Should().Be(stored.Id);
        }

        [Fact]
        public async Task FindExistingAsync_TheSameIdOnAnotherPlatform_DoesNotMatch()
        {
            AddVideo("On Vimeo", platform: "Vimeo", externalId: "76979871");
            await Context.SaveChangesAsync();

            var match = await Find(new VideoIdentity { Platform = "Twitch", ExternalId = "76979871" });

            match.Should().BeNull();
        }

        [Fact]
        public async Task FindExistingAsync_YouTubeIdsAreCaseSensitive()
        {
            AddVideo("Stored", externalId: VideoId);
            await Context.SaveChangesAsync();

            var match = await Find(new VideoIdentity { Platform = "YouTube", ExternalId = VideoId.ToLower() });

            match.Should().BeNull();
        }

        #endregion

        #region YouTube link

        [Theory]
        [InlineData("https://www.youtube.com/watch?v=abc123DEF45")]
        [InlineData("https://youtu.be/abc123DEF45")]
        [InlineData("https://www.youtube.com/shorts/abc123DEF45")]
        [InlineData("https://www.youtube.com/watch?list=PLxyz&v=abc123DEF45&t=30s")]
        public async Task FindExistingAsync_AYouTubeLink_MatchesTheRowHoldingItsId(string link)
        {
            var stored = AddVideo("Stored", externalId: VideoId);
            await Context.SaveChangesAsync();

            var match = await Find(new VideoIdentity { Platform = "Other", Link = link });

            match!.Id.Should().Be(stored.Id);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task FindExistingAsync_ALegacyRowWithoutAnId_IsFoundByTheIdInItsLink(string? storedId)
        {
            var legacy = AddVideo("Legacy", externalId: storedId, link: "https://youtu.be/abc123DEF45?si=share");
            AddVideo("Unrelated", link: "https://youtu.be/" + OtherVideoId);
            await Context.SaveChangesAsync();

            var match = await Find(new VideoIdentity
            {
                Platform = "YouTube",
                ExternalId = VideoId,
                Link = "https://www.youtube.com/watch?v=abc123DEF45"
            });

            match!.Id.Should().Be(legacy.Id);
        }

        [Fact]
        public async Task FindExistingAsync_ALegacyRowIsFoundFromTheIdAlone_WhenThePlatformIsYouTube()
        {
            var legacy = AddVideo("Legacy", link: "https://www.youtube.com/watch?v=abc123DEF45");
            await Context.SaveChangesAsync();

            var match = await Find(new VideoIdentity { Platform = "YouTube", ExternalId = VideoId });

            match!.Id.Should().Be(legacy.Id);
        }

        [Fact]
        public async Task FindExistingAsync_ALinkThatOnlyMentionsTheId_IsNotALegacyMatch()
        {
            AddVideo("Article about it", platform: "Other", link: "https://example.com/about/abc123DEF45");
            await Context.SaveChangesAsync();

            var match = await Find(new VideoIdentity { Platform = "YouTube", ExternalId = VideoId });

            match.Should().BeNull();
        }

        [Fact]
        public async Task FindExistingAsync_AYouTubeLinkDifferingOnlyInCase_DoesNotMatch()
        {
            AddVideo("Legacy", link: "https://www.youtube.com/watch?v=" + VideoId.ToLower());
            await Context.SaveChangesAsync();

            var match = await Find(new VideoIdentity
            {
                Platform = "YouTube",
                Link = "https://www.youtube.com/watch?v=" + VideoId
            });

            match.Should().BeNull();
        }

        #endregion

        #region Any other link

        [Theory]
        [InlineData("https://vimeo.com/76979871")]
        [InlineData("http://www.vimeo.com/76979871/")]
        [InlineData("https://VIMEO.com/76979871?utm_source=newsletter#t=10")]
        public async Task FindExistingAsync_TheSameLink_MatchesIgnoringSchemeWwwTrackingAndFragment(string probe)
        {
            var stored = AddVideo("On Vimeo", platform: "Vimeo", link: "https://vimeo.com/76979871");
            await Context.SaveChangesAsync();

            var match = await Find(new VideoIdentity { Platform = "Vimeo", Link = probe });

            match!.Id.Should().Be(stored.Id);
        }

        [Fact]
        public async Task FindExistingAsync_TheSameLink_RefusesARowKnownByADifferentId()
        {
            AddVideo("On Vimeo", platform: "Vimeo", externalId: "11111", link: "https://vimeo.com/76979871");
            await Context.SaveChangesAsync();

            var match = await Find(new VideoIdentity
            {
                Platform = "Vimeo",
                ExternalId = "22222",
                Link = "https://vimeo.com/76979871"
            });

            match.Should().BeNull();
        }

        #endregion

        #region Title

        [Fact]
        public async Task FindExistingAsync_TitlePlatformAndChannel_Match_IgnoringTitleCase()
        {
            var channelId = Guid.NewGuid();
            var stored = AddVideo("My Favorite Talk", channelId: channelId);
            AddVideo("My Favorite Talk", channelId: Guid.NewGuid());
            await Context.SaveChangesAsync();

            var match = await Find(new VideoIdentity
            {
                Platform = "YouTube",
                Title = " my favorite TALK ",
                ChannelId = channelId
            });

            match!.Id.Should().Be(stored.Id);
        }

        [Fact]
        public async Task FindExistingAsync_TheTitleAlone_NeverMatches()
        {
            AddVideo("Introduction");
            AddVideo("Introduction", channelId: Guid.NewGuid());
            await Context.SaveChangesAsync();

            var match = await Find(new VideoIdentity { Platform = "YouTube", Title = "Introduction" });

            match.Should().BeNull();
        }

        [Fact]
        public async Task FindExistingAsync_TheTitle_DoesNotMatchAcrossPlatforms()
        {
            var channelId = Guid.NewGuid();
            AddVideo("My Favorite Talk", platform: "Vimeo", channelId: channelId);
            await Context.SaveChangesAsync();

            var match = await Find(new VideoIdentity
            {
                Platform = "YouTube",
                Title = "My Favorite Talk",
                ChannelId = channelId
            });

            match.Should().BeNull();
        }

        [Fact]
        public async Task FindExistingAsync_TheTitle_RefusesARowKnownByADifferentId()
        {
            // Two uploads of one channel can share a title ("Live Q&A"); their ids tell them apart.
            var channelId = Guid.NewGuid();
            AddVideo("Live Q&A", externalId: OtherVideoId, channelId: channelId);
            await Context.SaveChangesAsync();

            var match = await Find(new VideoIdentity
            {
                Platform = "YouTube",
                ExternalId = VideoId,
                Title = "Live Q&A",
                ChannelId = channelId
            });

            match.Should().BeNull();
        }

        [Fact]
        public async Task FindExistingAsync_TheTitle_MatchesARowThatHasNoIdYet()
        {
            var channelId = Guid.NewGuid();
            var stored = AddVideo("Live Q&A", channelId: channelId);
            await Context.SaveChangesAsync();

            var match = await Find(new VideoIdentity
            {
                Platform = "YouTube",
                ExternalId = VideoId,
                Title = "Live Q&A",
                ChannelId = channelId
            });

            match!.Id.Should().Be(stored.Id);
        }

        #endregion

        #region Nothing to go on

        [Fact]
        public async Task FindExistingAsync_ReturnsNull_ForAnEmptyIdentity()
        {
            AddVideo("Stored", externalId: VideoId, link: "https://youtu.be/" + VideoId);
            await Context.SaveChangesAsync();

            (await Find(new VideoIdentity())).Should().BeNull();
            (await Find(new VideoIdentity { Platform = "YouTube", ExternalId = "  ", Link = " " })).Should().BeNull();
        }

        #endregion

        #region Helpers

        [Theory]
        [InlineData(null, null)]
        [InlineData("", null)]
        [InlineData("   ", null)]
        [InlineData(" abc123DEF45 ", "abc123DEF45")]
        public void NormalizeExternalId_TrimsAndTurnsBlankIntoNull(string? input, string? expected)
        {
            VideoDuplicateFinder.NormalizeExternalId(input).Should().Be(expected);
        }

        [Theory]
        [InlineData("https://www.youtube.com/watch?v=abc123DEF45", "abc123DEF45")]
        [InlineData("https://youtu.be/abc123DEF45", "abc123DEF45")]
        [InlineData("https://vimeo.com/76979871", null)]
        [InlineData("abc123DEF45", null)]
        [InlineData("", null)]
        [InlineData(null, null)]
        public void ExtractYouTubeId_OnlyReadsYouTubeLinks(string? link, string? expected)
        {
            VideoDuplicateFinder.ExtractYouTubeId(link).Should().Be(expected);
        }

        #endregion

        #region Absorb

        [Fact]
        public void AbsorbIdentity_FillsTheIdLinkAndChannel_WhenTheyAreMissing()
        {
            var channelId = Guid.NewGuid();
            var existing = new Video { Title = "Stored", Platform = "YouTube" };

            var changed = VideoDuplicateFinder.AbsorbIdentity(existing, new VideoIdentity
            {
                Platform = "youtube",
                ExternalId = $" {VideoId} ",
                Link = "https://www.youtube.com/watch?v=" + VideoId,
                ChannelId = channelId
            });

            changed.Should().BeTrue();
            existing.ExternalId.Should().Be(VideoId);
            existing.Link.Should().Be("https://www.youtube.com/watch?v=" + VideoId);
            existing.ChannelId.Should().Be(channelId);
        }

        [Fact]
        public void AbsorbIdentity_NeverOverwritesAValueAlreadyPresent()
        {
            var storedChannel = Guid.NewGuid();
            var existing = new Video
            {
                Title = "Stored",
                Platform = "YouTube",
                ExternalId = OtherVideoId,
                Link = "https://youtu.be/" + OtherVideoId,
                ChannelId = storedChannel
            };

            var changed = VideoDuplicateFinder.AbsorbIdentity(existing, new VideoIdentity
            {
                Platform = "YouTube",
                ExternalId = VideoId,
                Link = "https://youtu.be/" + VideoId,
                Title = "Incoming",
                ChannelId = Guid.NewGuid()
            });

            changed.Should().BeFalse();
            existing.ExternalId.Should().Be(OtherVideoId);
            existing.Link.Should().Be("https://youtu.be/" + OtherVideoId);
            existing.ChannelId.Should().Be(storedChannel);
            existing.Title.Should().Be("Stored");
            existing.Platform.Should().Be("YouTube");
        }

        [Fact]
        public void AbsorbIdentity_DoesNotTakeAnIdFromAnotherPlatform()
        {
            var existing = new Video { Title = "Stored", Platform = "Other", Link = "https://youtu.be/" + VideoId };

            var changed = VideoDuplicateFinder.AbsorbIdentity(existing, new VideoIdentity
            {
                Platform = "YouTube",
                ExternalId = VideoId,
                Link = "https://www.youtube.com/watch?v=" + VideoId
            });

            changed.Should().BeFalse();
            existing.ExternalId.Should().BeNull();
            existing.Platform.Should().Be("Other");
        }

        [Fact]
        public void AbsorbMetadata_FillsBlankFieldsOnly_AndLeavesTheUsersOwnFieldsAlone()
        {
            var published = new DateTime(2024, 3, 1, 12, 0, 0, DateTimeKind.Utc);
            var existing = new Video
            {
                Title = "My Title",
                Platform = "YouTube",
                Description = "My description",
                Notes = "My notes",
                Status = Status.Completed
            };

            var changed = VideoDuplicateFinder.AbsorbMetadata(existing, new Video
            {
                Title = "Incoming Title",
                Platform = "YouTube",
                Description = "Incoming description",
                Thumbnail = "https://i.ytimg.com/vi/abc123DEF45/hqdefault.jpg",
                LengthInSeconds = 212,
                PublishedAt = published,
                Notes = "Incoming notes",
                Status = Status.Uncharted
            });

            changed.Should().BeTrue();
            existing.Thumbnail.Should().Be("https://i.ytimg.com/vi/abc123DEF45/hqdefault.jpg");
            existing.LengthInSeconds.Should().Be(212);
            existing.PublishedAt.Should().Be(published);
            existing.Title.Should().Be("My Title");
            existing.Description.Should().Be("My description");
            existing.Notes.Should().Be("My notes");
            existing.Status.Should().Be(Status.Completed);
        }

        [Fact]
        public void AbsorbMetadata_ReportsNoChange_WhenNothingIsMissing()
        {
            var existing = new Video
            {
                Title = "Stored",
                Platform = "YouTube",
                Description = "Stored description",
                Thumbnail = "https://example.com/mine.jpg",
                LengthInSeconds = 100,
                PublishedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };

            var changed = VideoDuplicateFinder.AbsorbMetadata(existing, new Video
            {
                Title = "Incoming",
                Platform = "YouTube",
                Description = "Incoming description",
                Thumbnail = "https://i.ytimg.com/vi/abc123DEF45/hqdefault.jpg",
                LengthInSeconds = 212,
                PublishedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });

            changed.Should().BeFalse();
            existing.Thumbnail.Should().Be("https://example.com/mine.jpg");
            existing.LengthInSeconds.Should().Be(100);
        }

        #endregion
    }
}
