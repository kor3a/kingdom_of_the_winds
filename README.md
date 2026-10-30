# Kingdom of the Winds

A 2D tile-based online RPG prototype, built in C# end to end as the design plan describes: a Godot 4 (.NET) client, one shared rules library and a .NET account server. The game runs on the client for a single player; players log in so their character follows them between computers, phones and tablets, or play offline with the save kept on the device. It covers Phase 0 and Phase 1 of the roadmap, plus the dungeons, quests and storyline from Phase 3, with levels up to 100.

## Run it

You need Godot 4.7 (.NET edition) and the .NET SDK.

1. Open Godot, choose **Import**, and pick `client/project.godot`.
2. Press **F5** (or the ▶ button). Godot builds the C# and starts the game.

From a terminal (Godot isn't on the PATH by default after a winget install):

```bash
dotnet build client/KotW.sln
"%LOCALAPPDATA%\Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe" --path client
```

## Accounts and cloud saves

The title screen asks players to log in or create an account (email and password), or to play offline. Logged in, the character is saved to the account instead of the device, so it carries over to every other computer, phone or tablet. A character already saved on the device is offered once after logging in, and continuing moves it to the account (the old file stays behind as `save.uploaded.json`). If the same account is played on two devices at once, the one that loaded the older copy is refused instead of overwriting newer progress, and says so in chat. Character names are unique across accounts.

The account server is in `server/`: ASP.NET Core with PostgreSQL (Npgsql and Dapper). It creates its tables on start. Passwords are stored as salted PBKDF2 hashes, logins are random tokens kept for 90 days from last use (only a hash of each is stored), and each address gets ten login tries a minute.

To run it on your computer with Docker Desktop, copy `.env.example` to `.env` and set a database password in it, then start PostgreSQL and the server together. The server listens on `http://localhost:5080`, which the client uses by default:

```bash
docker compose up -d --build
```

`docker compose logs -f server` shows what it's doing, and `docker compose down` stops it (accounts are kept in a Docker volume until `docker compose down -v`).

Without Docker, make a database in your own PostgreSQL and run the server directly; the connection string is in `server/appsettings.json`, or set `ConnectionStrings__Db`:

```bash
psql -U postgres -c "CREATE USER kotw PASSWORD 'kotw'" -c "CREATE DATABASE kotw OWNER kotw"
dotnet run --project server
```

The client finds the server through `kotw/server_url` in `client/project.godot`; for one run, pass `-- --server=<url>`.

**Putting it online.** The same `docker-compose.yml`, with its `https` profile, adds Caddy in front for HTTPS on one VPS, as the design plan suggests. Point a domain at the machine, copy `.env.example` to `.env` and fill it in, then run `docker compose --profile https up -d --build`. Set `kotw/server_url` to `https://<your domain>` before exporting builds for players. iPhones only connect over HTTPS.

The client still decides what happens in the game, so a determined player could edit their own save before it uploads. The server checks that a save is well formed, not that it was earned; that comes when the server runs the game in Phase 2.

## Mac

Install the same two tools (the macOS build of Godot 4.7 .NET), open `client/project.godot` and press ▶. Nothing in the code is tied to Windows.

```bash
brew install --cask dotnet-sdk godot-mono
```

## iPhone and iPad

The game switches to touch controls on phones and tablets: a stick that appears where your thumb lands in the lower left, an attack button (攻) and target button (◎) in the lower right, and the hotbar beside them. Several fingers work at once, so you can walk and cast together. Hold an item in the bag for its details, to sell it or to throw it away.

To try the touch layout on a computer, run with `-- --touch`. The mouse then acts as one finger.

To put it on an iPhone you need a Mac with Xcode and an Apple Developer account (the free one works for your own phone). The project already has an iOS export preset (`client/export_presets.cfg`, bundle ID `com.kor3a.kingdomofthewinds`; change the Team ID there for another account).

1. In Godot, open **Editor → Manage Export Templates** and download the templates for your version (about 1.2 GB).
2. Export the Xcode project, from Godot (**Project → Export → iOS → Export Project**, with **Export Project Only** ticked) or from a terminal:

   ```bash
   /Applications/Godot_mono.app/Contents/MacOS/Godot --headless --path client --export-debug iOS "$PWD/build/ios/KingdomOfTheWinds.ipa"
   ```

3. Open `build/ios/KingdomOfTheWinds.xcodeproj` in Xcode, pick your iPhone and press Run. The first time, the phone needs **Settings → Privacy & Security → Developer Mode** on; with a free account, also trust your profile under **Settings → General → VPN & Device Management**.

The iOS Simulator doesn't work on Apple Silicon Macs with the official Godot 4.7.2 templates: their simulator engine library is Intel-only, so it would only run under Rosetta. Test on a real iPhone instead.

The app icon is `client/icon.png`, drawn by `tools/ArtGen` with the rest of the art.

C# on iOS is still marked experimental in Godot. The save file and server messages are read and written with generated JSON code (`SaveJson` in `Main.cs`, `ApiJson` in `Cloud.cs`), because reflection-based JSON is switched off in ahead-of-time builds.

## Layout

```
shared/        Game rules in plain C#, with no Godot code (the future server compiles these too)
  Content.cs   classes, skills, items, monsters, NPCs, quests
  Content.cs   types, tiles; the tables are split by subject:
    ContentJobs.cs      classes and skills
    ContentItems.cs     weapons, armour, consumables, loot and shops
    ContentMonsters.cs  monsters by zone, with level-scaled stats
    ContentStory.cs     NPCs, quests and the storyline
  Maps.cs      seeded builders for the fifteen zones
  Rules.cs     combat math, stats, level curve, save format
  World.cs     the simulation: inputs in, state and events out
  Api.cs       account server requests and replies, and the checks both sides share
client/        Godot 4 project (C#)
  scripts/     Main (input, loop, saving), GameView (tiles, actors, lights), Hud, Overlay, TitleScreen
               (log-in and character creation), Cloud (talks to the account server), TouchControls
  shaders/     tint.gdshader colours the grey skin and hair layers
  art/ audio/  generated by tools/ArtGen
server/        Account server: Program (endpoints), Store (tables, password hashing, login tokens), Dockerfile
tools/ArtGen/  Draws all the pixel art and synthesizes the sound effects and music
```

## World and story

**The Shattered Wind.** For a thousand years the Wind Pearl in the Sky Palace kept the four winds turning. Twelve winters ago the royal astrologer Heuk-un, "Black Cloud", climbed the Stair to Heaven to take its power, and it shattered. Its shards fell into the land's guardian spirits and drove them mad, and Heuk-un's cult is hunting the shards so he can become the wind itself. The main quests (★ in the quest log) follow the shards from Pungseong to the Eye of the Winds, and end with all eight brought home.

| Zone | Levels | Guardian |
|---|---|---|
| Pungseong (capital) | town | |
| Gate Paddies | 1-5 | |
| Whispering Pinewood | 5-12 | |
| Hollow of the Mountain Lord | 12-20 | Horang, the mountain tiger |
| Weeping Reed Marsh | 18-28 | Ok-cheol the Causeway King |
| Haeseong, the Salt Harbor | town | |
| Saltwind Coast | 28-36 | |
| Drowned Shrine of the Dragon King | 36-45 | Imugi |
| Cinder Highlands | 44-54 | Bulgasari the Iron-Eater |
| Cloudgate Monastery | town | |
| Frostfang Pass | 55-65 | Baekho, the White Tiger |
| Moonlit Bamboo Grove | 65-75 | The Gumiho of Nine Tails |
| Road of the Yellow Springs | 75-85 | The Lantern Judge |
| Ruins of the Sky Palace | 85-95 | The Thunder Roc |
| Eye of the Winds | 95-100 | Heuk-un, the Black Cloud |

There are 64 quests: the 29-step storyline, side quests in every zone, and a trial for each class. Each field zone has a camp with a shrine circle, and teleport scrolls carry you between the three towns.

**Classes.** At level 5 choose Warrior, Rogue, Mystic, Monk, Hunter (bow, attacks from 5 tiles) or Shaman (curses and life drain). At level 48 your job master sets a trial; pass it and at level 50 you advance to Warlord, Shadowblade, Stormcaller, Lotus Saint, Windstrider or Grand Shaman, keeping every base skill. Each class line has 13 skills, including lines, dashes, leaps that land with a shockwave, chain spells, repeated lightning strikes, knockback, pulls that drag foes in, life drain, execute, barriers, berserk rage, damage reflection, curses and roots. Every skill has its own pixel-art picture on the hotbar and in the Skills window. Pick which eight sit on the hotbar in the Skills window (K).

## Art

All sprites are generated by `tools/ArtGen`. It produces 32×32 pixel art with outlines and hue-shifted shading.

- **Characters** are paper dolls built from separate layers: skin, base clothes, armour, hair, helmet, shield and weapon. Gear you equip shows on your character.
- **Clothing** is in a hanbok style: crossed collars, ribbon ties and long coats.
- **Skin and hair** are drawn in grey and coloured in game, so any colour works.
- **Weapons** have guards, wrapped grips, tassels and glowing staff orbs, and leave a swing trail on the strike frame. Bows and ritual fans are drawn the same way.
- **Monsters** are drawn per species; zone variants re-dye or enlarge them, and humanlike foes (bandits, cultists, reapers) are paper dolls in their own outfits.

To regenerate the art after changing it:

```bash
dotnet run --project tools/ArtGen -- client
```

It first checks the content tables and maps (every ID exists, every portal lands on open ground, every NPC can be reached) and stops with a list if anything is wrong. It also writes these previews (not committed): `tools/ArtGen/preview.png`, a sheet of outfits; `preview_gear.png`, every weapon through its whole animation; `preview_monsters.png`, every monster; and `preview_maps.png`, every zone from above. To swap in your own art later, replace the PNGs under `client/art/`. The sprite sheet layout is described at the top of `tools/ArtGen/People.cs`.

## Controls

Arrow keys or WASD to move · Space to attack or talk · Tab to target · 1–8 for skills · 9/0 for potions · I (bag), C (stats), K (skills), Q (quests), H (help), M (music) · Enter to chat (`/s`, `/w name`, `/who`).

Logged in, saves go to the account. Offline, they go to Godot's user folder (`%APPDATA%\Godot\app_userdata\Kingdom of the Winds\save.json` on Windows, `~/Library/Application Support/Godot/app_userdata/Kingdom of the Winds/save.json` on a Mac) Either way the game saves on every map change, every 60 seconds, when you quit (waiting up to five seconds for the upload) and when a phone sends the game to the background.

For debugging, `-- --shot=<folder>` runs a short scripted tour of every zone and saves screenshots to that folder.
