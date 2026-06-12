# FlowForge

FlowForge est un prototype Unity de simulation-gestion Lean. Le joueur améliore progressivement des ateliers de production en réduisant les gaspillages, en stabilisant les standards, en améliorant la qualité et en respectant les personnes.

## Contenu livré

- Concept complet et progression des deux premiers mondes : [`docs/GAME_DESIGN.md`](docs/GAME_DESIGN.md)
- Architecture Unity orientée composants dans `Assets/FlowForge/Scripts`
- ScriptableObjects pour machines, mondes, niveaux, missions et outils Lean
- Prototype runtime auto-généré : grille, machines, opérateurs, flux, KPI, 5S et score

## Lancer le prototype

1. Ouvrir le dossier avec Unity 2022.3 LTS ou supérieur.
2. Créer une scène vide.
3. Ajouter un GameObject vide nommé `FlowForge Bootstrap`.
4. Ajouter le composant `FlowForge.Core.GameBootstrap`.
5. Appuyer sur Play.

Le bootstrap crée automatiquement la caméra, la lumière, la grille, les postes, les opérateurs, la simulation et le HUD. Cliquez sur **Appliquer 5S** pour voir une amélioration visible et mesurable.

## Philosophie

Le score ne récompense pas uniquement l'argent. Il valorise le service client, la qualité, le flux, la réduction des gaspillages et le stress d'équipe maîtrisé.
