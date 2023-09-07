namespace ServiceMatic.Test;

public class DependencyNodeTests
{
    [Fact]
    public void Constructor_ShouldInitializeServiceTypeAndDependencies()
    {
        // Arrange
        var serviceType = typeof(string);

        // Act
        var node = new DependencyNode(serviceType);

        // Assert
        node.ServiceType.Should().Be(serviceType);
        node.Dependencies.Should().BeEmpty();
    }

    [Fact]
    public void AddDependency_ShouldAddNodeToDependencies()
    {
        // Arrange
        var node1 = new DependencyNode(typeof(string));
        var node2 = new DependencyNode(typeof(int));

        // Act
        node1.AddDependency(node2);

        // Assert
        node1.Dependencies.Should().Contain(node2);
    }

    [Fact]
    public void AddDependency_ShouldNotAddDuplicateDependencies()
    {
        // Arrange
        var node1 = new DependencyNode(typeof(string));
        var node2 = new DependencyNode(typeof(int));

        // Act
        node1.AddDependency(node2);
        node1.AddDependency(node2);

        // Assert
        node1.Dependencies.Should().HaveCount(1);
        node1.Dependencies.First().Should().Be(node2);
    }

    [Fact]
    public void AddDependency_ShouldThrowArgumentNullExceptionForNullNode()
    {
        // Arrange
        var node1 = new DependencyNode(typeof(string));

        // Act & Assert
        Action act = () => node1.AddDependency(null);
        act.Should().Throw<ArgumentNullException>();
    }
}
