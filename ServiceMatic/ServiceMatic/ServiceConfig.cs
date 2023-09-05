namespace ServiceMatic;

/// <summary>
/// Holds configuration details for a service within the dependency injection container.
/// </summary>
public class ServiceConfig
{
    /// <summary>
    /// Gets or sets the type of the service to be registered.
    /// </summary>
    /// <value>
    /// A string representing the type of the service.
    /// </value>
    public string ServiceType { get; set; }

    /// <summary>
    /// Gets or sets the type that provides the implementation of the service.
    /// </summary>
    /// <value>
    /// A string representing the type that implements the service.
    /// </value>
    public string ImplementationType { get; set; }

    /// <summary>
    /// Gets or sets the lifetime scope of the service.
    /// </summary>
    /// <value>
    /// A string representing the lifetime scope of the service, such as "Transient", "Scoped", or "Singleton".
    /// </value>
    public string Lifetime { get; set; }
}
