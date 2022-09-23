using Amazon.Lambda.CloudWatchEvents;
using Amazon.Lambda.TestUtilities;
using Xunit;

namespace IndAppt.Tests;

public class FunctionTest
{
    [Fact]
    public async Task TestToUpperFunction()
    {
        var function = new Function(new IndApptOptions());
        var context = new TestLambdaContext();
        await function.FunctionHandler(new CloudWatchEvent<Details>(), context);
    }
}
