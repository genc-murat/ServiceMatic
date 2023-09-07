namespace ServiceMatic.Test;

[RegisterService(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient)]
public class AttributeService: IAttributeService
{
}

public interface IAttributeService
{
}
