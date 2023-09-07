namespace ServiceMatic;

/// <summary>
/// Represents a node in a dependency graph for services.
/// </summary>
public class DependencyNode
{
    /// <summary>
    /// Gets or sets the type of the service represented by this node.
    /// </summary>
    /// <value>
    /// The type of the service.
    /// </value>
    public Type ServiceType { get; set; }

    /// <summary>
    /// Gets or sets the set of nodes that this node depends on.
    /// </summary>
    /// <value>
    /// A hash set containing dependency nodes.
    /// </value>
    public HashSet<DependencyNode> Dependencies { get; set; } = new HashSet<DependencyNode>();

    /// <summary>
    /// Initializes a new instance of the <see cref="DependencyNode"/> class.
    /// </summary>
    /// <param name="serviceType">The type of the service represented by this node.</param>
    public DependencyNode(Type serviceType)
    {
        ServiceType = serviceType;
    }

    /// <summary>
    /// Adds a dependency to this node.
    /// </summary>
    /// <param name="node">The node to add as a dependency.</param>
    public void AddDependency(DependencyNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        Dependencies.Add(node);
    }
}
