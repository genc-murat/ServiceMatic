using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;

namespace ServiceMatic;

/// <summary>
/// Contains extension methods for IServiceCollection to facilitate service registration.
/// </summary>
public static class ServiceMaticExtensions
{
    /// <summary>
    /// Registers services from a given assembly based on certain criteria like filters and lifetimes.
    /// </summary>
    /// <param name="services">The IServiceCollection to which services are to be added.</param>
    /// <param name="assembly">The assembly to scan for services.</param>
    /// <param name="filter">Optional filter to include only specific types.</param>
    /// <param name="lifetime">The default service lifetime.</param>
    /// <param name="dependencyGraph">Optional DependencyGraph for tracking service dependencies.</param>
    /// <param name="registerIfNoInterface">Indicates whether to register the service even if it doesn't implement any interfaces.</param>
    /// <param name="interfaceFilter">Optional filter function to include only specific interfaces for each type.</param>
    /// <returns>The updated IServiceCollection.</returns>
    /// <remarks>
    /// This method dynamically registers services by scanning an assembly. It supports multiple configuration options, 
    /// including type filtering, interface filtering, and dependency graph updating.
    /// </remarks>
    public static IServiceCollection AddServicesFromAssembly(this IServiceCollection services, Assembly assembly, Func<Type, bool>? filter = null,
ServiceLifetime lifetime = ServiceLifetime.Transient,
DependencyGraph? dependencyGraph = null,
bool registerIfNoInterface = false,
Func<Type, Type, bool>? interfaceFilter = null)
    {
        ArgumentNullException.ThrowIfNull(assembly, nameof(assembly));

        var typesFromAssembly = new HashSet<Type>(
            assembly.GetTypes()
            .Where(type => !type.IsAbstract && !type.IsInterface && (type.Namespace == null || !type.Namespace.StartsWith("System")))
            .Where(filter ?? (type => true)));

        foreach (var type in typesFromAssembly)
        {
            var interfaceTypes = type.GetInterfaces()
                                     .Where(i => (i.Namespace == null || !i.Namespace.StartsWith("System")) &&
                                                 (interfaceFilter == null || interfaceFilter(type, i)));

            if (!interfaceTypes.Any() && registerIfNoInterface)
            {
                services.Add(new ServiceDescriptor(type, type, lifetime));
            }
            else
            {
                foreach (var interfaceType in interfaceTypes)
                {
                    services.Add(new ServiceDescriptor(interfaceType, type, lifetime));
                }
            }

            if (type.IsGenericTypeDefinition)
            {
                // Register an open generic type
                services.Add(new ServiceDescriptor(type, type, lifetime));
            }

            UpdateDependencyGraph(type, dependencyGraph, services); // Update graph
        }

        return services;

    }

    /// <summary>
    /// Registers services from a given assembly that are annotated with the custom attribute RegisterServiceAttribute.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <param name="assembly">The assembly to scan for services.</param>
    /// <param name="defaultLifetime">The default lifetime for services if not specified in the attribute.</param>
    /// <param name="dependencyGraph">Optional DependencyGraph for more complex dependency tracking.</param>
    /// <param name="interfaceFilter">Optional filter function to include only specific interfaces when registering services.</param>
    /// <returns>The updated IServiceCollection.</returns>
    /// <remarks>
    /// This method dynamically registers services by scanning an assembly for types annotated with a custom attribute.
    /// It also supports optional interface filtering and dependency graph tracking.
    /// </remarks>
    public static IServiceCollection AddServicesWithAttribute(this IServiceCollection services,
     Assembly assembly,
     ServiceLifetime defaultLifetime = ServiceLifetime.Transient,
     DependencyGraph? dependencyGraph = null,
     Func<Type, Type, bool>? interfaceFilter = null)
    {
        // This would register services that are annotated with a custom attribute
        // RegisterServiceAttribute should specify things like lifetime of the service
        var attributedTypes = assembly.GetTypes()
            .Where(type => type.GetCustomAttribute(typeof(RegisterServiceAttribute)) != null);

        foreach (var type in attributedTypes)
        {
            var attribute = type.GetCustomAttribute<RegisterServiceAttribute>();
            var lifetime = attribute?.Lifetime ?? defaultLifetime;

            var interfaceTypes = type.GetInterfaces().Where(i => interfaceFilter == null || interfaceFilter(type, i));

            foreach (var interfaceType in interfaceTypes)
            {
                services.Add(new ServiceDescriptor(interfaceType, type, lifetime));
            }

            if (type.IsGenericTypeDefinition)
            {
                // Register an open generic type
                services.Add(new ServiceDescriptor(type, type, lifetime));
            }

            // Assume UpdateDependencyGraph updates a dependency graph
            // This is optional and can be used for more complex dependency tracking
            UpdateDependencyGraph(type, dependencyGraph, services);
        }

        return services;
    }

