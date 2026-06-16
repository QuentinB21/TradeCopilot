# Deploiement VPS sous le portfolio

Objectif public :

```text
https://quentin-bouchot.fr/projets/TradeCopilot/
```

Le portfolio reste le proprietaire du domaine, du HTTPS et du reverse proxy Caddy. TradeCopilot reste dans son propre repo, avec son propre Docker Compose, sa base PostgreSQL et son Keycloak.

## Architecture cible

```text
/opt/portfolio_website   repo portfolio, Caddy public
/opt/tradecopilot        repo TradeCopilot
```

Un reseau Docker externe permet a Caddy de joindre TradeCopilot sans exposer de port public :

```bash
docker network create public-proxy
```

Le compose TradeCopilot de production utilise :

- `public-proxy` : uniquement pour `client`, `api`, `keycloak`
- `tradecopilot-internal` : pour les bases PostgreSQL et les flux internes

Dans le compose de production du portfolio, le service `caddy` doit aussi rejoindre ce reseau externe :

```yaml
services:
  caddy:
    networks:
      - default
      - public-proxy

networks:
  public-proxy:
    external: true
```

## Configuration Caddy cote portfolio

Dans le Caddyfile du portfolio, ajouter ces routes avant le `handle { reverse_proxy portfolio:80 }` :

```caddyfile
redir /projets/TradeCopilot /projets/TradeCopilot/ permanent

handle /projets/TradeCopilot/auth/callback* {
	uri strip_prefix /projets/TradeCopilot
	reverse_proxy tradecopilot-client:80
}

handle /projets/TradeCopilot/auth/* {
	uri strip_prefix /projets/TradeCopilot
	reverse_proxy tradecopilot-keycloak:8080
}

handle /projets/TradeCopilot/api/* {
	uri strip_prefix /projets/TradeCopilot
	reverse_proxy tradecopilot-api:8080
}

handle /projets/TradeCopilot/* {
	uri strip_prefix /projets/TradeCopilot
	reverse_proxy tradecopilot-client:80
}
```

Le `strip_prefix` est volontaire : l'API continue d'exposer `/api/*`, Keycloak continue d'exposer `/auth/*`, et le client Nginx sert le SPA comme s'il etait a la racine du conteneur.

## Variables TradeCopilot

Sur le VPS :

```bash
cd /opt/tradecopilot
cp .env.prod.example .env.prod
nano .env.prod
```

Changer tous les mots de passe :

- `KC_BOOTSTRAP_ADMIN_PASSWORD`
- `KEYCLOAK_DB_PASSWORD`
- `TRADECOPILOT_DB_PASSWORD`

Les URLs publiques attendues pour l'integration avec le portfolio sont deja renseignees dans `.env.prod.example`.

## Premier deploiement manuel

```bash
docker network inspect public-proxy >/dev/null 2>&1 || docker network create public-proxy
docker compose --env-file .env.prod -f docker-compose.prod.yml up -d --build
```

Verification :

```bash
docker compose --env-file .env.prod -f docker-compose.prod.yml ps
docker compose --env-file .env.prod -f docker-compose.prod.yml logs -f client
docker compose --env-file .env.prod -f docker-compose.prod.yml logs -f api
docker compose --env-file .env.prod -f docker-compose.prod.yml logs -f keycloak
```

## Keycloak

Le realm importe le client `tradecopilot-client` avec :

- `Valid redirect URIs` : `https://quentin-bouchot.fr/projets/TradeCopilot/auth/callback`
- `Web origins` : `https://quentin-bouchot.fr`
- `Valid post logout redirect URIs` : `https://quentin-bouchot.fr/projets/TradeCopilot/`

Si le realm existe deja, l'import ne remplace pas forcement la configuration. Dans ce cas, verifier ces valeurs dans l'admin Keycloak.

## CI/CD GitHub Actions

Le workflow `.github/workflows/deploy-production.yml` :

1. lance `dotnet test TradeCopilot.slnx`
2. compile le client avec la base `/projets/TradeCopilot/`
3. se connecte au VPS en SSH
4. cree le reseau `public-proxy` s'il manque
5. pull `main`
6. relance `docker compose --env-file .env.prod -f docker-compose.prod.yml up -d --build --remove-orphans`

Secrets GitHub attendus dans l'environnement `production` :

```text
SSH_HOST
SSH_PORT
SSH_USER
SSH_PRIVATE_KEY
DEPLOY_PATH
```

Exemple `DEPLOY_PATH` :

```text
/opt/tradecopilot
```

## Tests publics

Depuis un navigateur :

1. `https://quentin-bouchot.fr/` doit encore afficher le portfolio.
2. `https://quentin-bouchot.fr/projets/TradeCopilot/` doit afficher TradeCopilot.
3. `https://quentin-bouchot.fr/projets/TradeCopilot/api/dashboard` doit repondre `401` si non connecte.
4. `https://quentin-bouchot.fr/projets/TradeCopilot/auth/realms/tradecopilot/.well-known/openid-configuration` doit repondre le document OIDC.
5. Une connexion Keycloak doit revenir vers `/projets/TradeCopilot/auth/callback`, puis l'URL doit etre nettoyee vers `/projets/TradeCopilot/`.

## Points de vigilance

- Ne pas exposer PostgreSQL, Keycloak DB ou pgAdmin en production.
- Ne pas ajouter Traefik en production : Caddy du portfolio est le seul reverse proxy public.
- Garder `AUTH_REQUIRE_HTTPS_METADATA=false` tant que l'API lit la metadata Keycloak via `http://keycloak:8080` sur le reseau Docker interne. Le trafic navigateur reste en HTTPS via Caddy.
- Relancer le build client apres toute modification de `VITE_APP_BASE_PATH`, `VITE_API_BASE_URL` ou `VITE_AUTH_AUTHORITY`.
