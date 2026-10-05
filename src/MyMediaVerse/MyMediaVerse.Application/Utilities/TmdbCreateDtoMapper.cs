using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Utilities
{
    /// <summary>
    /// Turns an item mapped from TMDB into the create request the movie and TV show services take,
    /// so every TMDB import path stores the same fields.
    /// </summary>
    public static class TmdbCreateDtoMapper
    {
        public static CreateMovieDto ToCreateDto(Movie movie) => new()
        {
            Title = movie.Title,
            Description = movie.Description,
            Thumbnail = movie.Thumbnail,
            Link = $"https://www.themoviedb.org/movie/{movie.TmdbId}",
            TmdbId = movie.TmdbId,
            TmdbRating = movie.TmdbRating,
            TmdbBackdropPath = movie.TmdbBackdropPath,
            Tagline = movie.Tagline,
            Homepage = movie.Homepage,
            OriginalLanguage = movie.OriginalLanguage,
            OriginalTitle = movie.OriginalTitle,
            ImdbId = movie.ImdbId,
            ReleaseYear = movie.ReleaseYear,
            RuntimeMinutes = movie.RuntimeMinutes,
            Director = movie.Director,
            Cast = movie.Cast,
            MpaaRating = movie.MpaaRating,
            Genres = movie.Genres.Select(g => g.Name).ToArray(),
            Status = Status.Uncharted,
            MediaType = MediaType.Movie
        };

        public static CreateTvShowDto ToCreateDto(TvShow tvShow) => new()
        {
            Title = tvShow.Title,
            Description = tvShow.Description,
            Thumbnail = tvShow.Thumbnail,
            Link = $"https://www.themoviedb.org/tv/{tvShow.TmdbId}",
            TmdbId = tvShow.TmdbId,
            TmdbRating = tvShow.TmdbRating,
            TmdbPosterPath = tvShow.TmdbPosterPath,
            Tagline = tvShow.Tagline,
            Homepage = tvShow.Homepage,
            OriginalLanguage = tvShow.OriginalLanguage,
            OriginalName = tvShow.OriginalName,
            FirstAirYear = tvShow.FirstAirYear,
            LastAirYear = tvShow.LastAirYear,
            NumberOfSeasons = tvShow.NumberOfSeasons,
            NumberOfEpisodes = tvShow.NumberOfEpisodes,
            Creator = tvShow.Creator,
            Cast = tvShow.Cast,
            ContentRating = tvShow.ContentRating,
            Genres = tvShow.Genres.Select(g => g.Name).ToArray(),
            Status = Status.Uncharted,
            MediaType = MediaType.TVShow
        };
    }
}
