using SquidStd.Database.Abstractions.Types.Data;
using SquidStd.Database.Connection;

namespace SquidStd.Tests.Database;

public class ConnectionStringParserTests
{
    [Fact]
    public void Parse_SqliteRelativePath()
    {
        var result = ConnectionStringParser.Parse("sqlite://app.db");
        Assert.Equal(DatabaseProviderType.Sqlite, result.Provider);
        Assert.Equal("Data Source=app.db", result.NativeConnectionString);
    }

    [Fact]
    public void Parse_SqliteAbsolutePath()
    {
        var result = ConnectionStringParser.Parse("sqlite:///var/data/app.db");
        Assert.Equal("Data Source=/var/data/app.db", result.NativeConnectionString);
    }

    [Fact]
    public void Parse_SqliteInMemory()
    {
        var result = ConnectionStringParser.Parse("sqlite://:memory:");
        Assert.Equal("Data Source=:memory:", result.NativeConnectionString);
    }

    [Fact]
    public void Parse_Postgres_BuildsNativeString()
    {
        var result = ConnectionStringParser.Parse("postgres://user:pass@db.host:5433/appdb");
        Assert.Equal(DatabaseProviderType.Postgres, result.Provider);
        Assert.Equal("Host=db.host;Port=5433;Username=user;Password=pass;Database=appdb",
            result.NativeConnectionString);
    }

    [Fact]
    public void Parse_Postgres_DefaultsPortAndDecodesCredentials()
    {
        var result = ConnectionStringParser.Parse("postgresql://u%40corp:p%3Aw@h/appdb");
        Assert.Equal("Host=h;Port=5432;Username=u@corp;Password=p:w;Database=appdb",
            result.NativeConnectionString);
    }

    [Fact]
    public void Parse_MySql_BuildsNativeString()
    {
        var result = ConnectionStringParser.Parse("mysql://root:secret@127.0.0.1:3307/shop");
        Assert.Equal(DatabaseProviderType.MySql, result.Provider);
        Assert.Equal("Server=127.0.0.1;Port=3307;Uid=root;Pwd=secret;Database=shop",
            result.NativeConnectionString);
    }

    [Fact]
    public void Parse_SqlServer_BuildsNativeString()
    {
        var result = ConnectionStringParser.Parse("sqlserver://sa:Str0ng@localhost:1433/master");
        Assert.Equal(DatabaseProviderType.SqlServer, result.Provider);
        Assert.Equal("Server=localhost,1433;User Id=sa;Password=Str0ng;Database=master;TrustServerCertificate=true",
            result.NativeConnectionString);
    }

    [Fact]
    public void Parse_UnknownScheme_Throws()
    {
        Assert.Throws<NotSupportedException>(() => ConnectionStringParser.Parse("oracle://h/db"));
    }

    [Fact]
    public void Parse_MissingHostForServerProvider_Throws()
    {
        Assert.Throws<FormatException>(() => ConnectionStringParser.Parse("postgres:///db"));
    }
}
