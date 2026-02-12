using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Pricing.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection Decorate<TInterface, TDecorator>(this IServiceCollection services)
        where TInterface : class
        where TDecorator : class, TInterface
    {
        var wrappedDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(TInterface));
        
        if (wrappedDescriptor is null)
        {
            throw new InvalidOperationException($"{typeof(TInterface).Name} is not registered");
        }

        var objectFactory = ActivatorUtilities.CreateFactory(typeof(TDecorator), [typeof(TInterface)]);

        var newDescriptor = ServiceDescriptor.Describe(
            typeof(TInterface),
            provider => (TInterface)objectFactory(provider, [provider.CreateInstance(wrappedDescriptor)])!,
            wrappedDescriptor.Lifetime);

        services.RemoveAll(typeof(TInterface));
        services.Add(newDescriptor);

        return services;
    }

    private static object? CreateInstance(this IServiceProvider provider, ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationInstance is not null)
        {
            return descriptor.ImplementationInstance;
        }

        if (descriptor.ImplementationFactory is not null)
        {
            return descriptor.ImplementationFactory(provider);
        }

        return ActivatorUtilities.GetServiceOrCreateInstance(provider, descriptor.ImplementationType!);
    }
}
