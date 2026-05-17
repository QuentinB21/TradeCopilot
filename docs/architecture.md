# Architecture TradeCopilot

## Source fonctionnelle

Le PDF `Outil_de_suivi_dinvestissements.pdf` est la source de verite. Le MVP couvre en priorite :

- portefeuilles PEA BoursoBank et Trade Republic ;
- actifs suivis, statuts strategiques et regles d'allocation ;
- transactions manuelles ;
- recalcul des positions depuis les transactions ;
- dashboard global ;
- assistant mensuel 80% PEA / 20% Trade Republic ;
- preparation PostgreSQL et rapports.

## Modules de code

- `Domain` : objets metier stables, sans dependance technique. Les entites et enums sont decoupes par domaine (`Portfolios`, `Assets`, `Transactions`, `Prices`, `Allocation`, `Reporting`, `Journal`).
- `Application` : contrats DTO, interfaces de services, cas d'usage, calculs purs et recommandations.
- `Infrastructure` : implementation EF Core des repositories, seed initial Quentin, initialisation DB.
- `Api` : controllers REST par ressource, configuration du pipeline et composition racine.
- `Client` : application React structuree par pages fonctionnelles.

## Convention Controller-Service-Repository

Chaque domaine expose les couches suivantes quand elles sont utiles :

- Controller : surface HTTP REST, mapping des codes de retour, aucun calcul metier.
- Service applicatif : orchestration du cas d'usage, validation simple, mapping entite vers DTO.
- Repository : persistance EF Core et requetes data.

Exemples actuels :

- `PortfoliosController` -> `IPortfolioService` -> `IInvestmentRepository`.
- `AssetsController` -> `IAssetService` -> `IInvestmentRepository`.
- `TransactionsController` -> `ITransactionService` -> `IInvestmentRepository`.
- `PricesController` -> `IPriceService` -> `IInvestmentRepository`.
- `DashboardController` -> `IDashboardQueryService` -> calculs applicatifs + repository.
- `MonthlyPlanController` -> `IInvestmentPlanService` -> calculs applicatifs + repository.

## Frontieres microservices

Le repository est actuellement un monolithe modulaire deploye en une seule API pour garder le MVP simple. Le decoupage prepare une extraction microservices par bounded context :

- Portfolio Service : portefeuilles, cash, enveloppes.
- Asset Service : referentiel actifs, statuts strategiques, metadata.
- Transaction Service : journal immutable des operations.
- Pricing Service : cours, historique, cache fournisseur.
- Allocation Service : regles cible, ecarts, reequilibrage.
- Reporting Service : snapshots, rapports periodiques, exports.
- Advisory Service : assistant mensuel et recommandations consultatives.

Les services applicatifs ne dependent pas d'ASP.NET Core ni d'EF Core directement ; ils consomment des abstractions. C'est ce qui permettra de remplacer un repository local par un client HTTP/message bus lors de l'extraction d'un microservice.

## Frontend

Le client React ne presente plus de menu factice. Les vues disponibles correspondent a des fonctionnalites branchees sur l'API :

- Dashboard : valeur, performance, repartition, positions, ecarts cible.
- Portefeuilles : creation et modification.
- Actifs : creation et modification du referentiel.
- Transactions : saisie append-only et historique.
- Prix : saisie manuelle des cours et historique.
- Assistant : generation du plan mensuel.
- Strategie : lecture des regles configurees.

## Decisions initiales

1. Les positions sont derivees des transactions, jamais saisies manuellement.
2. Les gains realises utilisent le prix moyen d'achat au moment de la vente.
3. Les lignes `Observation`, `Frozen` et `PlannedExit` ne sont pas renforcees par l'assistant mensuel.
4. Les operations d'investissement restent consultatives : aucune execution reelle n'est declenchee.
5. Les parametres propres a Quentin sont seedes comme donnees, pas codes en dur dans les calculs.

## Increments suivants

1. Ajouter migrations EF Core et repositories persistants.
2. Ajouter authentification obligatoire avec 2FA optionnelle.
3. Brancher un fournisseur de prix avec cache, retries et quotas.
4. Generer les snapshots quotidiens de positions.
5. Ajouter rapports mensuels consultables et export PDF.
6. Ajouter imports CSV BoursoBank et Trade Republic.
