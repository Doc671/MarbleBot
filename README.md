# MarbleBot
MarbleBot is a Discord bot created by Doc671#1965 for several Algodoo marble race servers. It currently runs on .NET Core 3.1 and Discord.NET 2.1.1.

Check the wiki for command information: [https://github.com/Doc671/MarbleBot/wiki](https://github.com/Doc671/MarbleBot/wiki)

This bot contains several games based off Algodoo marble games on YouTube.

## Marble Race

Sign up in a marble race as anything you like, then play!

![mb/race](https://cdn.discordapp.com/attachments/296376584238137355/579963128792743946/unknown.png "Races")

## Marble Siege

Play in a Marble Siege battle!

![mb/siege](https://cdn.discordapp.com/attachments/296376584238137355/581958719236079777/unknown.png "The beginning of a Siege game")

Use items you've collected against the bosses!

![mb/siege](https://cdn.discordapp.com/attachments/296376584238137355/581959249635311617/unknown.png "Using an item during a Siege")

Activate a power-up to help you in the battle!

![mb/siege](https://cdn.discordapp.com/attachments/296376584238137355/581960295715569664/unknown.png "Activating a power-up during a Siege")

## Marble War

Play in a Marble War battle!

![mb/war](https://cdn.discordapp.com/attachments/296376584238137355/583227783577206784/unknown.png "Starting a War game")

Attack the members of the opposing team! If there is an odd number of contestants, an AI marble joins the team with fewer fighters!

![mb/war](https://cdn.discordapp.com/attachments/296376584238137355/583228188260433920/unknown.png "A war battle")

## Setup

A file named `BotCredentials.json` will need to be present in the executable's working directory. Use the following template:
```json
{
  "Token": "",
  "GoogleApiKey": "",
  "AdminIds": [ 224267581370925056 ],
  "DebugChannel": 409655798730326016
}
```

`GoogleApiKey` must be present to use the YouTube commands and the Google Sheets-based moderation system.