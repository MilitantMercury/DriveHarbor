# Validazione release

## Ambito automatizzato

La suite include test d'integrazione che invocano il `robocopy.exe` reale di
Windows esclusivamente dentro una directory temporanea univoca del runner.
Nessun percorso dell'utente viene selezionato o enumerato.

### Backup

Il test prepara file nuovi, un file modificato e un file presente soltanto nella
destinazione. Verifica che:

- i file nuovi e modificati arrivino nella destinazione;
- il file extra nella destinazione non venga eliminato;
- nomi e hash SHA-256 di tutti i file sorgente restino invariati.

### Mirror

Il test usa una destinazione sacrificabile contenente un file obsoleto. Verifica
che:

- il file obsoleto venga eliminato soltanto dalla destinazione;
- la destinazione finale abbia gli stessi nomi e hash della sorgente;
- nomi e hash SHA-256 della sorgente restino invariati.

Le fixture vengono eliminate al termine di ogni test. Il successo è attestato
dal controllo `build-and-test` della Pull Request che introduce questi test e
dalle esecuzioni successive su `main`.

## Verifiche ancora manuali

Restano intenzionalmente manuali i test della UI, l'annullamento durante una
copia di dimensioni realistiche, lo scollegamento fisico dell'SSD, la scansione
antivirus, la firma e la verifica su un'installazione Windows 11 pulita. Questi
punti non devono essere marcati come completati sulla sola base della CI.

## Provenienza del pacchetto

Il workflow associato ai tag genera una build provenance firmata tramite
Sigstore e GitHub OIDC per lo ZIP Windows x64. L'attestazione lega il digest del
pacchetto al repository, al commit, al tag e al workflow che lo ha prodotto. Il
bundle viene allegato alla draft release e GitHub conserva la stessa
attestazione nel proprio servizio.

Il checksum rileva corruzioni accidentali; la verifica dell'attestazione è il
controllo necessario per stabilire la provenienza della build. Non equivale a
una firma Authenticode e non rimuove gli avvisi SmartScreen.
