namespace SquidStd.Tests.Crypto.Pgp.Support;

/// <summary>Shares one <see cref="PgpTestKeys" /> instance across the PGP test classes.</summary>
[CollectionDefinition("PgpKeys")]
public sealed class PgpKeysCollection : ICollectionFixture<PgpTestKeys>
{
}
