using System.Net.Http.Json;
using System.Text.Json;
using Amazon.Lambda.CloudWatchEvents;
using Amazon.Lambda.Core;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace IndAppt;

public class Function
{
    private readonly IndApptOptions _options;
    private long? _chatId;
    private readonly ITelegramBotClient _telegramBotClient;
    private readonly HttpClient _httpClient;

    public Function(IndApptOptions options)
    {
        _options = options;
        _telegramBotClient = new TelegramBotClient(options.TelegramBot.ApiKey);
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_options.IndBaseAddress)
        };
    }

    private bool ConsiderSlot(Slot slot)
    {
        return slot.date >= _options.ReservationTimeBox.Start.Date &&
               slot.date <= _options.ReservationTimeBox.End.Date &&
               slot.startTime.Hour >= _options.ReservationTimeBox.Start.Hour &&
               slot.endTime.Hour <= _options.ReservationTimeBox.End.Hour;
    }


    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        Converters = { new DateJsonConverter(), new TimeJsonConverter() }
    };

    private bool _isActive = false;

    private async Task Start()
    {
        if (_isActive)
            return;

        _isActive = true;

        await SendMessage("starting...");
    }

    private async Task Stop()
    {
        if (!_isActive)
            return;

        _isActive = false;

        await SendMessage("stopping...");
    }

    public async Task FunctionHandler(CloudWatchEvent<Details> input, ILambdaContext context)
    {
        try
        {
            StartReceivingBotMessages();

            if (!_isActive)
            {
                context.Logger.LogInformation("Inactive state");
                return;
            }

            context.Logger.LogInformation($"{DateTime.Now.ToShortTimeString()} | Running...");

            var response = await GetAvailable(context);
            var preferredSlots = response.data.Where(ConsiderSlot).ToList();

            await SendMessage(
                $"Current available appointments. Preferred: {preferredSlots.Count}, Total: {response.data.Count}");

            foreach (var slot in preferredSlots)
            {
                var message =
                    $"{DateTime.Now.ToShortTimeString()} | Appointment found!\n\t{slot.date.ToShortDateString()} | {slot.startTime}-{slot.endTime}\n\t https://oap.ind.nl/oap/en/#/doc";

                context.Logger.LogInformation(message);

                await SendMessage(message);
                await SendMessage("Reserving appointment...");

                context.Logger.LogInformation("Reserving appointment");

                await ReserveSlot(context, slot);

                context.Logger.LogInformation("Submitting appointment");

                await SubmitSlot(context, slot);

                await SendMessage("Appointment submitted. Check your email");
                context.Logger.LogInformation("Appointment submitted");

                await Stop();

                break;
            }
        }
        catch (Exception e)
        {
            await SendMessage($"Appointment check failed! Stopping... \nError:\n{e.Message}");
            context.Logger.LogInformation($"{DateTime.Now.ToShortTimeString()} | Error occured {e}");
        }

        context.Logger.LogInformation($"{DateTime.Now.ToShortTimeString()} | Paused...");
    }

    private async Task SendMessage(string message)
    {
        if (_chatId is { })
            await _telegramBotClient.SendTextMessageAsync(_chatId, message);
    }

    private async Task SubmitSlot(ILambdaContext context, Slot slot)
    {
        var httpResponseMessage = await _httpClient.PostAsJsonAsync(
            "appointments/",
            CreateSubmitSlotRequest(slot),
            _jsonSerializerOptions);

        httpResponseMessage.EnsureSuccessStatusCode();

        var payload = await httpResponseMessage.Content.ReadAsStringAsync();
        var response = JsonSerializer.Deserialize<SubmitSlotResponse>(
            payload[5..],
            _jsonSerializerOptions);

        if (response is not { status: "OK" })
        {
            context.Logger.LogInformation($"Invalid response from IND book appointment api {payload}");
            throw new InvalidOperationException("Invalid response from IND book appointment api");
        }
    }

    private async Task<AvalableResponse> GetAvailable(ILambdaContext context)
    {
        var httpResponseMessage = await _httpClient.GetAsync("slots/?productKey=DOC&persons=1");
        httpResponseMessage.EnsureSuccessStatusCode();
        var payload = await httpResponseMessage.Content.ReadAsStringAsync();
        var response = JsonSerializer.Deserialize<AvalableResponse>(
            payload[5..],
            _jsonSerializerOptions)!;
        if (response is not { status: "OK" })
        {
            context.Logger.LogInformation($"Invalid response from IND appointment {payload}");
            throw new InvalidOperationException("Invalid response from IND appointment");
        }

        return response;
    }

    private async Task ReserveSlot(ILambdaContext context, Slot slot)
    {
        var responseMessage = await _httpClient.PostAsJsonAsync(
            $"slots/{slot.key}",
            new ReserveSlotRequest
            {
                key = slot.key,
                date = slot.date,
                parts = slot.parts,
                endTime = slot.endTime,
                startTime = slot.startTime
            },
            _jsonSerializerOptions);

        responseMessage.EnsureSuccessStatusCode();

        var payload = await responseMessage.Content.ReadAsStringAsync();

        var response = JsonSerializer.Deserialize<ReserveSlotResponse>(payload[5..])!;
        if (response is not { status: "OK" })
        {
            context.Logger.LogInformation($"Invalid response from IND appointment {payload}");
            throw new InvalidOperationException("Invalid response from IND appointment");
        }
    }

    public record ReserveSlotResponse
    {
        public string status { get; set; }
        public object data { get; set; }
    }

    public class ReserveSlotRequest
    {
        public string key { get; set; }
        public DateTime date { get; set; }
        public Time startTime { get; set; }
        public Time endTime { get; set; }
        public int parts { get; set; }
    }

    private bool _isBotListeningToMessages = false;

    private void StartReceivingBotMessages()
    {
        if (_isBotListeningToMessages)
            return;

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message }
        };
        _telegramBotClient.StartReceiving(UpdateHandler, ErrorHandler, receiverOptions);
        _isBotListeningToMessages = true;
    }

    private Task ErrorHandler(ITelegramBotClient arg1, Exception arg2, CancellationToken arg3)
    {
        return Task.CompletedTask;
    }

    private async Task UpdateHandler(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        if (update is { Type: UpdateType.Message, Message.Type: MessageType.Text })
        {
            var message = update.Message;
            var text = message.Text;

            if (text is null)
                return;

            _chatId = message.Chat.Id;
            if (text.StartsWith($"/start", StringComparison.InvariantCultureIgnoreCase))
            {
                await Start();
            }
            else if (text.StartsWith($"/stop", StringComparison.InvariantCultureIgnoreCase))
            {
                await Stop();
            }
            else
            {
                await SendMessage("Invalid command!\n\nCommands:\n\tstart\n\tstop");
            }
        }
    }

    private SubmitSlotRequest CreateSubmitSlotRequest(Slot slot)
    {
        return new SubmitSlotRequest
        {
            appointment = new Appointment
            {
                customers = new List<RequestCustomer>
                {
                    new()
                    {
                        bsn = _options.PersonalDetails.BSN,
                        firstName = _options.PersonalDetails.FirstName,
                        lastName = _options.PersonalDetails.LastName,
                        vNumber = _options.PersonalDetails.VNumber
                    }
                },
                date = slot.date,
                email = _options.PersonalDetails.Email,
                endTime = slot.endTime,
                startTime = slot.startTime,
                language = "en",
                phone = _options.PersonalDetails.Phone,
                productKey = "DOC"
            },
            bookableSlot = new BookableSlot
            {
                booked = false,
                date = slot.date,
                key = slot.key,
                parts = slot.parts,
                endTime = slot.endTime,
                startTime = slot.startTime
            },
        };
    }
}

