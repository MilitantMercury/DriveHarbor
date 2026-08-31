# Changelog

Tutte le modifiche rilevanti di DriveHarbor sono documentate in questo file.
Il progetto segue il versionamento semantico.

## [Unreleased]

## [1.0.2] - 2026-08-31

### Fixed

- L'updater richiede UAC per installazioni in cartelle protette, riapre l'app dopo errori o rollback e mostra l'esito dell'operazione.

### Added

- Barra di avanzamento e percentuale durante il download degli aggiornamenti.
- Politica di sicurezza aggiornata con versioni supportate, segnalazioni private e modello di fiducia degli aggiornamenti.

## [1.0.1] - 2026-08-31

### Changed

- Il pannello attività mostra un riepilogo comprensibile dei file sincronizzati, mantenuti, eliminati ed eventuali errori.
- La decodifica OEM di Robocopy evita caratteri italiani visualizzati con codifica errata.
- Rimossi dalla barra laterale i testi ridondanti sul funzionamento locale e sulla telemetria.
- I pannelli SSD e OneDrive indicano la disponibilità in verde o rosso e bloccano la sincronizzazione se una risorsa manca.

## [1.0.0] - 2026-08-31

### Added

- Spiegazione in-app delle differenze e dei rischi di Backup e Mirror.
- Logo originale, icona Windows multi-risoluzione e versione reale mostrata nell'app.
- Tema Sistema, Chiaro o Scuro con aggiornamento automatico dal tema di Windows.
- Attestazione Sigstore della provenienza per ogni archivio di release.
- Controllo aggiornamenti manuale e giornaliero con canali Stabile e Beta.
- Download atomico degli aggiornamenti con limite dimensione e verifica SHA-256.
- Workflow GitHub Actions aggiornati ai runtime Node.js 24.
- Updater self-contained con conferma, riavvio e ripristino su errore.
- Avvio Backup o Mirror autorizzato al collegamento dell'SSD, con delay configurabile e annullamento sicuro.

## [1.0.0-beta.1] - 2026-08-29

### Added

- Dashboard WPF e pagina Impostazioni.
- Configurazione JSON locale e versionata.
- Rilevamento stabile del volume tramite GUID, seriale e label.
- Backup, anteprima Mirror e Mirror con doppia conferma.
- Robocopy asincrono con annullamento, log e risultati comprensibili.
- Validazione fail-closed di percorsi, junction e posizione log.
- Preflight dello spazio libero e classificazione degli errori comuni.
- Build e draft release self-contained Windows x64 tramite GitHub Actions.
- Test d'integrazione reali Backup/Mirror su directory temporanee sacrificabili.
- Checksum SHA-256 automatico allegato ai pacchetti delle draft release.

### Security

- Backup predefinito e nessuna modifica della sorgente.
- Mirror bloccato senza anteprima e conferma esplicita.
- Percorsi uguali, annidati, ambigui o non verificabili vengono rifiutati.
