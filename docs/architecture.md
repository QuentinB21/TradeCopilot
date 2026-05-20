# Architecture TradeCopilot

## Source fonctionnelle

Le PDF `Outil_de_suivi_dinvestissements.pdf` est la source de verite. Le MVP couvre en priorite :

- portefeuilles configurables par l'utilisateur ;
- actifs suivis, positions d'ouverture, statuts strategiques et regles d'allocation ;
- transactions manuelles modifiables en cas d'erreur de saisie ;
- recalcul des positions depuis les transactions ;
- dashboard global ;
- assistant mensuel pilote par les cles de repartition configurees ;
- preparation PostgreSQL et rapports.

## Modules de code

- `Domain` : objets metier stables, sans dependance technique. Les entites et enums sont decoupes par domaine (`Portfolios`, `Assets`, `Transactions`, `Prices`, `Allocation`, `Strategy`, `Reporting`, `Journal`).
- `Application` : contrats DTO, interfaces de services, cas d'usage, calculs purs et recommandations.
- `Infrastructure` : implementation EF Core des repositories et initialisation DB.
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
- `TransactionImportsController` -> `ITransactionImportService` -> `ITransactionImportStrategy` -> `IInvestmentRepository`.
- `PricesController` -> `IPriceService` -> `IInvestmentRepository`.
- `MarketDataController` -> `IMarketDataService` -> `IMarketDataProvider`.
- `AllocationRulesController` -> `IAllocationRuleService` -> `IInvestmentRepository`.
- `StrategyRulesController` -> `IStrategyRuleService` -> `IInvestmentRepository`.
- `DashboardController` -> `IDashboardQueryService` -> calculs applicatifs + repository.
- `MonthlyPlanController` -> `IInvestmentPlanService` -> calculs applicatifs + repository.

## Frontieres microservices

Le repository est actuellement un monolithe modulaire deploye en une seule API pour garder le MVP simple. Le decoupage prepare une extraction microservices par bounded context :

- Portfolio Service : portefeuilles, cash, enveloppes.
- Asset Service : referentiel actifs, statuts strategiques, metadata.
- Transaction Service : journal immutable des operations.
- Import Service : strategies d'import par fournisseur, normalisation des lignes CSV et dedoublonnage.
- Pricing Service : cours, historique, cache fournisseur.
- Market Data Service : recherche d'instruments, dernier cours et normalisation fournisseur.
- Allocation Service : regles cible, ecarts, reequilibrage.
- Reporting Service : snapshots, rapports periodiques, exports.
- Advisory Service : assistant mensuel et recommandations consultatives.

Les services applicatifs ne dependent pas d'ASP.NET Core ni d'EF Core directement ; ils consomment des abstractions. C'est ce qui permettra de remplacer un repository local par un client HTTP/message bus lors de l'extraction d'un microservice.

## Frontend

Le client React ne presente plus de menu factice. Les vues disponibles correspondent a des fonctionnalites branchees sur l'API :

- Dashboard : valeur, performance, repartition, positions, ecarts cible.
- Portefeuilles : creation et modification.
- Actifs : creation et modification du referentiel.
- Transactions : saisie, correction et suppression des positions d'ouverture et mouvements.
- Imports CSV : selection de la provenance, upload du fichier, creation des actifs manquants et import dedoublonne.
- Prix : saisie, correction et suppression des cours manuels.
- Donnees de marche : recherche nom/ticker/ISIN et recuperation d'un dernier cours via fournisseur gratuit interchangeable.
- Assistant : generation du plan mensuel.
- Strategie : configuration des portefeuilles, actifs, cles de repartition globales, cles par ligne et regles de decision.

## Decisions initiales

1. Les positions sont derivees des transactions. Une position deja detenue est saisie comme position d'ouverture via une transaction d'achat ou d'ajustement.
2. Les gains realises utilisent le prix moyen d'achat au moment de la vente.
3. Les lignes `Observation`, `Frozen` et `PlannedExit` ne sont pas renforcees par l'assistant mensuel.
4. Les operations d'investissement restent consultatives : aucune execution reelle n'est declenchee.
5. Les donnees de demo restent disponibles dans les tests, mais l'application demarre avec une base vide.
6. Les suppressions de portefeuilles et d'actifs references sont bloquees avec un `409 Conflict` pour eviter les cascades destructrices.
7. Les donnees de marche passent par un port applicatif. Le fournisseur gratuit actuel est pratique pour le MVP, mais volontairement interchangeable car il n'offre pas de contrat de service officiel.
8. Les imports CSV utilisent un pattern Strategy : chaque provenance possede son parseur, ses mappings et ses regles de lignes ignorees. Trade Republic est la premiere strategie supportee.

## Increments suivants

1. Ajouter migrations EF Core et repositories persistants.
2. Ajouter authentification obligatoire avec 2FA optionnelle.
3. Ajouter cache, retries, backoff et journalisation des appels au fournisseur de prix.
4. Generer les snapshots quotidiens de positions.
5. Ajouter rapports mensuels consultables et export PDF.
6. Ajouter imports CSV BoursoBank et autres banques/courtiers.