public class Time
{
    public int Hour { get; set; }
    public int Minute { get; set; }

    public override string ToString() => $"{Hour:00}:{Minute:00}";
}

public class Slot
{
    public string key { get; set; }
    public DateTime date { get; set; }
    public Time startTime { get; set; }
    public Time endTime { get; set; }
    public int parts { get; set; }
}

public class AvalableResponse
{
    public string status { get; set; }
    public List<Slot> data { get; set; }
}

public class Details
{
}

public class Customer
{
    public string firstName { get; set; }
    public string lastName { get; set; }
    public string bsn { get; set; }
    public string vnumber { get; set; }
    public string fullName { get; set; }
    public string vNumber { get; set; }
}

public class Data
{
    public string key { get; set; }
    public int version { get; set; }
    public string code { get; set; }
    public string productKey { get; set; }
    public DateTime date { get; set; }
    public Time startTime { get; set; }
    public Time endTime { get; set; }
    public string email { get; set; }
    public bool hasEmail { get; set; }
    public string phone { get; set; }
    public string language { get; set; }
    public object status { get; set; }
    public bool hasDetail { get; set; }
    public List<Customer> customers { get; set; }
    public object birthDate { get; set; }
    public object user { get; set; }
}

public class SubmitSlotResponse
{
    public string status { get; set; }
    public Data data { get; set; }
}

public class Appointment
{
    public string productKey { get; set; }
    public DateTime date { get; set; }
    public Time startTime { get; set; }
    public Time endTime { get; set; }
    public string email { get; set; }
    public string phone { get; set; }
    public string language { get; set; }
    public List<RequestCustomer> customers { get; set; }
}

public class BookableSlot
{
    public string key { get; set; }
    public DateTime date { get; set; }
    public Time startTime { get; set; }
    public Time endTime { get; set; }
    public int parts { get; set; }
    public bool booked { get; set; }
}

public class RequestCustomer
{
    public string vNumber { get; set; }
    public string bsn { get; set; }
    public string firstName { get; set; }
    public string lastName { get; set; }
}

public class SubmitSlotRequest
{
    public BookableSlot bookableSlot { get; set; }
    public Appointment appointment { get; set; }
}