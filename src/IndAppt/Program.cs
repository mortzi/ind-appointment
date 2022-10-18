using System.Reflection;
using IndAppt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

var configuration = new ConfigurationBuilder()
    .AddJsonFile(
        Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "appsettings.json"))
    .Build();

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, args) => cts.Cancel();

var services = new ServiceCollection();
services.AddLogging(b => b.ClearProviders().AddConsole());

services.AddSingleton<IndApptOptions>(configuration.GetRequiredSection("IndAppt").Get<IndApptOptions>());

services.AddSingleton<ITelegramBotClient>(sp => new TelegramBotClient(
    sp.GetRequiredService<IndApptOptions>().TelegramBot.ApiKey));

services.AddSingleton<HttpClient>(sp => new HttpClient
{
    BaseAddress = new Uri(sp.GetRequiredService<IndApptOptions>().IndBaseAddress)
});

services.AddSingleton<Function>();

var sp = services.BuildServiceProvider();

sp.GetRequiredService<ILogger<Program>>().LogInformation(configuration.GetDebugView());

var function = sp.GetRequiredService<Function>();

while (!cts.IsCancellationRequested)
{
    await function.FunctionHandler();
    await Task.Delay(55_000);
}
