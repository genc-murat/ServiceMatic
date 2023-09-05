using Microsoft.Extensions.DependencyInjection;

namespace ServiceMatic;

/// <summary>
/// Attribute to specify the registration details of a service within the dependency injection container.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class RegisterServiceAttribute : Attribute
{
    /// <summary>
    /// Gets the lifetime scope of the service.
    /// </summary>
    /// <value>
    /// The lifetime scope defined as a <see cref="ServiceLifetime"/> enumeration.
    /// </value>
    public ServiceLifetime Lifetime { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterServiceAttribute"/> class.
    /// </summary>
    /// <param name="lifetime">Specifies the lifetime of the service in the dependency injection container.</param>
    public RegisterServiceAttribute(ServiceLifetime lifetime)
    {
        Lifetime = lifetime;
    }
}
