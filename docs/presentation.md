# TradeCopilot

TradeCopilot est une application web de suivi patrimonial conçue pour aider les investisseurs particuliers à mieux comprendre, organiser et piloter leurs investissements.

L'objectif est simple : centraliser ses portefeuilles, visualiser l'évolution de son patrimoine, comparer son allocation réelle avec sa stratégie cible, puis obtenir une aide à la décision pour ses prochains investissements.

## Pourquoi ce projet ?

Quand on investit sur plusieurs supports, il devient vite difficile de répondre clairement à des questions pourtant essentielles :

- Quelle est la valeur actuelle de mon patrimoine investi ?
- Combien ai-je réellement investi ?
- Quelle est ma performance globale ?
- Mon allocation correspond-elle encore à ma stratégie ?
- Où devrais-je investir en priorité ce mois-ci ?

TradeCopilot répond à ces questions avec une interface claire, pensée pour suivre ses décisions dans le temps et éviter de piloter son portefeuille uniquement à l'intuition.

## Fonctionnalités principales

- Suivi de plusieurs portefeuilles d'investissement.
- Gestion des actifs détenus : ETF, actions, crypto-actifs ou autres supports.
- Import et saisie de transactions.
- Calcul des positions, du prix de revient, des plus-values latentes et de l'allocation réelle.
- Visualisation de l'évolution globale du patrimoine.
- Définition d'une stratégie cible par portefeuille et par actif.
- Règles personnalisées pour encadrer les décisions d'investissement.
- Assistant mensuel pour suggérer où investir selon la stratégie définie.
- Import et export des règles de stratégie pour faciliter la configuration d'un nouveau compte.

## Mode invité

Un mode invité permet de découvrir TradeCopilot sans créer de compte.

Ce mode ouvre un jeu de données de démonstration en lecture seule. Il permet d'explorer le dashboard, les portefeuilles, les actifs, les transactions, la stratégie et l'assistant mensuel sans risque de modifier les données.

## Sécurité et confidentialité

L'application utilise une authentification sécurisée avec Keycloak.

Chaque utilisateur possède son propre espace de données. Les portefeuilles, actifs, transactions, règles et prix sont isolés par compte utilisateur, afin qu'un utilisateur ne puisse pas accéder aux données d'un autre.

Le mode invité est lui aussi isolé dans un espace technique dédié et reste limité à la lecture.

## Stack technique

TradeCopilot est développé avec :

- React, TypeScript et Vite pour l'interface web.
- ASP.NET Core pour l'API backend.
- PostgreSQL pour la persistance des données.
- Entity Framework Core pour l'accès aux données.
- Keycloak pour l'authentification.
- Docker Compose pour l'exécution locale et le déploiement VPS.

## Statut

TradeCopilot est actuellement un MVP fonctionnel.

Le projet continue d'évoluer autour de trois axes principaux :

- améliorer l'expérience utilisateur ;
- renforcer l'analyse patrimoniale ;
- préparer une utilisation publique plus robuste et sécurisée.

## Accès

- Application : [https://quentin-bouchot.fr/projets/TradeCopilot/](https://quentin-bouchot.fr/projets/TradeCopilot/)
- Dépôt GitHub : [https://github.com/QuentinB21/TradeCopilot](https://github.com/QuentinB21/TradeCopilot)

## Note

TradeCopilot est un outil d'aide au suivi et à la décision. Il ne fournit pas de conseil financier personnalisé et ne remplace pas l'analyse ou la responsabilité de l'utilisateur dans ses choix d'investissement.
