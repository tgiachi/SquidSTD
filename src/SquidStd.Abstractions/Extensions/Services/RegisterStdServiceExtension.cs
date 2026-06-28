using DryIoc;
using SquidStd.Abstractions.Data.Internal.Services;
using SquidStd.Abstractions.Extensions.Container;

namespace SquidStd.Abstractions.Extensions.Services;

public static class RegisterStdServiceExtension
{
    extension(IContainer container)
    {
        public IContainer RegisterStdService<TService, TImplementation>(int priority = 0)
            where TService : class
            where TImplementation : class, TService
        {
            container.Register<TService, TImplementation>(Reuse.Singleton);

            container.AddToRegisterTypedList(
                new ServiceRegistrationData(typeof(TService), typeof(TImplementation), priority)
            );

            return container;
        }
    }
}
