namespace ServiceMatic.Test;

public class DependencyGraphTests
{
    [Fact]
    public void Constructor_ShouldInitializeEmptyNodesDictionary()
    {
        // Act
        var dependencyGraph = new DependencyGraph();

        // Assert
        dependencyGraph.Nodes.Should().BeEmpty();
    }

    [Fact]
    public void AddService_ShouldAddServiceTypeToNodes()
    {
        // Arrange
        var dependencyGraph = new DependencyGraph();
        var serviceType = typeof(string);

        // Act
        var addedNode = dependencyGraph.AddService(serviceType);

        // Assert
        dependencyGraph.Nodes.Should().ContainKey(serviceType);
        addedNode.ServiceType.Should().Be(serviceType);
    }

    [Fact]
    public void AddService_ShouldNotDuplicateExistingServices()
    {
        // Arrange
        var dependencyGraph = new DependencyGraph();
        var serviceType = typeof(string);

        // Act
        var addedNode1 = dependencyGraph.AddService(serviceType);
        var addedNode2 = dependencyGraph.AddService(serviceType);

        // Assert
        addedNode1.Should().BeSameAs(addedNode2);
        dependencyGraph.Nodes.Should().HaveCount(1);
    }

    [Fact]
    public void AddService_ShouldReturnTheNodeForTheService()
    {
        // Arrange
        var dependencyGraph = new DependencyGraph();
        var serviceType = typeof(string);

        // Act
        var addedNode = dependencyGraph.AddService(serviceType);

        // Assert
        addedNode.Should().NotBeNull();
        addedNode.ServiceType.Should().Be(serviceType);
    }

    [Fact]
    public void AddService_ShouldThrowArgumentNullExceptionForNullServiceType()
    {
        // Arrange
        var dependencyGraph = new DependencyGraph();

        // Act & Assert
        Action act = () => dependencyGraph.AddService(null);
        act.Should().Throw<ArgumentNullException>();
    }
}
