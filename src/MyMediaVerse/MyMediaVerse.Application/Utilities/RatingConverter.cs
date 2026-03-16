using MyMediaVerse.Domain.Entities;

namespace MyMediaVerse.Application.Utilities
{
    /// <summary>
    /// Utility class for converting between different rating systems.
    /// </summary>
    public static class RatingConverter
    {
        /// <summary>
        /// Converts a Goodreads rating (1-5 scale) to PLB Rating enum.
        /// Conversion rules:
        /// - 5 = SuperLike
        /// - 4 = Like
        /// - 3 = Neutral
        /// - 1-2 = Dislike
        /// </summary>
        /// <param name="goodreadsRating">The Goodreads rating (1-5)</param>
        /// <returns>The corresponding PLB Rating enum value, or null if input is null</returns>
        public static Rating? ConvertGoodreadsRatingToPLBRating(decimal? goodreadsRating)
        {
            if (!goodreadsRating.HasValue)
            {
                return null;
            }

            return goodreadsRating.Value switch
            {
                5 => Rating.SuperLike,
                4 => Rating.Like,
                3 => Rating.Neutral,
                >= 1 and < 3 => Rating.Dislike,
                _ => null // Invalid rating
            };
        }
    }
}

