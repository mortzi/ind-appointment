using System.Reflection;
using Amazon.Lambda.CloudWatchEvents;
using Amazon.Lambda.TestUtilities;
using IndAppt;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .AddJsonFile(
        Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "appsettings.json"))
    .Build();

var function = new Function(configuration.GetRequiredSection("IndAppt").Get<IndApptOptions>());

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, args) => cts.Cancel();

while (!cts.IsCancellationRequested)
{
    await function.FunctionHandler(new CloudWatchEvent<Details>(), new TestLambdaContext());
    await Task.Delay(55_000);
}

