# Roadmap V1

## 1. Project foundation — completata

Solution, build deterministica, test, CI, pubblicazione self-contained e
documentazione delle decisioni di sicurezza.

## 2. Configuration and path safety — completata

Modello impostazioni, persistenza atomica in AppData, esclusioni e validazione
dei percorsi con test non distruttivi. La selezione visuale sarà collegata nella
feature della dashboard.

## 3. Drive detection — completata

Acquisizione e risoluzione dell'identità del volume, cambio lettera, stati
collegato/non collegato e gestione fail-closed delle corrispondenze ambigue.

## 4. Robocopy sync engine — completata

Costruzione sicura degli argomenti, Backup/Mirror e anteprima, interpretazione
risultati, output in tempo reale, annullamento e file di log con retention.

## 5. Desktop dashboard — completata

Dashboard, pagina Impostazioni, log in tempo reale, annullamento, anteprima e
doppia conferma Mirror con messaggi orientati agli utenti non tecnici.

## 6. V1 hardening — completata

Scenari di errore, preflight spazio, packaging, documentazione finale e workflow
di draft release. La pubblicazione resta subordinata alla checklist manuale.

## Dopo la V1

- Avvio automatico al collegamento dell'SSD e sincronizzazione periodica mentre
  la sorgente resta disponibile — completati.
- Avvio con Windows per l'utente corrente — completato.
- System tray, funzionamento in background e notifica locale — completati.
- Supporto Windows ARM64.
