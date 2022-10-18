# IndAppt

**This project intends to help people who want to reserve IND desk appointments, and waiting list is very long**

**This code is provided as is and the writer has no guarantee on the usage of this code**

## How to use

- Clone this repository: 
    ```ps1
    git clone https://github.com/mortzi/ind-appointment.git
    ```
- Update appsettings.json content (at src/IndAppt/appsettings.json):
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
- Run the app using docker or locally
  1. Using docker (requires docker to be installed)
        ```ps1
        cd ./src/IndAppt/
        docker build -t indappt .
        docker run --name myIndAppt indappt
        ```
        *Note:* To stop the the container and start it again do:

            ```ps1
            docker stop myIndAppt
            docker start myIndAppt -a
            ```
  
  2. Locally (requires dotnet 6.0 to be installed)
        ```ps1
        dotnet run ./src/IndAppt/IndAppt.csproj
        ```

- Go to your telegram bot and send `/start` command to start looking for appointment
- To stop the app, send `/stop` command to the bot