    /// <summary>
    /// Decorates an existing registered service of type TInterface using a decorator factory function.
    /// </summary>
    /// <typeparam name="TInterface">The type of the service to decorate.</typeparam>
    /// <param name="services">The IServiceCollection to update.</param>
    /// <param name="decoratorFactory">The factory function to create the decorator.</param>
    /// <returns>The updated IServiceCollection.</returns>
    /// <remarks>
    /// This method allows you to wrap an existing registered service with additional functionality, a common use case in the Decorator Pattern.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when the service of type TInterface is not registered or has multiple registrations.</exception>
    /// <exception cref="ArgumentNullException">Thrown when the decorator factory is null.</exception>
    public static IServiceCollection Decorate<TInterface>(
 this IServiceCollection services,
 Func<IServiceProvider, TInterface, TInterface> decoratorFactory)
 where TInterface : class
    {
        ArgumentNullException.ThrowIfNull(decoratorFactory, nameof(decoratorFactory));

        var wrappedDescriptors = services.Where(s => s.ServiceType == typeof(TInterface)).ToList();

        if (!wrappedDescriptors.Any())
        {
            throw new InvalidOperationException($"Service of type {typeof(TInterface).Name} is not registered.");
        }

        if (wrappedDescriptors.Count > 1)
        {
            throw new InvalidOperationException($"Multiple registrations for the service type {typeof(TInterface).Name} found. Cannot apply decorator.");
        }

        var wrappedDescriptor = wrappedDescriptors.First();
        services.Remove(wrappedDescriptor);

        services.Add(new ServiceDescriptor(typeof(TInterface), serviceProvider =>
        {
            var original = (TInterface)serviceProvider.CreateInstance(wrappedDescriptor.ImplementationType);
            return decoratorFactory(serviceProvider, original);
        }, wrappedDescriptor.Lifetime));

        return services;
    }

    /// <summary>
    /// Builds and returns a DependencyGraph from the registered services in the IServiceCollection.
    /// </summary>
    /// <param name="services">The IServiceCollection containing the registered services.</param>
    /// <returns>A DependencyGraph containing nodes for each registered service.</returns>
    /// <remarks>
    /// This method iterates through each ServiceDescriptor in the IServiceCollection, and adds a corresponding node to the DependencyGraph.
    /// </remarks>
    public static DependencyGraph GetDependencyGraph(this IServiceCollection services)
    {
        var graph = new DependencyGraph();
        foreach (var descriptor in services)
        {
            graph.AddService(descriptor.ServiceType);
        }
        return graph;
    }

    /// <summary>
    /// Creates an instance of the specified type using the provided IServiceProvider to resolve dependencies.
    /// </summary>
    /// <param name="serviceProvider">The IServiceProvider that will resolve the dependencies for the object being created.</param>
    /// <param name="type">The Type of the object to create.</param>
    /// <returns>A new instance of the specified type.</returns>
    /// <remarks>
    /// This method serves as a wrapper for the ActivatorUtilities.CreateInstance method, which uses the IServiceProvider to resolve dependencies when creating a new instance.
    /// </remarks>
    private static object CreateInstance(this IServiceProvider serviceProvider, Type type)
    {
        return ActivatorUtilities.CreateInstance(serviceProvider, type);
    }

