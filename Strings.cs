using System;
using System.Collections.Generic;
using System.Globalization;

namespace NdxNexus
{
    // Talen van de setup en de verwijderaar: Nederlands, Engels, Duits, Frans, Kroatisch.
    internal static class L
    {
        public static readonly string[] Codes = { "nl", "en", "de", "fr", "hr" };
        public static readonly string[] Names = { "Nederlands", "English", "Deutsch", "Français", "Hrvatski" };
        public static string Lang = "en";

        private static int Idx { get { int i = Array.IndexOf(Codes, Lang); return i < 0 ? 1 : i; } }

        public static string Detect()
        {
            string two = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
            return Array.IndexOf(Codes, two) >= 0 ? two : "en";
        }

        public static void Set(string code)
        {
            if (!string.IsNullOrEmpty(code) && Array.IndexOf(Codes, code.ToLowerInvariant()) >= 0) Lang = code.ToLowerInvariant();
        }

        public static string T(string key, params object[] args)
        {
            string[] v;
            string s = D.TryGetValue(key, out v) ? v[Idx] : key;
            return args.Length == 0 ? s : string.Format(s, args);
        }

        // volgorde: nl, en, de, fr, hr
        private static readonly Dictionary<string, string[]> D = new Dictionary<string, string[]>
        {
            { "btn.open_project", new[] { "Project openen", "Open project", "Projekt öffnen", "Ouvrir un projet", "Otvori projekt" } },
            { "btn.save_project", new[] { "Project opslaan", "Save project", "Projekt speichern", "Enregistrer le projet", "Spremi projekt" } },
            { "s.new", new[] { "Nieuwe setup", "New setup", "Neues Setup", "Nouvel installateur", "Nova instalacija" } },
            { "s.intro", new[] { "Kies de programmamap en vul de gegevens in. {0} maakt één setup.exe (Nederlands, Engels, Duits, Frans en Kroatisch).", "Choose the program folder and fill in the details. {0} creates a single setup.exe (Dutch, English, German, French and Croatian).", "Wählen Sie den Programmordner und geben Sie die Daten ein. {0} erstellt eine einzelne setup.exe (Niederländisch, Englisch, Deutsch, Französisch und Kroatisch).", "Choisissez le dossier du programme et renseignez les informations. {0} crée un seul setup.exe (néerlandais, anglais, allemand, français et croate).", "Odaberite mapu programa i unesite podatke. {0} izrađuje jedan setup.exe (nizozemski, engleski, njemački, francuski i hrvatski)." } },
            { "s.intro.open", new[] { " Jij bepaalt de vormgeving.", " You decide the design.", " Sie bestimmen das Design.", " Vous décidez de l'apparence.", " Vi određujete izgled." } },
            { "s.intro.ndx", new[] { " In NDX-huisstijl.", " In NDX brand style.", " Im NDX-Corporate-Design.", " Dans l'identité visuelle NDX.", " U NDX stilu." } },
            { "f.name", new[] { "Naam van het programma", "Program name", "Name des Programms", "Nom du programme", "Naziv programa" } },
            { "f.version", new[] { "Versie", "Version", "Version", "Version", "Verzija" } },
            { "f.publisher", new[] { "Uitgever", "Publisher", "Herausgeber", "Éditeur", "Izdavač" } },
            { "f.srcdir", new[] { "Programmamap (alle bestanden die mee moeten)", "Program folder (all files to include)", "Programmordner (alle mitzuliefernden Dateien)", "Dossier du programme (tous les fichiers à inclure)", "Mapa programa (sve datoteke koje se uključuju)" } },
            { "f.exe", new[] { "Hoofdprogramma (relatief pad, bv. MijnApp.exe)", "Main program (relative path, e.g. MyApp.exe)", "Hauptprogramm (relativer Pfad, z. B. MeineApp.exe)", "Programme principal (chemin relatif, p. ex. MonApp.exe)", "Glavni program (relativna putanja, npr. MojaAplikacija.exe)" } },
            { "f.icon", new[] { "Icoon van het programma (.ico, optioneel)", "Program icon (.ico, optional)", "Programmsymbol (.ico, optional)", "Icône du programme (.ico, facultatif)", "Ikona programa (.ico, neobavezno)" } },
            { "btn.choose", new[] { "Kiezen…", "Choose…", "Auswählen…", "Choisir…", "Odaberi…" } },
            { "sec.defaults", new[] { "Standaardkeuzes in de setup (de gebruiker kiest zelf in de wizard)", "Default choices in the setup (the user decides in the wizard)", "Standardauswahl im Setup (der Benutzer entscheidet im Assistenten)", "Choix par défaut dans l'installateur (l'utilisateur décide dans l'assistant)", "Zadani odabiri u instalaciji (korisnik odlučuje u čarobnjaku)" } },
            { "chk.permachine", new[] { "Voor alle gebruikers (beheerdersrechten nodig)", "For all users (administrator rights required)", "Für alle Benutzer (Administratorrechte erforderlich)", "Pour tous les utilisateurs (droits administrateur requis)", "Za sve korisnike (potrebna administratorska prava)" } },
            { "chk.menu", new[] { "Startmenu-snelkoppeling", "Start menu shortcut", "Startmenü-Verknüpfung", "Raccourci du menu Démarrer", "Prečac u izborniku Start" } },
            { "chk.desktop", new[] { "Bureaublad-snelkoppeling", "Desktop shortcut", "Desktop-Verknüpfung", "Raccourci sur le Bureau", "Prečac na radnoj površini" } },
            { "chk.launch", new[] { "Programma starten na installatie", "Launch program after installation", "Programm nach der Installation starten", "Lancer le programme après l'installation", "Pokreni program nakon instalacije" } },
            { "chk.taskbar", new[] { "Taakbalk vastmaken", "Pin to taskbar", "An Taskleiste anheften", "Épingler à la barre des tâches", "Prikvači na programsku traku" } },
            { "chk.autostart", new[] { "Starten met Windows", "Start with Windows", "Mit Windows starten", "Démarrer avec Windows", "Pokreni s Windowsima" } },
            { "btn.preview", new[] { "Voorbeeld", "Preview", "Vorschau", "Aperçu", "Pregled" } },
            { "btn.build", new[] { "Setup maken", "Create setup", "Setup erstellen", "Créer l'installateur", "Izradi instalaciju" } },
            { "sec.brand", new[] { "Vormgeving van de setup", "Setup design", "Design des Setups", "Apparence de l'installateur", "Izgled instalacije" } },
            { "brand.note", new[] { "Zonder eigen logo toont de setup alleen de naam van je programma. De setup bevat geen NDX-onderdelen.", "Without your own logo the setup only shows the name of your program. The setup contains no NDX elements.", "Ohne eigenes Logo zeigt das Setup nur den Namen Ihres Programms. Das Setup enthält keine NDX-Elemente.", "Sans logo personnel, l'installateur n'affiche que le nom de votre programme. L'installateur ne contient aucun élément NDX.", "Bez vlastitog logotipa instalacija prikazuje samo naziv vašeg programa. Instalacija ne sadrži NDX elemente." } },
            { "f.logo", new[] { "Logo (PNG of JPG, bij voorkeur wit of licht op transparant)", "Logo (PNG or JPG, preferably white or light on transparent)", "Logo (PNG oder JPG, vorzugsweise weiß oder hell auf transparentem Hintergrund)", "Logo (PNG ou JPG, de préférence blanc ou clair sur fond transparent)", "Logotip (PNG ili JPG, po mogućnosti bijel ili svijetao na prozirnoj pozadini)" } },
            { "f.side", new[] { "Achtergrond van het zijpaneel (optioneel, ca. 240 x 540)", "Side panel background (optional, approx. 240 x 540)", "Hintergrund der Seitenleiste (optional, ca. 240 x 540)", "Arrière-plan du panneau latéral (facultatif, env. 240 x 540)", "Pozadina bočne ploče (neobavezno, oko 240 x 540)" } },
            { "f.splash", new[] { "Afbeelding voor het opstartscherm (optioneel, 960 x 540)", "Image for the splash screen (optional, 960 x 540)", "Bild für den Startbildschirm (optional, 960 x 540)", "Image pour l'écran de démarrage (facultatif, 960 x 540)", "Slika za početni zaslon (neobavezno, 960 x 540)" } },
            { "f.cdark", new[] { "Donkere kleur (zijpaneel en koppen)", "Dark colour (side panel and headings)", "Dunkle Farbe (Seitenleiste und Überschriften)", "Couleur foncée (panneau latéral et titres)", "Tamna boja (bočna ploča i naslovi)" } },
            { "f.caccent", new[] { "Accentkleur (selectie, voortgang)", "Accent colour (selection, progress)", "Akzentfarbe (Auswahl, Fortschritt)", "Couleur d'accent (sélection, progression)", "Naglašena boja (odabir, napredak)" } },
            { "f.cprimary", new[] { "Hoofdknop", "Main button", "Hauptschaltfläche", "Bouton principal", "Glavni gumb" } },
            { "f.tagline", new[] { "Slogan (optioneel, onder de welkomsttekst)", "Tagline (optional, below the welcome text)", "Slogan (optional, unter dem Begrüßungstext)", "Slogan (facultatif, sous le texte de bienvenue)", "Slogan (neobavezno, ispod uvodnog teksta)" } },
            { "chk.sysfont", new[] { "Systeemlettertype (Segoe UI) in plaats van Montserrat / Open Sans", "System font (Segoe UI) instead of Montserrat / Open Sans", "Systemschrift (Segoe UI) statt Montserrat / Open Sans", "Police système (Segoe UI) au lieu de Montserrat / Open Sans", "Sistemski font (Segoe UI) umjesto Montserrat / Open Sans" } },
            { "sec.license", new[] { "Licentie (EULA) en vertaling", "License (EULA) and translation", "Lizenz (EULA) und Übersetzung", "Licence (CLUF) et traduction", "Licenca (EULA) i prijevod" } },
            { "lic.note", new[] { "Plak of laad de originele licentietekst en kies in welke taal die is geschreven. Daarna kun je hem automatisch laten vertalen. Bekijk en corrigeer elke vertaling: een automatische vertaling is niet juridisch bindend en de setup meldt dat ook aan de gebruiker.", "Paste or load the original license text and choose the language it is written in. You can then have it translated automatically. Review and correct every translation: an automatic translation is not legally binding and the setup tells the user so.", "Fügen Sie den Originallizenztext ein oder laden Sie ihn und wählen Sie die Sprache, in der er verfasst ist. Danach können Sie ihn automatisch übersetzen lassen. Prüfen und korrigieren Sie jede Übersetzung: Eine automatische Übersetzung ist rechtlich nicht bindend, und das Setup weist den Benutzer darauf hin.", "Collez ou chargez le texte original de la licence et choisissez sa langue. Vous pouvez ensuite le faire traduire automatiquement. Relisez et corrigez chaque traduction : une traduction automatique n'a pas de valeur juridique et l'installateur en informe l'utilisateur.", "Zalijepite ili učitajte izvorni tekst licence i odaberite jezik na kojem je napisan. Zatim ga možete automatski prevesti. Pregledajte i ispravite svaki prijevod: automatski prijevod nije pravno obvezujući, a instalacija o tome obavještava korisnika." } },
            { "lic.srclang", new[] { "Taal van de originele tekst", "Language of the original text", "Sprache des Originaltextes", "Langue du texte original", "Jezik izvornog teksta" } },
            { "lic.load", new[] { "Tekst laden uit bestand…", "Load text from file…", "Text aus Datei laden…", "Charger le texte depuis un fichier…", "Učitaj tekst iz datoteke…" } },
            { "lic.provider", new[] { "Vertaaldienst", "Translation service", "Übersetzungsdienst", "Service de traduction", "Usluga prijevoda" } },
            { "lic.key", new[] { "API-sleutel", "API key", "API-Schlüssel", "Clé API", "API ključ" } },
            { "lic.url", new[] { "Server-URL (LibreTranslate), bv. https://libretranslate.jouwserver.nl", "Server URL (LibreTranslate), e.g. https://libretranslate.yourserver.com", "Server-URL (LibreTranslate), z. B. https://libretranslate.ihrserver.de", "URL du serveur (LibreTranslate), p. ex. https://libretranslate.votreserveur.fr", "URL poslužitelja (LibreTranslate), npr. https://libretranslate.vasposluzitelj.hr" } },
            { "lic.model", new[] { "Model (Claude)", "Model (Claude)", "Modell (Claude)", "Modèle (Claude)", "Model (Claude)" } },
            { "lic.remember", new[] { "Sleutel onthouden op deze computer", "Remember key on this computer", "Schlüssel auf diesem Computer speichern", "Mémoriser la clé sur cet ordinateur", "Zapamti ključ na ovom računalu" } },
            { "lic.privacy", new[] { "Let op: de licentietekst wordt naar de gekozen vertaaldienst gestuurd. Kroatisch wordt niet door elke dienst ondersteund; krijg je een foutmelding, vul die taal dan zelf in.", "Note: the license text is sent to the chosen translation service. Croatian is not supported by every service; if you get an error, fill in that language yourself.", "Hinweis: Der Lizenztext wird an den gewählten Übersetzungsdienst gesendet. Kroatisch wird nicht von jedem Dienst unterstützt; bei einer Fehlermeldung tragen Sie diese Sprache bitte selbst ein.", "Remarque : le texte de la licence est envoyé au service de traduction choisi. Le croate n'est pas pris en charge par tous les services ; en cas d'erreur, saisissez cette langue vous-même.", "Napomena: tekst licence šalje se odabranoj usluzi prijevoda. Hrvatski ne podržava svaka usluga; ako dobijete pogrešku, unesite taj jezik sami." } },
            { "lic.translate", new[] { "Vertalen naar de andere talen", "Translate into the other languages", "In die anderen Sprachen übersetzen", "Traduire dans les autres langues", "Prevedi na ostale jezike" } },
            { "lic.tabs", new[] { "Tekst per taal", "Text per language", "Text pro Sprache", "Texte par langue", "Tekst po jeziku" } },
            { "lic.none", new[] { "Nog geen licentietekst: de setup toont dan geen licentiepagina.", "No license text yet: the setup then shows no license page.", "Noch kein Lizenztext: Das Setup zeigt dann keine Lizenzseite.", "Pas encore de texte de licence : l'installateur n'affiche alors pas de page de licence.", "Još nema teksta licence: instalacija tada ne prikazuje stranicu licence." } },
            { "lic.in", new[] { "In de setup: {0}.", "In the setup: {0}.", "Im Setup: {0}.", "Dans l'installateur : {0}.", "U instalaciji: {0}." } },
            { "lic.orig", new[] { " (origineel)", " (original)", " (Original)", " (original)", " (izvornik)" } },
            { "lic.autotr", new[] { " (automatisch vertaald)", " (automatically translated)", " (automatisch übersetzt)", " (traduit automatiquement)", " (automatski preveden)" } },
            { "st.saved", new[] { "Project opgeslagen.", "Project saved.", "Projekt gespeichert.", "Projet enregistré.", "Projekt spremljen." } },
            { "st.opened", new[] { "Project geopend.", "Project opened.", "Projekt geöffnet.", "Projet ouvert.", "Projekt otvoren." } },
            { "st.done", new[] { "Klaar: {0}", "Done: {0}", "Fertig: {0}", "Terminé : {0}", "Gotovo: {0}" } },
            { "st.failed", new[] { "Mislukt.", "Failed.", "Fehlgeschlagen.", "Échec.", "Neuspjelo." } },
            { "st.translating", new[] { "Vertalen naar {0}…", "Translating to {0}…", "Übersetze nach {0}…", "Traduction vers {0}…", "Prevodim na {0}…" } },
            { "st.translated", new[] { "Vertaald. Controleer elke taal voordat je de setup maakt.", "Translated. Check each language before creating the setup.", "Übersetzt. Prüfen Sie jede Sprache, bevor Sie das Setup erstellen.", "Traduit. Vérifiez chaque langue avant de créer l'installateur.", "Prevedeno. Provjerite svaki jezik prije izrade instalacije." } },
            { "st.translated.err", new[] { "Klaar, met {0} fout(en).", "Done, with {0} error(s).", "Fertig, mit {0} Fehler(n).", "Terminé, avec {0} erreur(s).", "Gotovo, s {0} pogrešaka." } },
            { "b.collect", new[] { "Bestanden verzamelen…", "Collecting files…", "Dateien werden gesammelt…", "Collecte des fichiers…", "Prikupljanje datoteka…" } },
            { "b.assemble", new[] { "Installer samenstellen…", "Assembling installer…", "Installationsprogramm wird zusammengestellt…", "Assemblage de l'installateur…", "Sastavljanje instalacije…" } },
            { "b.nofiles", new[] { "De programmamap bevat geen bestanden.", "The program folder contains no files.", "Der Programmordner enthält keine Dateien.", "Le dossier du programme ne contient aucun fichier.", "Mapa programa ne sadrži datoteke." } },
            { "b.already", new[] { "Dit is zelf al een installer; start {0} zelf.", "This is already an installer; start {0} itself.", "Dies ist bereits ein Installationsprogramm; starten Sie {0} selbst.", "Ceci est déjà un installateur ; lancez {0} directement.", "Ovo je već instalacija; pokrenite sam {0}." } },
            { "msg.name", new[] { "Vul de naam van het programma in.", "Enter the program name.", "Geben Sie den Namen des Programms ein.", "Saisissez le nom du programme.", "Unesite naziv programa." } },
            { "msg.ver", new[] { "Vul een versie in.", "Enter a version.", "Geben Sie eine Version ein.", "Saisissez une version.", "Unesite verziju." } },
            { "msg.src", new[] { "Kies een bestaande programmamap.", "Choose an existing program folder.", "Wählen Sie einen vorhandenen Programmordner.", "Choisissez un dossier de programme existant.", "Odaberite postojeću mapu programa." } },
            { "msg.exe", new[] { "Het hoofdprogramma staat niet in de programmamap.", "The main program is not in the program folder.", "Das Hauptprogramm befindet sich nicht im Programmordner.", "Le programme principal ne se trouve pas dans le dossier du programme.", "Glavni program nije u mapi programa." } },
            { "msg.icon", new[] { "Het icoonbestand bestaat niet.", "The icon file does not exist.", "Die Symboldatei existiert nicht.", "Le fichier d'icône n'existe pas.", "Datoteka ikone ne postoji." } },
            { "msg.licsrc", new[] { "De originele licentietekst ({0}) is leeg. Kies de juiste brontaal of vul de tekst in.", "The original license text ({0}) is empty. Choose the correct source language or enter the text.", "Der originale Lizenztext ({0}) ist leer. Wählen Sie die richtige Quellsprache oder geben Sie den Text ein.", "Le texte de licence original ({0}) est vide. Choisissez la bonne langue source ou saisissez le texte.", "Izvorni tekst licence ({0}) je prazan. Odaberite ispravan izvorni jezik ili unesite tekst." } },
            { "msg.img", new[] { "Een afbeelding van de vormgeving bestaat niet: {0}", "An image of the design does not exist: {0}", "Ein Bild des Designs existiert nicht: {0}", "Une image de l'apparence n'existe pas : {0}", "Slika izgleda ne postoji: {0}" } },
            { "msg.contrast", new[] { "De kleur van de hoofdknop lijkt te veel op de donkere kleur.", "The main button colour is too similar to the dark colour.", "Die Farbe der Hauptschaltfläche ähnelt der dunklen Farbe zu stark.", "La couleur du bouton principal est trop proche de la couleur foncée.", "Boja glavnog gumba previše sliči tamnoj boji." } },
            { "msg.needlic", new[] { "Vul eerst de originele licentietekst in (in de taal die je hierboven koos).", "Enter the original license text first (in the language you chose above).", "Geben Sie zuerst den originalen Lizenztext ein (in der oben gewählten Sprache).", "Saisissez d'abord le texte de licence original (dans la langue choisie ci-dessus).", "Prvo unesite izvorni tekst licence (na jeziku odabranom gore)." } },
            { "msg.overwrite", new[] { "Bestaande vertalingen worden overschreven. Doorgaan?", "Existing translations will be overwritten. Continue?", "Vorhandene Übersetzungen werden überschrieben. Fortfahren?", "Les traductions existantes seront remplacées. Continuer ?", "Postojeći prijevodi bit će prepisani. Nastaviti?" } },
            { "msg.built", new[] { "De setup is gemaakt. Map openen?", "The setup has been created. Open the folder?", "Das Setup wurde erstellt. Ordner öffnen?", "L'installateur a été créé. Ouvrir le dossier ?", "Instalacija je izrađena. Otvoriti mapu?" } },
            { "msg.buildfail", new[] { "Setup maken mislukt: {0}", "Creating the setup failed: {0}", "Das Setup konnte nicht erstellt werden: {0}", "La création de l'installateur a échoué : {0}", "Izrada instalacije nije uspjela: {0}" } },
            { "msg.brandfail", new[] { "De vormgeving kon niet worden gelezen: {0}", "The design could not be read: {0}", "Das Design konnte nicht gelesen werden: {0}", "L'apparence n'a pas pu être lue : {0}", "Izgled se nije mogao pročitati: {0}" } },
            { "msg.readfail", new[] { "Kon het project niet lezen: {0}", "Could not read the project: {0}", "Das Projekt konnte nicht gelesen werden: {0}", "Impossible de lire le projet : {0}", "Projekt se nije mogao pročitati: {0}" } },
            { "msg.previewfail", new[] { "Voorbeeld mislukt: {0}", "Preview failed: {0}", "Vorschau fehlgeschlagen: {0}", "L'aperçu a échoué : {0}", "Pregled nije uspio: {0}" } },
            { "dlg.folder", new[] { "Kies de map met het programma", "Choose the folder with the program", "Wählen Sie den Ordner mit dem Programm", "Choisissez le dossier du programme", "Odaberite mapu s programom" } },
            { "flt.ico", new[] { "Icoon (*.ico)|*.ico", "Icon (*.ico)|*.ico", "Symbol (*.ico)|*.ico", "Icône (*.ico)|*.ico", "Ikona (*.ico)|*.ico" } },
            { "flt.img", new[] { "Afbeelding|*.png;*.jpg;*.jpeg", "Image|*.png;*.jpg;*.jpeg", "Bild|*.png;*.jpg;*.jpeg", "Image|*.png;*.jpg;*.jpeg", "Slika|*.png;*.jpg;*.jpeg" } },
            { "flt.txt", new[] { "Tekst (*.txt)|*.txt", "Text (*.txt)|*.txt", "Text (*.txt)|*.txt", "Texte (*.txt)|*.txt", "Tekst (*.txt)|*.txt" } },
            { "flt.exe", new[] { "Programma (*.exe)|*.exe", "Program (*.exe)|*.exe", "Programm (*.exe)|*.exe", "Programme (*.exe)|*.exe", "Program (*.exe)|*.exe" } },
            { "flt.proj", new[] { "{0}-project (*.nexus)|*.nexus", "{0} project (*.nexus)|*.nexus", "{0}-Projekt (*.nexus)|*.nexus", "Projet {0} (*.nexus)|*.nexus", "{0} projekt (*.nexus)|*.nexus" } },
            { "tr.nokey.deepl", new[] { "Vul je DeepL-sleutel in.", "Enter your DeepL key.", "Geben Sie Ihren DeepL-Schlüssel ein.", "Saisissez votre clé DeepL.", "Unesite svoj DeepL ključ." } },
            { "tr.nokey.claude", new[] { "Vul je Anthropic API-sleutel in.", "Enter your Anthropic API key.", "Geben Sie Ihren Anthropic-API-Schlüssel ein.", "Saisissez votre clé API Anthropic.", "Unesite svoj Anthropic API ključ." } },
            { "tr.nourl", new[] { "Vul de server-URL van LibreTranslate in.", "Enter the LibreTranslate server URL.", "Geben Sie die LibreTranslate-Server-URL ein.", "Saisissez l'URL du serveur LibreTranslate.", "Unesite URL LibreTranslate poslužitelja." } },
            { "tr.count", new[] { "De vertaaldienst gaf een ander aantal regels terug.", "The translation service returned a different number of lines.", "Der Übersetzungsdienst gab eine andere Zeilenanzahl zurück.", "Le service de traduction a renvoyé un nombre de lignes différent.", "Usluga prijevoda vratila je drugačiji broj redaka." } },
            { "tr.http", new[] { "Vertaaldienst: HTTP {0} {1}", "Translation service: HTTP {0} {1}", "Übersetzungsdienst: HTTP {0} {1}", "Service de traduction : HTTP {0} {1}", "Usluga prijevoda: HTTP {0} {1}" } },
            { "err.startup", new[] { "Opstarten mislukt", "Startup failed", "Start fehlgeschlagen", "Échec du démarrage", "Pokretanje nije uspjelo" } },
            { "err.unexpected", new[] { "Onverwachte fout", "Unexpected error", "Unerwarteter Fehler", "Erreur inattendue", "Neočekivana pogreška" } },
            { "err.generic", new[] { "Fout", "Error", "Fehler", "Erreur", "Pogreška" } },
            { "s.sample", new[] { "Mijn programma", "My program", "Mein Programm", "Mon programme", "Moj program" } },
            { "title.install", new[] { "{0} installeren", "Install {0}", "{0} installieren", "Installer {0}", "Instalacija programa {0}" } },
            { "title.uninstall", new[] { "{0} verwijderen", "Uninstall {0}", "{0} deinstallieren", "Désinstaller {0}", "Deinstalacija programa {0}" } },
            { "version", new[] { "Versie {0}", "Version {0}", "Version {0}", "Version {0}", "Verzija {0}" } },
            { "language", new[] { "Taal", "Language", "Sprache", "Langue", "Jezik" } },

            { "welcome.title", new[] { "Welkom", "Welcome", "Willkommen", "Bienvenue", "Dobrodošli" } },
            { "welcome.intro", new[] { "Deze wizard installeert {0} op je computer. Het duurt maar even.", "This wizard installs {0} on your computer. It only takes a moment.", "Dieser Assistent installiert {0} auf Ihrem Computer. Das dauert nur einen Moment.", "Cet assistant installe {0} sur votre ordinateur. Cela ne prend qu'un instant.", "Ovaj čarobnjak instalira {0} na vaše računalo. Trajat će samo trenutak." } },
            { "publisher", new[] { "Uitgever: {0}", "Publisher: {0}", "Herausgeber: {0}", "Éditeur : {0}", "Izdavač: {0}" } },
            { "space", new[] { "Benodigde ruimte: ongeveer {0}", "Space required: about {0}", "Benötigter Speicherplatz: ca. {0}", "Espace requis : environ {0}", "Potreban prostor: oko {0}" } },
            { "tagline", new[] { "Digitaal verbonden. Altijd onderweg.", "Digitaal verbonden. Altijd onderweg.", "Digitaal verbonden. Altijd onderweg.", "Digitaal verbonden. Altijd onderweg.", "Digitaal verbonden. Altijd onderweg." } },

            { "btn.start", new[] { "Starten", "Start", "Starten", "Démarrer", "Započni" } },
            { "btn.next", new[] { "Volgende", "Next", "Weiter", "Suivant", "Dalje" } },
            { "btn.back", new[] { "Vorige", "Back", "Zurück", "Précédent", "Natrag" } },
            { "btn.cancel", new[] { "Annuleren", "Cancel", "Abbrechen", "Annuler", "Odustani" } },
            { "btn.install", new[] { "Installeren", "Install", "Installieren", "Installer", "Instaliraj" } },
            { "btn.close", new[] { "Sluiten", "Close", "Schließen", "Fermer", "Zatvori" } },
            { "btn.launchclose", new[] { "Starten en sluiten", "Launch and close", "Starten und schließen", "Lancer et fermer", "Pokreni i zatvori" } },
            { "btn.browse", new[] { "Bladeren…", "Browse…", "Durchsuchen…", "Parcourir…", "Odaberi…" } },
            { "btn.uninstall", new[] { "Verwijderen", "Uninstall", "Deinstallieren", "Désinstaller", "Ukloni" } },

            { "license.title", new[] { "Licentie", "License", "Lizenz", "Licence", "Licenca" } },
            { "license.intro", new[] { "Lees de voorwaarden en ga akkoord om door te gaan.", "Read the terms and accept them to continue.", "Lesen Sie die Bedingungen und stimmen Sie zu, um fortzufahren.", "Lisez les conditions et acceptez-les pour continuer.", "Pročitajte uvjete i prihvatite ih za nastavak." } },
            { "license.accept", new[] { "Ik ga akkoord met de voorwaarden", "I accept the terms", "Ich akzeptiere die Bedingungen", "J'accepte les conditions", "Prihvaćam uvjete" } },

            { "folder.title", new[] { "Installatiemap", "Install location", "Installationsordner", "Dossier d'installation", "Mapa za instalaciju" } },
            { "folder.intro", new[] { "Kies waar {0} wordt geïnstalleerd.", "Choose where {0} will be installed.", "Wählen Sie, wo {0} installiert wird.", "Choisissez où installer {0}.", "Odaberite gdje će se {0} instalirati." } },
            { "folder.dialog", new[] { "Kies de installatiemap", "Choose the install folder", "Installationsordner wählen", "Choisir le dossier d'installation", "Odaberite mapu za instalaciju" } },
            { "folder.all", new[] { "Installatie voor alle gebruikers (beheerdersrechten nodig).", "Installation for all users (administrator rights required).", "Installation für alle Benutzer (Administratorrechte erforderlich).", "Installation pour tous les utilisateurs (droits administrateur requis).", "Instalacija za sve korisnike (potrebna su administratorska prava)." } },
            { "folder.user", new[] { "Installatie voor jouw account, geen beheerdersrechten nodig.", "Installation for your account only, no administrator rights needed.", "Installation nur für Ihr Konto, keine Administratorrechte nötig.", "Installation pour votre compte uniquement, aucun droit administrateur requis.", "Instalacija samo za vaš račun, administratorska prava nisu potrebna." } },
            { "folder.invalid", new[] { "Kies een geldige installatiemap, bijvoorbeeld {0}.", "Choose a valid install folder, for example {0}.", "Wählen Sie einen gültigen Installationsordner, z. B. {0}.", "Choisissez un dossier d'installation valide, par exemple {0}.", "Odaberite ispravnu mapu za instalaciju, npr. {0}." } },

            { "opt.title", new[] { "Opties", "Options", "Optionen", "Options", "Opcije" } },
            { "opt.intro", new[] { "Kies zelf wat er verder wordt aangemaakt.", "Choose what else should be set up.", "Wählen Sie selbst, was zusätzlich eingerichtet wird.", "Choisissez ce qui doit être configuré en plus.", "Sami odaberite što će se još postaviti." } },
            { "opt.menu", new[] { "Snelkoppeling in het Startmenu", "Start menu shortcut", "Verknüpfung im Startmenü", "Raccourci dans le menu Démarrer", "Prečac u izborniku Start" } },
            { "opt.desktop", new[] { "Snelkoppeling op het bureaublad", "Desktop shortcut", "Verknüpfung auf dem Desktop", "Raccourci sur le Bureau", "Prečac na radnoj površini" } },
            { "opt.taskbar", new[] { "Vastmaken aan de taakbalk", "Pin to the taskbar", "An die Taskleiste anheften", "Épingler à la barre des tâches", "Prikvači na programsku traku" } },
            { "opt.autostart", new[] { "Starten met Windows", "Start with Windows", "Mit Windows starten", "Démarrer avec Windows", "Pokreni s Windowsima" } },
            { "opt.launch", new[] { "{0} starten na de installatie", "Launch {0} after installation", "{0} nach der Installation starten", "Lancer {0} après l'installation", "Pokreni {0} nakon instalacije" } },
            { "opt.taskbarnote", new[] { "Vastmaken aan de taakbalk wordt door Windows niet altijd toegestaan; zo niet, dan kun je het programma zelf vastmaken.", "Windows does not always allow pinning from a setup; if it doesn't work, you can pin the program yourself.", "Windows erlaubt das Anheften nicht immer; falls es nicht klappt, können Sie das Programm selbst anheften.", "Windows n'autorise pas toujours l'épinglage ; sinon, vous pouvez épingler le programme vous-même.", "Windows ne dopušta uvijek prikvačivanje; ako ne uspije, program možete prikvačiti sami." } },

            { "license.mt", new[] { "Dit is een automatische vertaling ter informatie. Bij verschillen geldt de originele tekst ({0}).", "This is an automatic translation for convenience. In case of differences the original text ({0}) prevails.", "Dies ist eine automatische Übersetzung zur Information. Bei Abweichungen gilt der Originaltext ({0}).", "Ceci est une traduction automatique à titre indicatif. En cas de divergence, le texte original ({0}) prévaut.", "Ovo je automatski prijevod radi informiranja. U slučaju razlika vrijedi izvorni tekst ({0})." } },
            { "license.showorig", new[] { "Toon origineel ({0})", "Show original ({0})", "Original anzeigen ({0})", "Afficher l'original ({0})", "Prikaži izvornik ({0})" } },
            { "license.showtrans", new[] { "Toon vertaling", "Show translation", "Übersetzung anzeigen", "Afficher la traduction", "Prikaži prijevod" } },
            { "preview.note", new[] { "Voorbeeld: er wordt niets geïnstalleerd", "Preview: nothing is installed", "Vorschau: es wird nichts installiert", "Aperçu : rien n'est installé", "Pregled: ništa se ne instalira" } },
            { "inst.title", new[] { "Installeren", "Installing", "Installation läuft", "Installation en cours", "Instalacija" } },
            { "inst.intro", new[] { "{0} wordt geïnstalleerd…", "{0} is being installed…", "{0} wird installiert…", "{0} est en cours d'installation…", "{0} se instalira…" } },
            { "inst.unins", new[] { "Verwijderprogramma maken…", "Creating uninstaller…", "Deinstallationsprogramm wird erstellt…", "Création du programme de désinstallation…", "Izrada programa za uklanjanje…" } },
            { "inst.shortcuts", new[] { "Snelkoppelingen maken…", "Creating shortcuts…", "Verknüpfungen werden erstellt…", "Création des raccourcis…", "Izrada prečaca…" } },
            { "inst.register", new[] { "Registreren in Windows…", "Registering with Windows…", "Registrierung in Windows…", "Enregistrement dans Windows…", "Registracija u Windowsima…" } },
            { "inst.done", new[] { "Klaar", "Done", "Fertig", "Terminé", "Gotovo" } },

            { "done.title", new[] { "Klaar", "All done", "Fertig", "Terminé", "Gotovo" } },
            { "done.intro", new[] { "{0} is geïnstalleerd. Veel plezier!", "{0} is installed. Enjoy!", "{0} wurde installiert. Viel Spaß!", "{0} est installé. Bon vol !", "{0} je instaliran. Uživajte!" } },
            { "fail.title", new[] { "Er ging iets mis", "Something went wrong", "Etwas ist schiefgelaufen", "Une erreur est survenue", "Nešto je pošlo po zlu" } },
            { "fail.intro", new[] { "De installatie is niet voltooid. Sluit {0} als het draait en probeer het opnieuw.", "The installation did not finish. Close {0} if it is running and try again.", "Die Installation wurde nicht abgeschlossen. Schließen Sie {0}, falls es läuft, und versuchen Sie es erneut.", "L'installation n'a pas abouti. Fermez {0} s'il est ouvert et réessayez.", "Instalacija nije dovršena. Zatvorite {0} ako je pokrenut i pokušajte ponovno." } },
            { "cancel.confirm", new[] { "Weet je zeker dat je de installatie wilt afbreken?", "Are you sure you want to cancel the installation?", "Möchten Sie die Installation wirklich abbrechen?", "Voulez-vous vraiment annuler l'installation ?", "Jeste li sigurni da želite prekinuti instalaciju?" } },

            { "un.intro", new[] { "Alle bestanden en snelkoppelingen van versie {0} worden verwijderd.", "All files and shortcuts of version {0} will be removed.", "Alle Dateien und Verknüpfungen der Version {0} werden entfernt.", "Tous les fichiers et raccourcis de la version {0} seront supprimés.", "Sve datoteke i prečaci verzije {0} bit će uklonjeni." } },
            { "un.done", new[] { "{0} is verwijderd.", "{0} has been removed.", "{0} wurde entfernt.", "{0} a été supprimé.", "{0} je uklonjen." } },
            { "un.nolog", new[] { "Geen installatiegegevens gevonden naast dit programma.", "No installation data found next to this program.", "Neben diesem Programm wurden keine Installationsdaten gefunden.", "Aucune donnée d'installation trouvée à côté de ce programme.", "Pokraj ovog programa nisu pronađeni podaci o instalaciji." } },
            { "un.link", new[] { "Verwijder {0}", "Uninstall {0}", "{0} deinstallieren", "Désinstaller {0}", "Ukloni {0}" } },
        };
    }
}
