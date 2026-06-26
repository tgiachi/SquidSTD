using Microsoft.CodeAnalysis;

namespace SquidStd.Generators.Events;

[Generator(LanguageNames.CSharp)]
public sealed class EventListenerRegistrationGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
    }
}
