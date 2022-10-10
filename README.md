# IndAppt

**This project intends to help people who want to reserve IND desk appointments, and cannot wait in the long waiting list**

**This code is provided as is and the writer has no guarantee on the usage of this code**



To run the application locally:
- Have dotnet installed on your machine:
  - Download and run Install dotnet script: https://dot.net/v1/dotnet-install.sh
- Clone this repository: 
```ps1
git clone https://github.com/mortzi/ind-appointment.git
```
- Replace appsettings.json content with your own settings:
```json
{
    "IndAppt": {
        "IndBaseAddress": "https://oap.ind.nl/oap/api/desks/AM/",
// Create a new telegram bot using https://telegram.me/BotFather, and set the api key here
        "TelegramBot": {
            "ApiKey": "<YOUR_TELEGRAM_BOT_API_KEY>"
        },
        "PersonalDetails": {
            "BSN": "<YOUR_BSN>",
            "FirstName": "<YOUR_FIRST_NAME>",
            "LastName": "<YOUR_LAST_NAME>",
            "VNumber": "<YOUR_V_NUMBER>",
            "Email": "<YOUR_EMAIL_ADDRESS>",
            "Phone": "<YOUR_DUTCH_PHONE_NUMBER>"
        },
// The timebox you desire your appointment reservation        
        "ReservationTimeBox": {
            "Start": "09/16/2022 06:00",
            "End": "09/20/2022 23:59"
        }
    }
}
```
- Run the app
```ps1
dotnet run ./src/IndAppt.Runner/IndAppt.Runner.csproj
```
- Go to your telegram bot and send `/start` command to start looking for appointment
- To stop the app, send `/stop` command to the bot
