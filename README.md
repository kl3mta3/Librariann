# Librariann

Librariann is a self-hosted digital library and reading server. Point it at your own media, and it organizes,
serves, and lets you read everything from a browser or compatible app - epubs, comics, manga, PDFs, and
audiobooks - with progress, highlights, and settings that follow you across devices.

## Features

**Library & formats**
- Multiple independent libraries, each with their own folders, file type rules, and exclude patterns
- Automatic folder watching and scanning, or trigger scans manually
- Supported formats: `.epub`, `.pdf`, comic/manga archives (`.cbz`, `.cbr`, `.cb7`, `.cbt`, `.zip`, `.rar`, `.7z`,
  `.tar.gz`), and audiobooks (`.m4b`, `.mp3`, `.m4a`)

**Reading experience**
- Built-in epub reader with paginated and scrolling layouts, custom fonts, themes, and immersive mode
- Highlights and annotations with user-defined colors, anchored so they survive re-reads
- Bookmarks, reading lists, collections, "want to read," and smart filters for building custom views of your
  library
- Per-user, per-library reading profiles so settings can differ by device or by library
- Text-to-speech, including support for a self-hosted Kokoro TTS server alongside the browser's own voices
- OPDS support for reading in third-party e-reader apps

**Metadata & discovery**
- Automated metadata matching against Open Library, with optional ComicVine and MangaDex providers
- Custom field mapping and blacklist/whitelist controls over what gets applied
- Cover art, publisher info, and people/creator pages

**Accounts & access**
- Multi-user with role-based permissions (admin, read-only, download, and more granular roles)
- OpenID Connect (OIDC) support for signing in through an existing identity provider
- Auth keys for OPDS clients and other integrations
- Self-service invites: an optional "Request an Invite" link admins can enable on the login screen, with a
  review queue, default permission presets, and optional auto-accept

**Server & administration**
- Scheduled tasks for scanning, backups, and cleanup
- Email notifications for invites, password resets, and one-off server-wide notices to your users
- One-click install/update for ffmpeg and for a managed Kokoro TTS process, supervised directly by the server
- Automated acquisition: indexers, quality profiles, and a wanted-item catalog for tracking down and pulling in
  missing media
- Server activity, stats, and media-issue reporting from the admin dashboard

## Plex integration

A separate companion app, the [Librariann Plex Patcher](https://github.com/kl3mta3/Librariann-Plex-Patcher),
adds a Librariann entry to Plex Web's sidebar so you can jump into your library without leaving Plex. See that
repo's releases page for the download, and your Librariann server's Settings > Info > Plex Patch tab for setup
instructions specific to your instance.

## Getting started

### Docker

```bash
docker run -d \
  --name librariann \
  -p 5000:5000 \
  -v /path/to/config:/librariann/config \
  -v /path/to/your/media:/media \
  kl3mta3/librariann:latest
```

Everything Librariann needs to persist - its database, settings, cache, and covers - lives under the mounted
`config` volume. Back that folder up before updating.

### Native (Windows / Linux / macOS)

Download the archive for your platform from the Releases page, extract it anywhere, and run the executable.
On first launch it creates a `config` folder next to itself for its database and settings.

## First-time setup

1. Open the server's address in a browser (default port `5000`).
2. Create the initial admin account.
3. Add a library, pointing it at a folder containing your media.
4. Once the scan finishes, your library is ready to read.

## Configuration

All persistent state lives under the `config` folder next to the executable (or the mounted volume, in Docker):
the database, `appsettings.json`, cache, covers, logs, and backups. Server-wide settings (port, email, task
schedules, and everything else) are managed from the admin Settings pages once you're logged in - editing
`appsettings.json` directly is only needed for the handful of settings noted in it (host/port bindings, the
token signing key, and embedding origins for external integrations).

## Updating

1. Stop the running instance.
2. Back up the `config` folder somewhere separate, as a precaution.
3. Replace the application files with the new version, leaving `config` untouched.
4. Start it back up. Librariann backs up its own database automatically before applying any needed migration,
   and restores that backup if a migration fails.

## Community

- [Discord](https://discord.gg/fkNu9f8uyu)
- [GitHub](https://github.com/Kl3mta3/Librariann)

## License

Librariann is licensed under GPLv3. See [LICENSE](LICENSE) and [docs/NOTICE.md](docs/NOTICE.md) for full
license and attribution details.
