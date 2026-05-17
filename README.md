# TradeCopilot

TradeCopilot est un copilote patrimonial personnel pour suivre des portefeuilles financiers, calculer les positions, comparer l'allocation cible et produire une recommandation d'investissement mensuelle.

Le cahier des charges source est le fichier `Outil_de_suivi_dinvestissements.pdf`.

## Stack

- Backend : ASP.NET Core Web API, .NET 10, architecture Domain/Application/Infrastructure.
- Data : PostgreSQL via Entity Framework Core, creation et seed automatiques au demarrage API.
- Jobs cible : Hangfire ou Quartz.NET dans un increment suivant.
- Frontend : React, TypeScript, Vite, TanStack Query.
- Deploiement cible : Docker Compose, VPS, reverse proxy HTTPS.

## Structure

```text
src/
  TradeCopilot.Api/             controllers REST, bootstrap HTTP, composition
  TradeCopilot.Application/     DTOs, services applicatifs, calculs, ports
  TradeCopilot.Domain/          entites et enums metier par bounded context
  TradeCopilot.Infrastructure/  EF Core, repositories, seed, initialisation DB
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
- `GET|POST|DELETE /api/allocation-rules`
- `GET|POST|DELETE /api/strategy-rules`
- `POST /api/transactions`

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

La chaine de connexion par defaut est configuree dans `src/TradeCopilot.Api/appsettings.Development.json`. Au demarrage, l'API cree le schema avec EF Core si necessaire, puis insere le seed Quentin lorsque la base est vide.
