using Microsoft.Extensions.Logging;
using NSubstitute;

namespace MyMediaVerse.UnitTests.TestHelpers.Substitutes
{
    /// <summary>
    /// Tiny factory wrappers for the NSubstitute idioms repeated across the suite.
    /// </summary>
    public static class SubstituteFor
    {
        public static ILogger<T> Logger<T>() => Substitute.For<ILogger<T>>();

        public static TService Service<TService>() where TService : class
            => Substitute.For<TService>();
    }
}
