SAMPLE di  **sequenza completa di step** già pronta da dare in pasto a Cloud Code per sviluppare una **.NET 8 Minimal API** con **REST + WebSocket + Docker**, usando la strategia “mista” (lista completa + esecuzione step-by-step).

---

## 📝 Prompt pronto all’uso per Cloud Code

> **Contesto**: Sto sviluppando un’applicazione **.NET 8 Minimal API** che fornisce sia endpoint REST che WebSocket, pensata per girare in **Docker** su un ambiente a risorse limitate (ARM32, 1GB RAM).
> L’app deve includere: autenticazione JWT, persistenza su database SQLite, logging, gestione errori, e un canale WebSocket per notifiche in tempo reale.
>
> **Macro-task complessivo**:
> **Step 1** – Creare nuovo progetto `.NET 8 Minimal API` con:
>
> * Struttura di cartelle pulita (`/src`, `/Data`, `/Models`, `/Services`, `/Controllers`, `/Middleware`)
> * Configurazione logging base
> * Swagger/OpenAPI abilitato
> * Gestione configurazioni da `appsettings.json`
> * Middleware base per eccezioni globali
>
> **Step 2** – Implementare la risorsa principale `[NomeEntità]` (es. `Message`, `Task`, ecc.):
>
> * Modello EF Core
> * DbContext con SQLite
> * Endpoint CRUD REST minimal API
> * Migrazioni e seed iniziale dati
>
> **Step 3** – Aggiungere autenticazione JWT:
>
> * Registrazione/login utente
> * Protezione endpoint riservati
> * Middleware di validazione token
>
> **Step 4** – Implementare WebSocket:
>
> * Endpoint WS per notifiche
> * Servizio per invio eventi in tempo reale su modifiche DB
> * Gestione connessioni multiple
>
> **Step 5** – Validazione e gestione errori:
>
> * `FluentValidation` per validare input
> * Middleware personalizzato per formattare errori
>
> **Step 6** – Creare file `Dockerfile` ottimizzato:
>
> * Multi-stage build per ridurre dimensione immagine
> * Compatibilità ARM32
> * Configurazione per esecuzione in 1GB RAM
>
> **Step 7** – Scrivere test automatizzati:
>
> * Test unitari con `xUnit`
> * Test di integrazione con WebApplicationFactory
>
> **Istruzioni**:
>
> * Ora esegui **solo lo Step 1**.
> * Fornisci il codice completo e spiegazioni tecniche.
> * Chiedi conferma prima di procedere allo Step 2.
> * Mantieni lo stato e il contesto degli step già completati.
> * Se trovi ambiguità tecniche, chiedi chiarimenti prima di scrivere il codice.

---

📦 **Come usarlo in Cloud Code**

1. Copia e incolla l’intero prompt.
2. Quando l’AI completa uno step, rivedi e conferma.
3. Procedi col comando: “Procedi con Step X”.
4. Mantieni tutto il codice generato in un repository locale per non perdere lo stato se la sessione si resetta.

---

Se vuoi, posso anche prepararti **la versione dello Step 1 già completa di codice .NET 8 Minimal API** con struttura cartelle, logging, Swagger, e middleware di base, così quando incolli il prompt in Cloud Code lui parte direttamente dal codice e non deve inventare tutto da zero.
Vuoi che ti faccia subito anche **Step 1** in codice?



============
TODO LIST:
============
- [ ] Review MachineLearningService
- [ ] Clear project

> **Step 1** – Review MachineLearningService:
> * Check current implementation
> * Add tests for existing methods
> * Review and optimize code
> * propose improvements
> * propose new features to increates the performance of the model
> * propose alternative approaches to dotnet ML
>
> > **Step 2** – Move domain to  `[FileCategorization_SHARED]`
> * Re-organize Entities structure: separate domain models to DTOs and Entities
> * Move domain models to shared project
> * Move common services to shared project
> * Move common utilities to shared project
> * 
    > **Step 3** – Clear project `[FileCategorization_API]`
> * Remove unused files
> * Remove Decprecated endpoints 
> * Remove unused services 
> * 
> * 