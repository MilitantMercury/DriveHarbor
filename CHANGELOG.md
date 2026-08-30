# Changelog

Tutte le modifiche rilevanti di DriveHarbor sono documentate in questo file.
Il progetto segue il versionamento semantico.

## [Unreleased]

### Added

- Spiegazione in-app delle differenze e dei rischi di Backup e Mirror.
- Logo originale, icona Windows multi-risoluzione e versione reale mostrata nell'app.
- Tema Sistema, Chiaro o Scuro con aggiornamento automatico dal tema di Windows.
- Attestazione Sigstore della provenienza per ogni archivio di release.
- Controllo aggiornamenti manuale e giornaliero con canali Stabile e Beta.

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
