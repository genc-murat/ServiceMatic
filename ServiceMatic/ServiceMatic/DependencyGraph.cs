namespace ServiceMatic;

/// <summary>
/// Manages a graph of dependencies between various services.
/// </summary>
public class DependencyGraph
{
    /// <summary>
    /// Gets or sets the nodes representing the services in the dependency graph.
    /// </summary>
    /// <value>
    /// A dictionary where the key is the service type and the value is the corresponding dependency node.
    /// </value>
    public Dictionary<Type, DependencyNode> Nodes { get; set; } = new Dictionary<Type, DependencyNode>();

    /// <summary>
    /// Adds a service to the dependency graph.
    /// </summary>
    /// <param name="serviceType">The type of service to add.</param>
    /// <returns>The dependency node representing the added service.</returns>
    public DependencyNode AddService(Type serviceType)
    {
        if (!Nodes.ContainsKey(serviceType))
        {
            Nodes[serviceType] = new DependencyNode(serviceType);
        }

        return Nodes[serviceType];
    }
}
