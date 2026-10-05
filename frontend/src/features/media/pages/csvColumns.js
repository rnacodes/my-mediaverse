// The columns the CSV upload reads, one list per media type. Keep these in step with the
// row parsers in UploadController: a column listed here must be one the parser reads.

export const CSV_MEDIA_TYPES = [
    { value: 'Book', label: 'Book' },
    { value: 'Movie', label: 'Movie' },
    { value: 'TVShow', label: 'TV Show' },
    { value: 'Video', label: 'Video' },
];

const OWNERSHIP_STATUS = { name: 'OwnershipStatus', description: 'Own, Rented or Streamed' };

// Read for every media type.
export const SHARED_COLUMNS = [
    { name: 'Title', description: 'The title of the item' },
    { name: 'Description', description: 'A description of the item' },
    { name: 'Link', description: 'URL of the item' },
    { name: 'Thumbnail', description: 'URL of a cover or thumbnail image' },
    { name: 'Notes', description: 'Your own notes' },
    { name: 'Status', description: 'Uncharted, ActivelyExploring, Completed or Abandoned' },
    { name: 'Rating', description: 'SuperLike, Like, Neutral or Dislike' },
    { name: 'DateCompleted', description: 'A date such as 2024-01-15' },
    { name: 'Topics', description: 'Topic names separated by semicolons' },
    { name: 'Genres', description: 'Genre names separated by semicolons' },
];

export const TYPE_COLUMNS = {
    Book: {
        columns: [
            { name: 'Author', description: 'The author’s name' },
            { name: 'ISBN', description: 'ISBN-10 or ISBN-13' },
            { name: 'ASIN', description: 'Amazon identifier' },
            { name: 'Format', description: 'Digital, Physical or Audiobook' },
            { name: 'PartOfSeries', description: 'TRUE or FALSE' },
            { name: 'Publisher', description: 'The publisher’s name' },
            { name: 'YearPublished', description: 'A year such as 1999' },
            { name: 'DateRead', description: 'A date such as 2024-01-15' },
            { name: 'MyReview', description: 'Your review of the book' },
            { name: 'GoodreadsRating', description: 'A number from 0 to 5' },
            OWNERSHIP_STATUS,
        ],
    },
    Movie: {
        note: 'A row with a TmdbId is filled in from TMDB: only your own columns (Status, Rating, '
            + 'OwnershipStatus, DateCompleted, Notes, Topics, Genres) are read from the file. '
            + 'A row without one is stored as typed.',
        columns: [
            { name: 'TmdbId', description: 'The movie’s id on The Movie Database' },
            { name: 'ImdbId', description: 'The movie’s id on IMDb' },
            { name: 'Director', description: 'The director’s name' },
            { name: 'Cast', description: 'Cast members' },
            { name: 'ReleaseYear', description: 'A year such as 1999' },
            { name: 'RuntimeMinutes', description: 'Length in minutes' },
            { name: 'MpaaRating', description: 'Such as PG-13' },
            { name: 'Tagline', description: 'The movie’s tagline' },
            { name: 'Homepage', description: 'URL of the official site' },
            { name: 'OriginalLanguage', description: 'Such as en' },
            { name: 'OriginalTitle', description: 'The title in its original language' },
            { name: 'TmdbRating', description: 'A number from 0 to 10' },
            OWNERSHIP_STATUS,
        ],
    },
    TVShow: {
        note: 'A row with a TmdbId is filled in from TMDB: only your own columns (Status, Rating, '
            + 'OwnershipStatus, DateCompleted, Notes, Topics, Genres) are read from the file. '
            + 'A row without one is stored as typed.',
        columns: [
            { name: 'TmdbId', description: 'The show’s id on The Movie Database' },
            { name: 'Creator', description: 'The creator’s name' },
            { name: 'Cast', description: 'Cast members' },
            { name: 'FirstAirYear', description: 'A year such as 2008' },
            { name: 'LastAirYear', description: 'A year such as 2013' },
            { name: 'NumberOfSeasons', description: 'A whole number' },
            { name: 'NumberOfEpisodes', description: 'A whole number' },
            { name: 'ContentRating', description: 'Such as TV-MA' },
            { name: 'Tagline', description: 'The show’s tagline' },
            { name: 'Homepage', description: 'URL of the official site' },
            { name: 'OriginalLanguage', description: 'Such as en' },
            { name: 'OriginalName', description: 'The name in its original language' },
            { name: 'TmdbRating', description: 'A number from 0 to 10' },
            OWNERSHIP_STATUS,
        ],
    },
    Video: {
        note: 'A video row needs a Link as well as a Title.',
        columns: [
            { name: 'Platform', description: 'Where the video lives; YouTube when left out' },
            { name: 'VideoId', description: 'The video’s id on its platform (ExternalId also works); taken from a YouTube link when left out' },
            { name: 'ChannelId', description: 'The YouTube channel id; links the video to that channel when it is already in the library' },
            { name: 'LengthInSeconds', description: 'Length in seconds (DurationInSeconds also works)' },
        ],
    },
};
