# MarbleBot
A discord bot created for Algodoo marble race servers, made with Discord.NET.

## Setup
A file named `BotCredentials.json` will need to be present in the executable's working directory. A template exists in MarbleBot/Resources/BotCredentials.json and is configured to be copied to the output directory on build. An example file looks like:
```json
{
  "Token": "",
  "GoogleApiKey": "",
  "GoogleServiceAccountKeyPath": "",
  "AdminUserIds": [ 224267581370925056 ],
  "DebugGuildIds": [ 408694288604463114 ]
}
```

`GoogleApiKey` must be present to use any YouTube-related commands.

In addition, a PostgreSQL database is required for the bot to function. The connection string can be specified in MarbleBot/Resources/DatabaseSettings.json.
