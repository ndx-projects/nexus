# NDX Nexus (intern) en Nexus (wereldwijd)

Programma om installers (setup.exe) te maken. Eén codebase, twee edities:

| | **NDX Nexus** (intern) | **Nexus** (wereldwijd) |
|---|---|---|
| Bouwen | `build-intern.bat` | `build-open.bat` |
| Resultaat | `publish\intern\NDXNexus.exe` | `publish\open\Nexus.exe` |
| Vormgeving van de setups | altijd NDX-huisstijl (logo, routepatroon, slogan) | de maker kiest zelf: logo, kleuren, achtergrond, opstartscherm, slogan, lettertype |
| NDX-onderdelen in de setup | ja | nee, ook niet als logo of slogan |

`build.bat` bouwt beide. Vereist de .NET 8 SDK (`winget install Microsoft.DotNet.SDK.8`). De namen staan op een plek (`Edition.cs`) en zijn dus makkelijk te wijzigen.

## Taal van Nexus zelf
De Studio (en alle meldingen) is beschikbaar in Nederlands, Engels, Duits, Frans en Kroatisch. Rechtsboven in de Studio kies je de taal; alles wat je hebt ingevuld blijft staan. Bij het starten gebruikt het programma de taal van Windows (anders Engels). Ook het opstartscherm volgt de taal.

## Een setup maken
1. Start de exe (splashscreen, daarna de Studio).
2. Kies de programmamap; naam, hoofdprogramma en icoon worden voorgesteld.
3. Vul versie en uitgever in en kies de standaardkeuzes (snelkoppelingen, taakbalk, starten met Windows, starten na installatie). De gebruiker kiest zelf in de wizard.
4. *(alleen Nexus wereldwijd)* Stel onder **Vormgeving van de setup** logo, kleuren, afbeeldingen, slogan en lettertype in.
5. Klik **Voorbeeld** om de echte wizard te zien (er wordt niets geïnstalleerd) en daarna **Setup maken**.
6. Met **Project opslaan** bewaar je alles als `.nexus`-bestand.

## Vormgeving (Nexus wereldwijd)
- Logo: PNG of JPG; kies er een dat goed leesbaar is op de donkere kleur. Zonder logo toont de setup alleen de naam van het programma.
- Donkere kleur: zijpaneel, opstartscherm en koppen. Wordt automatisch donker genoeg gemaakt voor witte tekst.
- Accentkleur: selectie, taalknop, voortgangsbalk. Hoofdknop: de grote knop; de tekstkleur erop wordt automatisch wit of donker gekozen.
- Het lettertype is Montserrat / Open Sans (zit in de exe) of het Windows-lettertype Segoe UI.
- De vormgeving blijft ook beschikbaar voor de verwijderaar (map `nexus-brand` naast het programma, wordt bij verwijderen opgeruimd).

## Licentie (EULA) en automatisch vertalen
Plak of laad de originele tekst, kies de taal ervan en klik **Vertalen naar de andere talen**. Drie diensten:
- **DeepL**: API-sleutel (Free of Pro; Free-sleutels eindigen op `:fx`).
- **LibreTranslate**: URL van een eigen of externe server, eventueel met sleutel. De tekst blijft op jouw server als je die zelf host.
- **Claude (Anthropic)**: API-sleutel; vertaalt het hele document met behoud van opmaak. Model is aan te passen.

Daarna kun je elke taal nalezen en aanpassen (een aangepaste taal telt niet meer als automatische vertaling). In de setup ziet de gebruiker de licentie in de gekozen taal. Is die automatisch vertaald, dan staat er een melding bij dat de originele tekst voorgaat, met een link om het origineel te lezen. Een automatische vertaling is niet juridisch bindend; laat belangrijke voorwaarden controleren door een jurist.

Let op: de tekst wordt naar de gekozen dienst gestuurd. De sleutel wordt alleen bewaard als je "Sleutel onthouden" aanvinkt (in `%LOCALAPPDATA%\<naam>\translator.json`, niet versleuteld).

## Wat de setup doet
Wizard met taalkeuze (Nederlands, Engels, Duits, Frans, Kroatisch), licentie, map, opties, installeren, klaar. Snelkoppelingen, verwijderprogramma, vermelding bij "Apps & onderdelen", per gebruiker of voor alle gebruikers. Stil installeren: `Setup.exe /S` (eventueel `/D=C:\Pad` en `/LANG=de`), stil verwijderen: `uninstall.exe /S`. Foutcode 5 = beheerdersrechten ontbreken.

## Bekende beperkingen
- Kroatisch wordt niet door elke vertaaldienst ondersteund.
- Het icoon van de setup.exe zelf is altijd het Nexus-icoon (het icoon van jouw programma gaat naar snelkoppelingen en Apps & onderdelen).
- Geen digitale handtekening: Windows SmartScreen kan een waarschuwing tonen.
- Als de setup niets doet: kijk in `%TEMP%\<naam>-fout.log` (`NDXNexus-fout.log` of `Nexus-fout.log`).
- Lettertypen: Montserrat en Open Sans zijn SIL OFL (licenties in de map `Fonts`).
