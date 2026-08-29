# Checklist release

Una release non deve essere pubblicata finché tutti i punti applicabili non sono
stati verificati manualmente.

## Qualità

- [ ] La PR di release è approvata e la pipeline è verde.
- [ ] Versione aggiornata in `Directory.Build.props` e changelog finalizzato.
- [ ] Build self-contained avviata su un sistema Windows 11 pulito.
- [ ] Dashboard, selezione cartelle e persistenza verificate manualmente.
- [ ] Annullamento verificato durante una copia di test isolata.

## Sicurezza dati

- [ ] Backup verificato soltanto su directory temporanee dedicate.
- [ ] Mirror verificato soltanto su directory temporanee sacrificabili.
- [ ] Confermato che nessun test usa directory reali dell'utente.
- [ ] Testati SSD scollegato e destinazione rimossa prima dell'avvio.
- [ ] Testato scollegamento del supporto durante una copia isolata.
- [ ] Testati spazio insufficiente, accesso negato e file occupato.
- [ ] Verificato che sorgente, destinazione e log non siano annidati.
- [ ] Verificato che la sorgente non venga mai modificata.

## Distribuzione

- [ ] Artifact sottoposto a scansione antivirus.
- [ ] Hash SHA-256 registrato nelle note della release.
- [ ] Firma del codice applicata, oppure assenza della firma dichiarata.
- [ ] Draft release revisionata manualmente prima della pubblicazione.

Il workflow associato ai tag `v*` crea deliberatamente una release in stato
**draft**. La pubblicazione resta sempre un'azione manuale.
