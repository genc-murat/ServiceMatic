namespace ServiceMatic.Test;

public class TestServiceDecorator : ITestService
{
    private readonly ITestService _original;

    public TestServiceDecorator(ITestService original)
    {
        _original = original;
    }

    // Assume ITestService has a method called "DoSomething"
    public void DoSomething()
    {
        // Add additional behavior here, then call the original method
        _original.DoSomething();
    }
}

public interface ITestService
{
    void DoSomething();
}

public class TestService : ITestService
{
    public void DoSomething()
    {

    }
}