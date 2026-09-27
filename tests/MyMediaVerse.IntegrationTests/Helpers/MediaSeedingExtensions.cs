using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.IntegrationTests.Helpers
{
    /// <summary>
    /// Creates the media items a test needs in place before it exercises something else (a mixlist,
    /// a topic lookup, a delete). Items are created through the API rather than written to the
    /// database, so topics and genres are normalized and linked exactly as they are for real data.
    /// </summary>
    public static class MediaSeedingExtensions
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Creates an article through <c>POST /api/article</c> and returns it. Throws when the
        /// article was not created, so a failed seed is reported where it happened instead of
        /// surfacing later as a null or a wrong status in the test that depended on it.
        /// An article's link is its identity: two seeds in one test must not share one.
        /// </summary>
        public static async Task<ArticleResponseDto> CreateArticleAsync(
            this HttpClient client,
            string title,
            string[]? topics = null,
            string[]? genres = null,
            string? link = null,
            string? description = null)
        {
            var dto = new CreateArticleDto
            {
                Title = title,
                Link = link,
                Description = description,
                Status = Status.Uncharted,
                Topics = topics ?? Array.Empty<string>(),
                Genres = genres ?? Array.Empty<string>()
            };

            var response = await client.PostAsJsonAsync("/api/article", dto, JsonOptions);
            if (response.StatusCode != HttpStatusCode.Created)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException(
                    $"Seeding article '{title}' failed: {(int)response.StatusCode} {response.StatusCode}. {body}");
            }

            var created = await response.Content.ReadFromJsonAsync<ArticleResponseDto>(JsonOptions);
            return created ?? throw new InvalidOperationException(
                $"Seeding article '{title}' returned an empty body.");
        }
    }
}
