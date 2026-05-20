# TradeCopilot

TradeCopilot est un copilote patrimonial personnel pour suivre des portefeuilles financiers, calculer les positions, comparer l'allocation cible et produire une recommandation d'investissement mensuelle.

Le cahier des charges source est le fichier `Outil_de_suivi_dinvestissements.pdf`.

## Stack

- Backend : ASP.NET Core Web API, .NET 10, architecture Domain/Application/Infrastructure.
- Data : PostgreSQL via Entity Framework Core, creation automatique du schema au demarrage API.
- Jobs cible : Hangfire ou Quartz.NET dans un increment suivant.
- Frontend : React, TypeScript, Vite, TanStack Query.
- Deploiement cible : Docker Compose, VPS, reverse proxy HTTPS.

## Structure

```text
src/
  TradeCopilot.Api/             controllers REST, bootstrap HTTP, composition
  TradeCopilot.Application/     DTOs, services applicatifs, calculs, ports
  TradeCopilot.Domain/          entites et enums metier par bounded context
  TradeCopilot.Infrastructure/  EF Core, repositories, initialisation DB
  TradeCopilot.Client/          client React/Vite par vues fonctionnelles
tests/
  TradeCopilot.Tests/           tests calculs et allocation
docs/
  architecture.md               decisions d'architecture initiales
```

Le backend est un monolithe modulaire pret pour extraction microservices : les frontieres metier sont explicites, l'API ne manipule pas directement EF Core, les DTOs sont separes des entites, et les controllers REST sont regroupes par ressource.

## Demarrage Docker complet

La stack complete se lance avec une seule commande depuis la racine :

```powershell
docker compose up --build
```

Services inclus :

- Traefik : [http://localhost](http://localhost) et dashboard [http://localhost:8080](http://localhost:8080)
- Client React/Nginx : route `http://localhost/`
- API ASP.NET Core : route `http://localhost/api/*`
- Swagger API : [http://localhost/swagger](http://localhost/swagger)
- PostgreSQL : port local `5432`

Traefik utilise un provider fichier dans `infra/traefik/dynamic.yml` au lieu du provider Docker. Cela evite les problemes de socket Docker Desktop sous Windows tout en gardant un routage automatique via les noms de services Compose.

Si le port `80` est deja utilise sur la machine, change le mapping Traefik dans `docker-compose.yml`, par exemple `"8088:80"`, puis utilise `http://localhost:8088`.

Le Dockerfile API contourne volontairement les images `mcr.microsoft.com/dotnet/*` : il part de `debian:bookworm-slim`, installe le SDK .NET depuis `packages.microsoft.com`, puis publie l'API en binaire Linux self-contained. Cela evite les erreurs Docker Desktop du type `failed to do request ... mcr.microsoft.com ... EOF`.

Si le build echoue pendant l'installation du SDK .NET, teste l'acces reseau suivant :

```powershell
docker run --rm debian:bookworm-slim bash -lc "apt-get update && apt-get install -y curl ca-certificates && curl -I https://packages.microsoft.com"
docker compose up --build
```

Si cette commande echoue aussi, le blocage est reseau/proxy/DNS au niveau des conteneurs Docker, pas lie a TradeCopilot.

## Commandes backend hors Docker

```powershell
dotnet test TradeCopilot.slnx
dotnet run --project src/TradeCopilot.Api --urls http://localhost:5088
```

Endpoints utiles :

- `GET /api/dashboard`
- `GET /api/positions`
- `POST /api/monthly-plan` avec `{ "amount": 400 }`
- `GET /api/strategy`
- `GET|POST|PUT|DELETE /api/allocation-rules`
- `GET|POST|PUT|DELETE /api/strategy-rules`
- `GET|POST|PUT|DELETE /api/transactions`
- `POST /api/transaction-imports` en `multipart/form-data` avec `provider`, `portfolioId`, `file`
- `GET|POST|PUT|DELETE /api/prices`
- `GET /api/market-data/instruments?query=air%20liquide`
- `GET /api/market-data/quotes/AI.PA`

## Donnees de marche

Le MVP utilise un fournisseur gratuit sans cle API (`yahoo-finance`) pour deux usages :

- rechercher un instrument par nom, ticker ou ISIN depuis la page Actifs ;
- recuperer le dernier cours connu depuis la page Prix.

Le fournisseur est encapsule derriere `IMarketDataProvider`. Il peut donc etre remplace plus tard par OpenFIGI + un fournisseur de prix, Twelve Data, Finnhub, EODHD ou une source payante sans changer les controllers ni le client.

Pour les valeurs europeennes, le symbole court ne suffit souvent pas. Il faut utiliser le symbole fournisseur avec suffixe de place, par exemple `AI.PA` pour Air Liquide a Paris ou `QDVE.DE` pour l'ETF cote en Allemagne. Si un actif a ete cree avec un ticker trop court, il peut etre corrige depuis l'ecran Actifs.

## Imports CSV

Les transactions peuvent etre importees depuis l'ecran Transactions. L'utilisateur choisit explicitement la provenance du fichier avant l'upload ; le backend selectionne ensuite la strategie de parsing correspondante.

Source supportee actuellement :

- `TradeRepublic` : export CSV avec colonnes `date`, `category`, `type`, `asset_class`, `symbol`, `shares`, `price`, `amount`, `fee`, `tax`, `currency`, `transaction_id`.

Les transactions importees conservent `ImportSource` et `ExternalId` pour eviter les doublons lors d'un second import du meme fichier. Les actifs absents sont crees automatiquement avec le statut `Observation`, afin de ne pas influencer l'assistant mensuel sans validation utilisateur.

## Commandes frontend hors Docker

Le client utilise Vite recent et demande Node `^20.19.0 || >=22.12.0`. Le chemin recommande reste Docker Compose, mais le mode local fonctionne avec un Node a jour :

```powershell
cd src/TradeCopilot.Client
npm install
npm run dev
```

Le serveur Vite proxifie `/api` vers `http://localhost:5088`.

## Base PostgreSQL seule

```powershell
docker compose up -d postgres
```

La chaine de connexion par defaut est configuree dans `src/TradeCopilot.Api/appsettings.Development.json`. Au demarrage, l'API cree le schema avec EF Core si necessaire. Aucune donnee de demo n'est inseree en environnement applicatif.

Pour repartir d'une base locale vide :

```powershell
docker compose down -v
docker compose up -d
```
