using Microsoft.Extensions.DependencyInjection;

namespace ServiceMatic.Test;

public class RegisterServiceAttributeTests
{
    [Fact]
    public void Constructor_ShouldSetLifetimeProperty()
    {
        // Arrange & Act
        var attribute = new RegisterServiceAttribute(ServiceLifetime.Singleton);

        // Assert
        attribute.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void Attribute_ShouldBeApplicableToClass()
    {
        // Arrange
        var attributeType = typeof(RegisterServiceAttribute);

        // Act
        var attributes = Attribute.GetCustomAttributes(typeof(AttributeService), attributeType);

        // Assert
        attributes.Should().NotBeNullOrEmpty();
        attributes[0].Should().BeOfType<RegisterServiceAttribute>();
        ((RegisterServiceAttribute)attributes[0]).Lifetime.Should().Be(ServiceLifetime.Transient);
    }
}