    /// <summary>
    /// Counts the number of registered services in the IServiceCollection.
    /// </summary>
    /// <param name="services">The IServiceCollection containing the registered services.</param>
    /// <returns>The number of services registered in the IServiceCollection.</returns>
    /// <remarks>
    /// This is a simple utility method that returns the total number of services registered in the given IServiceCollection.
    /// </remarks>
    public static int CountRegisteredServices(this IServiceCollection services)
    {
        return services.Count();
    }

    /// <summary>
    /// Adds a batch of services to the IServiceCollection.
    /// </summary>
    /// <param name="services">The IServiceCollection to add services to.</param>
    /// <param name="batch">A list of tuples, where each tuple contains the service type, its corresponding implementation type, and the service's lifetime.</param>
    /// <returns>The updated IServiceCollection.</returns>
    /// <remarks>
    /// This method takes a batch of services and adds them to the IServiceCollection. 
    /// It checks to ensure that each implementation type is assignable from its service type. 
    /// If the addition of any service fails, an exception is thrown.
    /// </remarks>
    public static IServiceCollection AddBatchServices(this IServiceCollection services,
List<(Type serviceType, Type implementationType, ServiceLifetime lifetime)> batch)
    {
        foreach (var (serviceType, implementationType, lifetime) in batch)
        {
            // Check if implementationType actually implements serviceType
            if (!serviceType.IsAssignableFrom(implementationType))
            {
                throw new ArgumentException($"The given implementation type {implementationType.FullName} does not implement the service type {serviceType.FullName}.");
            }

            try
            {
                services.Add(new ServiceDescriptor(serviceType, implementationType, lifetime));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to add service: {ex.Message}", ex);
            }
        }

        return services;
    }

    /// <summary>
    /// Validates the registered services in the IServiceCollection against the DependencyGraph.
    /// Throws an InvalidOperationException if unresolved dependencies are found.
    /// </summary>
    /// <param name="services">The IServiceCollection containing the registered services.</param>
    /// <param name="graph">The DependencyGraph containing the dependencies among registered services.</param>
    /// <remarks>
    /// This method performs a comprehensive check to ensure that all service registrations are correct.
    /// If it finds unresolved dependencies, an exception is thrown listing those dependencies.
    /// </remarks>
    public static void ValidateRegistrations(this IServiceCollection services, DependencyGraph graph)
    {
        List<Type> unresolvedTypes = new List<Type>();

        if (services == null || !services.Any())
        {
            throw new InvalidOperationException("No services have been registered.");
        }

        ValidateServiceDescriptors(services, unresolvedTypes);
        ValidateDependencyGraph(graph, services, unresolvedTypes);

        if (unresolvedTypes.Count > 0)
        {
            throw new InvalidOperationException($"Unresolved dependencies found: {string.Join(", ", unresolvedTypes)}");
        }
    }

    /// <summary>
    /// Validates the service descriptors in the IServiceCollection.
    /// Adds any unresolved dependencies to the list of unresolved types.
    /// </summary>
    /// <param name="services">The IServiceCollection containing the registered services.</param>
    /// <param name="unresolvedTypes">List of Types that could not be resolved.</param>
    /// <remarks>
    /// This method iterates through each ServiceDescriptor in the IServiceCollection and validates it
    /// using the ValidateType method. If any dependency for a service is not registered, its type is 
    /// added to the list of unresolved types.
    /// </remarks>
    private static void ValidateServiceDescriptors(IServiceCollection services, List<Type> unresolvedTypes)
    {
        foreach (var serviceDescriptor in services)
        {
            Type? implementationType = serviceDescriptor.ImplementationType ?? serviceDescriptor.ImplementationInstance?.GetType();

            if (implementationType == null) continue;

            try
            {
                ValidateType(implementationType, services, unresolvedTypes);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to validate the type {implementationType}.", ex);
            }
        }
    }

