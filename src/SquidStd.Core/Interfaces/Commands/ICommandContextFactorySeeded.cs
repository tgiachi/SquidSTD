namespace SquidStd.Core.Interfaces.Commands;

/// <summary>
///     Produces a <typeparamref name="TContext" /> from a seed (for example the connection a message
///     arrived on), so a dispatch can carry the context that belongs to that seed.
/// </summary>
/// <typeparam name="TContext">The context type produced.</typeparam>
/// <typeparam name="TSeed">The seed the context is built from.</typeparam>
public interface ICommandContextFactory<out TContext, in TSeed>
{
    /// <summary>Creates the context for the given seed.</summary>
    /// <param name="seed">The seed (for example the originating connection).</param>
    /// <returns>The context to pass to handlers.</returns>
    TContext Create(TSeed seed);
}
