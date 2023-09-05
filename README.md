# ServiceMatic Library for .NET

ServiceMatic is an extension library designed to make Dependency Injection in .NET simpler and more powerful. The library extends `IServiceCollection` with a range of methods that streamline service registration, validation, and more.

## Table of Contents

- [Features](#features)
  - [AddServicesFromAssembly](#addservicesfromassembly)
  - [AddServicesWithAttribute](#addserviceswithattribute)
  - [Decorate](#decorate)
  - [GetDependencyGraph](#getdependencygraph)
  - [CountRegisteredServices](#countregisteredservices)
  - [AddBatchServices](#addbatchservices)
  - [ValidateRegistrations](#validateregistrations)
  - [AddServicesFromConfiguration](#addservicesfromconfiguration)
  - [RemoveServices](#removeservices)
- [Getting Started](#getting-started)
- [License](#license)

## Features

### AddServicesFromAssembly

Register all concrete types in a given assembly to their respective interfaces.

#### Usage:

```csharp
var services = new ServiceCollection();
services.AddServicesFromAssembly(Assembly.GetExecutingAssembly());
```

#### Parameters:

- `Assembly assembly`: The assembly to scan for services.
- `Func<Type, bool> filter`: A filter function to include/exclude types from registration.
- `ServiceLifetime lifetime`: The lifetime of the registered service (Transient, Scoped, Singleton).
  
### AddServicesWithAttribute

Scan an assembly for classes with the `RegisterService` attribute and register them accordingly.

#### Usage:

First, decorate your classes with `RegisterService` attribute:

```csharp
[RegisterService(ServiceLifetime.Singleton)]
public class MySingletonService : IMySingletonService
{
    // Implementation
}
```

Then, use `AddServicesWithAttribute`:

```csharp
var services = new ServiceCollection();
services.AddServicesWithAttribute(Assembly.GetExecutingAssembly());
```

#### Parameters:

- `Assembly assembly`: The assembly to scan.
- `ServiceLifetime defaultLifetime`: Default lifetime for services without a specified lifetime in the attribute.

### Decorate

Wrap an existing service with a decorator to extend its functionality.

#### Usage:

```csharp
services.Decorate<IMyService>((sp, myService) => new MyServiceDecorator(myService));
```

### GetDependencyGraph

Retrieve a graph representing the dependencies between registered services.

#### Usage:

```csharp
var graph = services.GetDependencyGraph();
```

### CountRegisteredServices

Get the total count of registered services in `IServiceCollection`.

#### Usage:

```csharp
int count = services.CountRegisteredServices();
```

### AddBatchServices

Register multiple services at once using a list of tuples, where each tuple contains a service type, its implementation, and the lifetime.

#### Usage:

```csharp
var batch = new List<(Type serviceType, Type implementationType, ServiceLifetime lifetime)>
{
    (typeof(IMyService), typeof(MyService), ServiceLifetime.Transient)
};
services.AddBatchServices(batch);
```

### ValidateRegistrations

Validate all registered services to ensure they can be resolved.

#### Usage:

```csharp
var graph = new DependencyGraph();
services.ValidateRegistrations(graph);
```

### AddServicesFromConfiguration

Register services from a JSON configuration file.

#### Usage:

Assume a JSON (`config.json`) as follows:

```json
[
  {
    "ServiceType": "IMyService, MyNamespace",
    "ImplementationType": "MyService, MyNamespace",
    "Lifetime": "Transient"
  }
]
```

Load this into your services collection like this:

```csharp
services.AddServicesFromConfiguration("path/to/config.json", graph);
```

### RemoveServices

Remove services from `IServiceCollection` based on a predicate function.

#### Usage:

```csharp
services.RemoveServices(descriptor => descriptor.ServiceType == typeof(IMyService));
```