# Architettura

## Obiettivo

DriveHarbor sincronizza una cartella su un SSD esterno verso una cartella locale
gestita dal client ufficiale OneDrive. La V1 non comunica con Microsoft Graph e
non invia telemetria o dati a servizi esterni.

## Struttura della solution

- `DriveHarbor.App`: applicazione WPF e composizione dei servizi.
- `DriveHarbor.Core`: dominio, configurazione, validazione, rilevamento volumi,
  orchestrazione della sincronizzazione, integrazione Robocopy e logging.
- `DriveHarbor.Core.Tests`: test unitari e test con directory temporanee isolate.

La logica che decide se una sincronizzazione è sicura appartiene a `Core` e non
alla UI. Questo consente in futuro di aggiungere pianificazione e system tray
senza duplicare le regole di sicurezza.

## Componenti previsti

1. `JsonConfigurationStore` salva atomicamente un file JSON versionato in
   `%LocalAppData%\DriveHarbor` e non sovrascrive configurazioni illeggibili.
2. `PathSafetyValidator` impedisce percorsi uguali, annidati, non disponibili o
   con destinazione esterna alle radici OneDrive note.
3. `WindowsVolumeCatalog` legge GUID, seriale, label e tipo delle unità pronte;
   `DriveDetectionService` acquisisce e risolve l'identità in modo fail-closed.
4. `RobocopyService` costruisce ed esegue il comando senza finestra console.
5. `SynchronizationService` applica precondizioni, modalità e cancellazione.
6. `LogService` produce eventi leggibili dalla UI e file giornalieri limitati.

Le interfacce verranno introdotte quando esiste un secondo consumatore o quando
servono per isolare dipendenze di sistema nei test.

`IOneDriveRootProvider` è la prima interfaccia di sistema: permette alla logica
di validazione di usare le variabili OneDrive di Windows in produzione e radici
temporanee controllate nei test.

`IVolumeCatalog` isola l'enumerazione Win32 dai criteri di corrispondenza. Il
resolver preferisce il GUID, usa il seriale quando il GUID corrente non è
leggibile e usa la label solo per disambiguare. Un seriale duplicato senza altro
segnale produce lo stato `Ambiguous`, mai una scelta automatica.

Il percorso assoluto configurato conserva il percorso relativo alla radice del
volume. Dopo un cambio lettera viene ricostruito sulla radice rilevata e deve
esistere prima che lo stato diventi `Connected`.

## Decisioni iniziali

- Target `net10.0-windows`, Windows 11 x64.
- WPF con MVVM leggero, senza framework UI esterni nella foundation.
- Pubblicazione self-contained, inizialmente non single-file.
- Modalità predefinita `Backup`.
- Robocopy è il motore di copia della V1.
- Persistenza JSON locale, nessun database.
- Versionamento semantico a partire da `0.1.0`.
- Nessun privilegio amministrativo richiesto dall'applicazione.

## Principi di sicurezza

- La sorgente non viene mai modificata o eliminata.
- Le cancellazioni sono ammesse solo nella destinazione e solo in Mirror.
- Mirror richiede conferma esplicita e precondizioni più restrittive.
- Se identità del volume, percorsi o disponibilità sono incerti, l'operazione si
  interrompe prima di avviare Robocopy.
- Un errore imprevisto non deve trasformarsi in una sincronizzazione aggressiva.
