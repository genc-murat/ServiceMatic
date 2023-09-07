using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Xunit;
using ServiceMatic;
using System.Reflection;
using FluentAssertions.Common;

namespace ServiceMatic.Test;

public class ServiceMaticExtensionsTests
{
    private IServiceCollection _serviceCollection;

    public ServiceMaticExtensionsTests()
    {
        _serviceCollection = new ServiceCollection();
    }

    [Fact]
    public void AddServicesFromAssembly_ShouldRegisterServices()
    {
        // Arrange
        Assembly assembly = Assembly.GetExecutingAssembly();

        // Act
        _serviceCollection.AddServicesFromAssembly(assembly, registerIfNoInterface: true);

        // Assert
        _serviceCollection.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void AddServicesFromAssembly_ShouldThrowArgumentNullException_WhenAssemblyIsNull()
    {
        // Act
        Action act = () => _serviceCollection.AddServicesFromAssembly(null, registerIfNoInterface: true);

        // Assert
        act.Should().Throw<ArgumentNullException>().And.ParamName.Should().Be("assembly");
    }

    [Fact]
    public void AddServicesWithAttribute_ShouldRegisterServicesWithAttribute()
    {
        // Arrange
        Assembly assembly = Assembly.GetExecutingAssembly();

        // Act
        _serviceCollection.AddServicesWithAttribute(assembly);

        // Assert
        _serviceCollection.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public void AddBatchServices_ShouldRegisterBatchOfServices()
    {
        // Arrange
        var batch = new List<(Type serviceType, Type implementationType, ServiceLifetime lifetime)>
        {
            (typeof(IService), typeof(Service), ServiceLifetime.Transient),
            (typeof(IAnotherService), typeof(AnotherService), ServiceLifetime.Scoped)
        };

        // Act
        _serviceCollection.AddBatchServices(batch);

        // Assert
        _serviceCollection.Count.Should().Be(batch.Count);
    }

    [Fact]
    public void AddServicesFromAssembly_ShouldUpdateDependencyGraph()
    {
        // Arrange
        Assembly assembly = Assembly.GetExecutingAssembly();
        var dependencyGraph = new DependencyGraph();

        // Act
        _serviceCollection.AddServicesFromAssembly(assembly, dependencyGraph: dependencyGraph);

        // Assert
        dependencyGraph.Nodes.Should().NotBeEmpty();
    }

    [Fact]
    public void Decorate_ShouldWrapServiceWithDecorator()
    {
        // Arrange
        _serviceCollection.AddSingleton<ITestService, TestService>();
        Func<IServiceProvider, ITestService, ITestService> decoratorFactory = (sp, original) => new TestServiceDecorator(original);

        // Act
        _serviceCollection.Decorate(decoratorFactory);

        // Assert
        var provider = _serviceCollection.BuildServiceProvider();
        var service = provider.GetService<ITestService>();

        // Assuming TestServiceDecorator sets a specific property to indicate it's wrapping the original service
        service.Should().BeOfType<TestServiceDecorator>();
    }

    [Fact]
    public void AddServicesFromAssembly_WithFilters_ShouldOnlyRegisterFilteredServices()
    {
        // Arrange
        Assembly assembly = Assembly.GetExecutingAssembly();
        Func<Type, bool> filter = type => type == typeof(TestService);

        // Act
        _serviceCollection.AddServicesFromAssembly(assembly, filter: filter);

        // Assert
        _serviceCollection.Count.Should().Be(1);
    }

    [Fact]
    public void Decorate_WithoutRegisteringService_ShouldThrowException()
    {
        // Arrange
        Func<IServiceProvider, ITestService, ITestService> decoratorFactory = (sp, original) => new TestServiceDecorator(original);

        // Act
        Action act = () => _serviceCollection.Decorate(decoratorFactory);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddServicesFromAssembly_ShouldRegisterServicesWithCorrectLifetime()
    {
        // Arrange
        Assembly assembly = Assembly.GetExecutingAssembly();

        // Act
        _serviceCollection.AddServicesFromAssembly(assembly, lifetime: ServiceLifetime.Singleton);

        // Assert
        var descriptor = _serviceCollection.First(d => d.ServiceType == typeof(ITestService));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void RemoveServices_ShouldRemoveCorrectServices()
    {
        // Arrange
        _serviceCollection.AddSingleton<ITestService, TestService>();
        _serviceCollection.AddSingleton<IAnotherService, AnotherService>();

        // Act
        _serviceCollection.RemoveServices(descriptor => descriptor.ServiceType == typeof(ITestService));

        // Assert
        _serviceCollection.Should().NotContain(descriptor => descriptor.ServiceType == typeof(ITestService));
        _serviceCollection.Should().Contain(descriptor => descriptor.ServiceType == typeof(IAnotherService));
    }

    [Fact]
    public async Task AddServicesFromConfigurationAsync_ShouldRegisterServicesCorrectly()
    {
        // Arrange
        var json = "[{ \"ServiceType\": \"ServiceMatic.Test.ITestService, ServiceMatic.Test\", \"ImplementationType\": \"ServiceMatic.Test.TestService, ServiceMatic.Test\", \"Lifetime\": \"Singleton\"}]";
        await File.WriteAllTextAsync("config.json", json);

        // Act
        await _serviceCollection.AddServicesFromConfigurationAsync("config.json",new DependencyGraph());

        // Assert
        var descriptor = _serviceCollection.First(d => d.ServiceType == typeof(ITestService));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Theory]
    [InlineData(ServiceLifetime.Singleton)]
    [InlineData(ServiceLifetime.Transient)]
    [InlineData(ServiceLifetime.Scoped)]
    public void AddServicesFromAssembly_ShouldRegisterServicesWithProvidedLifetime(ServiceLifetime lifetime)
    {
        // Arrange
        Assembly assembly = Assembly.GetExecutingAssembly();

        // Act
        _serviceCollection.AddServicesFromAssembly(assembly, lifetime: lifetime);

        // Assert
        var descriptor = _serviceCollection.First(d => d.ServiceType == typeof(ITestService));
        descriptor.Lifetime.Should().Be(lifetime);
    }

    [Fact]
    public void AddServicesFromAssembly_ShouldThrowExceptionForNullAssembly()
    {
        // Arrange, Act & Assert
        Action act = () => _serviceCollection.AddServicesFromAssembly(null!);

        act.Should().Throw<ArgumentNullException>()
           .And.ParamName.Should().Be("assembly");
    }

}
