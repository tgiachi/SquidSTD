using SquidStd.Database.Abstractions.Types.Data;

namespace SquidStd.Database.Connection;

/// <summary>
/// The result of parsing a URI connection string: the provider and the native connection string.
/// </summary>
/// <param name="Provider">The resolved database provider.</param>
/// <param name="NativeConnectionString">The provider-native connection string for FreeSql.</param>
public sealed record ParsedConnection(DatabaseProviderType Provider, string NativeConnectionString);
