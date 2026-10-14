# Nexus

Create Windows installers (`setup.exe`) in your own brand style. Pick a program folder, set your logo and colours, translate your license text, and Nexus produces a single, self-contained setup file.

## Features

- **One-file setups.** The result is a single `setup.exe` with a wizard, an uninstaller and an entry in *Apps & features*.
- **Your own look.** Logo, colours, side-panel and splash images, tagline and font are all yours. Without a logo the setup simply shows your program's name. A live **Preview** shows the real wizard (nothing is installed).
- **Five languages.** The wizard and Nexus itself come in English, Dutch, German, French and Croatian. The setup starts in the language of Windows and the user can switch on the first page.
- **Automatic EULA translation.** Paste your license text, pick its language and let Nexus translate it into the other four (DeepL, LibreTranslate or the Claude API). Review and edit every language before building. Machine-translated texts are marked in the setup, with a link to the original.
- **The user decides.** Start menu shortcut, desktop shortcut, taskbar pin, start with Windows and launch after install are all chosen by the user in the wizard. You only set the defaults.
- **Per user or for all users.** Per-user installs need no administrator rights; all-users installs ask for elevation.
- **Silent install.** `Setup.exe /S` (optionally `/D=C:\Path` and `/LANG=de`) and `uninstall.exe /S`.

## Requirements

- Windows 10 or 11, 64-bit.
- To build from source: the [.NET 8 SDK](https://dotnet.microsoft.com/download) (`winget install Microsoft.DotNet.SDK.8`).

## Download

Get the latest `Nexus-<version>-win-x64.zip` from the [Releases](../../releases) page, extract it and run `Nexus.exe`. The SHA-256 checksum is in `SHA256.txt`.

## Usage

1. Start `Nexus.exe`.
2. Choose the program folder. Name, main program and icon are suggested automatically.
3. Enter the version and publisher, and set the default options.
4. Under **Setup design**, add your logo, colours, images and tagline.
5. Under **License (EULA) and translation**, paste or load your text, choose its language and click **Translate into the other languages**. Check each language.
6. Click **Preview** to try the wizard, then **Create setup**.
7. **Save project** stores everything in a `.nexus` file so you can rebuild the next version quickly.

### Setup design

| Option | Notes |
|---|---|
| Logo | PNG or JPG; a white or light logo on a transparent background works best on the dark panel. |
| Dark colour | Side panel, splash and headings. It is darkened automatically if needed so that white text stays readable. |
| Accent colour | Selection, language buttons and progress bar. |
| Main button | The large call-to-action button. Its text colour is chosen automatically (white or dark) for good contrast. |
| Side panel / splash image | Optional. About 240 x 540 and 960 x 540 pixels. |
| Tagline | Optional line under the welcome text. |
| Font | Montserrat / Open Sans (bundled) or the Windows system font (Segoe UI). |

The design is also stored next to the installed program (folder `nexus-brand`) so the uninstaller looks the same. It is removed when the program is uninstalled.

### Translating the license text

| Service | You need |
|---|---|
| DeepL | An API key (free keys end in `:fx`). |
| LibreTranslate | The URL of a server, optionally an API key. Self-host it to keep your text private. |
| Claude (Anthropic) | An Anthropic API key. Translates whole sections and keeps the formatting. The model name can be changed. |

Notes:

- Your license text is sent to the service you choose.
- The API key is only stored if you tick *Remember key on this computer* (in `%LOCALAPPDATA%\Nexus\translator.json`, not encrypted).
- Not every service supports Croatian. If a language fails, type or paste it yourself.
- **A machine translation is not legally binding.** Have important terms reviewed by a lawyer.

## Building from source

```
build-open.bat
```

This produces `publish\open\Nexus.exe` (a single self-contained file, about 70 MB because .NET is included).

The repository does not include the splash and header images. The program still starts without them, but the splash screen and the header of the main window stay empty. See `Assets\LEESMIJ-afbeeldingen.txt` for the file names; add your own PNGs with those names.

If something goes wrong at startup, an error message is shown and written to `%TEMP%\Nexus-fout.log`.

## Project layout

| File | Purpose |
|---|---|
| `Program.cs` | Start-up, mode detection (studio, installer, uninstaller), silent mode |
| `Studio.cs` | The main window where setups are created |
| `Builder.cs`, `Payload.cs` | Creates and reads the setup file (`[exe][zip][length][magic]`) |
| `Installer.cs`, `Uninstaller.cs` | The wizard, the install engine and the uninstaller |
| `Theme.cs` | Per-setup design (colours, logo, images, fonts) |
| `Translator.cs` | DeepL, LibreTranslate and Claude translation |
| `Strings.cs` | All texts in five languages |
| `Ui.cs`, `Splash.cs` | Shared controls and the splash screen |

## Known limitations

- Pinning to the taskbar is a best effort. Windows has no official API for it and it may not work on Windows 11.
- The icon of `setup.exe` itself is always the Nexus icon. Your program's icon is used for shortcuts and *Apps & features*.
- Setups are not code-signed, so Windows SmartScreen may show a warning.
- Translations in the user interface were written without a native-speaker review; corrections are welcome.

## Licenses

- Nexus: see `LICENSE`.
- Montserrat and Open Sans (bundled in `Fonts`) are licensed under the SIL Open Font License; the license texts are included.