    /// <summary>
    /// Validates that all dependencies in the DependencyGraph are registered in the provided IServiceCollection.
    /// Adds any unresolved dependencies to the list of unresolved types.
    /// </summary>
    /// <param name="graph">The DependencyGraph containing the nodes and their dependencies.</param>
    /// <param name="services">The IServiceCollection containing the registered services.</param>
    /// <param name="unresolvedTypes">List of Types that could not be resolved.</param>
    /// <remarks>
    /// This method iterates through each node in the DependencyGraph and checks if all its dependencies are registered in the service collection.
    /// If any dependency is not registered, its type is added to the list of unresolved types.
    /// </remarks>
    private static void ValidateDependencyGraph(DependencyGraph graph, IServiceCollection services, List<Type> unresolvedTypes)
    {
        foreach (var node in graph.Nodes.Values)
        {
            foreach (var dependency in node.Dependencies)
            {
                if (!IsRegistered(dependency.ServiceType, services))
                {
                    unresolvedTypes.Add(dependency.ServiceType);
                }
            }
        }
    }

    /// <summary>
    /// Validates whether all constructor dependencies for a given Type are registered in the provided IServiceCollection.
    /// Adds unresolved types to the list of unresolved types.
    /// </summary>
    /// <param name="type">The Type whose dependencies need to be validated.</param>
    /// <param name="services">The IServiceCollection containing the registered services.</param>
    /// <param name="unresolvedTypes">List of Types that could not be resolved.</param>
    /// <exception cref="InvalidOperationException">Thrown when no public constructors are found for the given Type.</exception>
    /// <remarks>
    /// This method examines each constructor for the given Type and ensures that all its dependencies are registered in the service collection.
    /// </remarks>
    private static void ValidateType(Type type, IServiceCollection services, List<Type> unresolvedTypes)
    {
        // Get constructors
        var constructors = type.GetConstructors();
        if (!constructors.Any())
        {
            throw new InvalidOperationException($"No public constructors found for the type {type}.");
        }

        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();

            // Check each constructor parameter
            foreach (var parameter in parameters)
            {
                // If parameter type is not registered, add it to the list of unresolved types
                if (!IsRegistered(parameter.ParameterType, services))
                {
                    unresolvedTypes.Add(parameter.ParameterType);
                }
            }
        }
    }

    private static readonly ConcurrentDictionary<Type, bool> IsTypeRegisteredCache = new ConcurrentDictionary<Type, bool>();

    /// <summary>
    /// Checks if a service of a particular type is already registered in the IServiceCollection.
    /// Caches the result to optimize future lookups.
    /// </summary>
    /// <param name="type">The Type of the service to check for.</param>
    /// <param name="services">The IServiceCollection to search within.</param>
    /// <returns>True if the type is registered, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the services parameter is null.</exception>
    /// <remarks>
    /// The method uses a concurrent dictionary as a cache to improve performance for frequent lookups.
    /// </remarks>
    private static bool IsRegistered(Type type, IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));

        if (IsTypeRegisteredCache.TryGetValue(type, out bool isRegistered))
        {
            return isRegistered;
        }

        isRegistered = services.Any(serviceDescriptor =>
            serviceDescriptor.ServiceType == type ||
            serviceDescriptor.ImplementationType == type ||
            (serviceDescriptor.ImplementationInstance != null && serviceDescriptor.ImplementationInstance.GetType() == type));

        IsTypeRegisteredCache.AddOrUpdate(type, isRegistered, (key, oldValue) => isRegistered);

        return isRegistered;
    }

    /// <summary>
    /// Removes services from the IServiceCollection based on the provided predicate.
    /// </summary>
    /// <param name="services">The IServiceCollection from which services will be removed.</param>
    /// <param name="predicate">A function to test each service descriptor for a condition; services that meet this condition will be removed.</param>
    /// <returns>The modified IServiceCollection after removal of services.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the predicate is null.</exception>
    /// <remarks>
    /// The method evaluates the predicate on each registered service in the IServiceCollection.
    /// Services that satisfy the predicate are removed.
    /// </remarks>
    public static IServiceCollection RemoveServices(this IServiceCollection services, Func<ServiceDescriptor, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate, nameof(predicate));

        for (int i = services.Count - 1; i >= 0; i--)
        {
            if (predicate(services[i]))
            {
                services.RemoveAt(i);
            }
        }

        return services;
    }

    /// <summary>
    /// Updates the provided dependency graph with information about the given type and its dependencies.
    /// </summary>
    /// <param name="type">The service type being registered.</param>
    /// <param name="graph">The DependencyGraph object where dependency information will be stored.</param>
    /// <param name="services">The current IServiceCollection for reference. Not used in the current method but kept for possible future extensions.</param>
    /// <remarks>
    /// The method adds the type to the dependency graph and then inspects the type's constructors to determine its dependencies.
    /// Only the constructor with the most parameters is considered.
    /// Dependencies from the 'System' namespace are skipped.
    /// </remarks>
    private static void UpdateDependencyGraph(Type type, DependencyGraph? graph, IServiceCollection services)
    {
        if (graph == null)
        {
            return;
        }

        var node = graph.AddService(type);

        if (node == null)
        {
            // Handle or log error
            return;
        }

        var constructors = type.GetConstructors();

        var largestConstructor = constructors.OrderByDescending(c => c.GetParameters().Length).FirstOrDefault();

        if (largestConstructor != null)
        {
            var parameters = largestConstructor.GetParameters();

            foreach (var parameter in parameters)
            {
                // Skip adding built-in types as dependencies.
                if (parameter.ParameterType.Namespace == "System")
                {
                    continue;
                }

                var dependencyNode = graph.AddService(parameter.ParameterType);
                node.AddDependency(dependencyNode);
            }
        }
    }

    /// <summary>
    /// Registers services based on a configuration file and updates the provided dependency graph.
    /// </summary>
    /// <param name="services">The IServiceCollection to which services are to be added.</param>
    /// <param name="configurationPath">The path to the configuration file.</param>
    /// <param name="graph">The dependency graph to update.</param>
    /// <returns>An updated IServiceCollection.</returns>
    /// <remarks>
    /// This method reads a configuration file that contains specifications for services to be registered. 
    /// It validates these configurations before registering the services and updating the dependency graph.
    /// </remarks>
    /// <exception cref="FileNotFoundException">Thrown when the configuration file is not found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when there are invalid or missing configurations.</exception>
    public static async Task<IServiceCollection> AddServicesFromConfigurationAsync(this IServiceCollection services, string configurationPath, DependencyGraph graph)
    {
        if (!File.Exists(configurationPath))
        {
            throw new FileNotFoundException($"Configuration file {configurationPath} not found.");
        }

        var fileContent = await File.ReadAllTextAsync(configurationPath);
        var serviceConfigs = JsonSerializer.Deserialize<List<ServiceConfig>>(fileContent);

        foreach (var config in serviceConfigs)
        {
            // Validate ServiceType and ImplementationType
            if (string.IsNullOrWhiteSpace(config.ServiceType) || string.IsNullOrWhiteSpace(config.ImplementationType))
            {
                throw new InvalidOperationException("ServiceType or ImplementationType cannot be null or empty.");
            }

            var serviceType = Type.GetType(config.ServiceType);
            var implementationType = Type.GetType(config.ImplementationType);

            if (serviceType == null || implementationType == null)
            {
                throw new InvalidOperationException($"Either the service type {config.ServiceType} or the implementation type {config.ImplementationType} could not be found.");
            }

            // Check type compatibility
            if (!serviceType.IsAssignableFrom(implementationType))
            {
                throw new InvalidOperationException($"The implementation type {implementationType.FullName} is not compatible with {serviceType.FullName}.");
            }

            // Validate Lifetime
            if (!Enum.TryParse(config.Lifetime, out ServiceLifetime lifetime))
            {
                throw new InvalidOperationException($"Invalid service lifetime specified: {config.Lifetime}");
            }

            services.Add(new ServiceDescriptor(serviceType, implementationType, lifetime));

            graph?.AddService(serviceType);
        }

        return services;
    }

}
