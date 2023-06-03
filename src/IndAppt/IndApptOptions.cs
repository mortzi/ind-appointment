namespace IndAppt;

#nullable disable
public class IndApptOptions
{
    public string IndBaseAddress { get; set; }
    public TelegramBot TelegramBot { get; set; }
    public PersonalDetails PersonalDetails { get; set; }
    public ReservationTimeBox ReservationTimeBox { get; set; }
}

public record class TelegramBot
{
    public string ApiKey { get; set; }
}

public record class PersonalDetails
{
    public string BSN { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string VNumber { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
}

public record class ReservationTimeBox
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public TimeSpan StartHour { get; set; }
    public TimeSpan EndHour { get; set; }
}