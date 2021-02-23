# Valheim Overseer

Automatic world backups, logging, scheduled restarts, and cleaner console output for your Valheim dedicated server.

(Currently available for Windows servers only)

## How It Works
Valheim Overseer runs on top of `valheim_server.exe`, showing only its relevant console output and responding
to server events. 

Raw logs from the `valheim_server.exe` process are stored in `logs/` while Valheim Overseer only displays logs for relevant events
such as player connections, world backups, etc.

Automatic backups are made on each world save and stored in `backups/`. Valheim servers save every 20 minutes.

Logging and backup settings are configured via `config.json`.

## Install

1. Download the latest release for your platform
2. Copy files from `Valheim_Overseer.zip` to your Valheim server directory ([click here](https://steamcommunity.com/sharedfiles/filedetails/?id=760447682) for instructions on locating it)
3. Configure your server settings via `config.json`:
   - `ServerName` - Server name that will appear in server browser
   - `World` - Name of world file. Defaults to `"Dedicated"`.
   - `Password` - Server password
   - `Port` - Port which your server runs on (make sure your router [forwards this port](https://www.google.com/search?q=how+to+port+forward) if running from home)
   - `SteamAppId` - Valheim Steam app ID. Defaults to `892970`.
   - `MaxWorldBackups` - Maximum number of world backups to save. Defaults to `60` (10 hours worth of saves - Valheim saves world every 20 minutes).
   
## Start Server
Simply run `Start Valheim Overseer.bat` located in your Valheim server folder.

## Stop Server
With the server console window selected, press `CTRL` + `C` to safely shut down the server.

Closing the window may result in the `valheim_server.exe` process to remain running in the background. If this happens,
you must manually kill the process from Task Manager.