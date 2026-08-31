using System.Globalization;
using Typesense;

namespace MyMediaVerse.Infrastructure.Services.Search
{
    /// <summary>
    /// Vector query for hybrid (keyword + vector) search: an empty vector tells Typesense to embed
    /// the query text itself, <c>k</c> bounds how many neighbors the vector leg contributes, and
    /// the distance threshold drops documents that are not actually similar to the query.
    /// </summary>
    /// <remarks>
    /// The pinned Typesense client (8.4.0) refuses to construct a <see cref="VectorQuery"/> with an
    /// empty vector unless an <c>id</c> is supplied, even though the server accepts exactly that
    /// form for hybrid search. This subclass passes a placeholder id purely to satisfy that check
    /// and overrides <see cref="ToQuery"/> - the only thing the client's JSON converter uses - to
    /// emit the id-less query Typesense expects.
    /// </remarks>
    internal sealed record HybridVectorQuery : VectorQuery
    {
        private const string PlaceholderId = "hybrid";

        private readonly int _k;
        private readonly double _distanceThreshold;

        public HybridVectorQuery(string vectorFieldName, int k, double distanceThreshold)
            : base(Array.Empty<float>(), vectorFieldName, id: PlaceholderId, k: k)
        {
            _k = k;
            _distanceThreshold = distanceThreshold;
        }

        /// <inheritdoc />
        public override string ToQuery() =>
            $"{VectorFieldName}:([],k:{_k},distance_threshold:{_distanceThreshold.ToString(CultureInfo.InvariantCulture)})";
    }
}
