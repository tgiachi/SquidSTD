using DryIoc;
using SquidStd.Core.Interfaces.Serialization;
using SquidStd.Core.Json;
using SquidStd.Persistence.Abstractions.Interfaces.Persistence;
using SquidStd.Persistence.Extensions;
using SquidStd.Persistence.Services;

namespace SquidStd.Tests.Persistence;

public class PersistenceRegistrationExtensionsTests
{
    private sealed class Player
    {
        public int Id { get; set; }
    }

    [Fact]
    public void RegisterPersistedEntity_PopulatesRegistry()
    {
        using var container = new Container();
        var serializer = new JsonDataSerializer();
        container.RegisterInstance<IDataSerializer>(serializer);
        container.RegisterInstance<IDataDeserializer>(serializer);
        container.Register<IPersistenceEntityRegistry, PersistenceEntityRegistry>(Reuse.Singleton);

        container.RegisterPersistedEntity<Player, int>(1, "Player", 1, p => p.Id);
        container.ApplyPersistedEntityRegistrations();

        var registry = container.Resolve<IPersistenceEntityRegistry>();
        Assert.True(registry.IsRegistered<Player, int>());
        Assert.Equal("Player", registry.GetDescriptor(1).TypeName);
    }
}
