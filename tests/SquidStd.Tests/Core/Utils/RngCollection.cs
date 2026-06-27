namespace SquidStd.Tests.Core.Utils;

/// <summary>
///     Serializes tests that exercise the global <see cref="SquidStd.Core.Utils.BuiltInRng" /> state so a
///     seeded sequence is not perturbed by concurrent consumers.
/// </summary>
[CollectionDefinition("BuiltInRng")]
public sealed class RngCollection
{
}
