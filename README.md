# FlowForge

FlowForge est un prototype Unity de simulation-gestion Lean. Le joueur améliore progressivement des ateliers de production en réduisant les gaspillages, en stabilisant les standards, en améliorant la qualité et en respectant les personnes.

## Contenu livré

- Concept complet et progression des deux premiers mondes : [`docs/GAME_DESIGN.md`](docs/GAME_DESIGN.md)
- Architecture Unity orientée composants dans `Assets/FlowForge/Scripts`
- ScriptableObjects pour machines, mondes, niveaux, missions et outils Lean
- Prototype runtime auto-généré : grille, machines, opérateurs, flux, KPI, 5S et score

## Lancer le prototype

1. Ouvrir le dossier avec Unity 2022.3 LTS ou supérieur.
2. Ouvrir la scène `Assets/FlowForge/Scenes/FlowForgePrototype.unity`.
3. Appuyer sur Play.

La scène contient un GameObject `GameManager`. Si vous devez reconstruire la scène complète avec les objets concrets déjà placés dans la hiérarchie, utilisez le menu Unity **Tools > FlowForge > Build Prototype Scene**. La scène jouable contient caméra isométrique, lumière, grille, trois machines, un opérateur, le HUD et le bouton **Appliquer 5S**.

## Philosophie

Le score ne récompense pas uniquement l'argent. Il valorise le service client, la qualité, le flux, la réduction des gaspillages et le stress d'équipe maîtrisé.
