# FlowForge — Concept complet

## 1. Vision

**FlowForge** est un jeu de simulation-gestion Lean en ateliers stylisés. Le joueur ne gagne pas seulement en générant du profit : il gagne en créant de la valeur client avec moins de gaspillage, plus de qualité, un meilleur flux et des équipes moins stressées.

Le ton est premium, clair et positif : ateliers lumineux, machines lisibles, opérateurs animés, flux visibles, effets immédiats lors des améliorations. L'objectif émotionnel est : **« encore une amélioration et j'arrête »**.

## 2. Piliers de design

1. **Observer avant d'optimiser** : les problèmes se voient dans l'atelier (stocks qui gonflent, opérateurs qui marchent trop, machines rouges, rebuts visibles).
2. **Lean crédible** : valeur client, réduction des gaspillages, respect des personnes, standards stables, qualité intégrée et résolution à la cause racine.
3. **Données digestes** : les KPI existent, mais sous forme de jauges, tendances et alertes visuelles, pas de tableur.
4. **Feedback gratifiant** : chaque action Lean produit un avant/après visuel, une mini-célébration et une amélioration mesurable.
5. **Sessions courtes** : niveaux de 5 à 12 minutes avec objectifs principaux, objectifs secondaires et badges.
6. **Architecture évolutive** : mondes, niveaux, missions, machines et outils Lean configurés par ScriptableObjects.

## 3. Boucle de gameplay

1. Le joueur reçoit des commandes clients avec exigences de délai, quantité et qualité.
2. Il observe l'atelier : flux, WIP, opérateurs, pannes, rebuts, attentes et goulots.
3. Il place ou améliore postes, machines, contrôles qualité, zones de stock et opérateurs.
4. La production tourne en temps réel accéléré.
5. Des gaspillages apparaissent : défauts, retouches, attentes, transports, stocks, surprocess, mouvements inutiles, pannes.
6. Le joueur consulte des indicateurs Lean simples : rebuts, lead time, OEE/TRS, service, WIP, attente, distance, retouches, CNQ, satisfaction, stress, score Lean.
7. Il applique une action Lean adaptée : 5S, standard work, poka-yoke, Kanban, SMED, TPM, Andon, etc.
8. L'atelier change visuellement : trajets plus courts, zones rangées, machines plus stables, défauts évités, flux plus fluide.
9. Il gagne argent, réputation, étoiles, badges et nouveaux outils.
10. Il débloque un niveau plus complexe ou un challenge quotidien.

## 4. Indicateurs intégrés

- Taux de rebuts : part d'ordres ayant généré un défaut.
- Lead time : durée moyenne entre création et livraison d'une commande.
- OEE/TRS : disponibilité × performance × qualité.
- Taux de service client : commandes livrées par rapport aux commandes reçues.
- Niveau de stock/WIP : files d'attente entre postes.
- Temps d'attente : cumul du WIP dans le temps.
- Distance parcourue : déplacements opérateurs observables.
- Nombre de retouches : défauts nécessitant reprise.
- Coût de non-qualité : pénalité économique des défauts.
- Satisfaction client : composite délai, qualité et sérénité d'équipe.
- Stress équipe : monte avec déplacements et instabilité, baisse avec standards et ergonomie.
- Score Lean global : score composite non financier.

## 5. Outils Lean et progression

| Déblocage | Outil | Problème adressé | Feedback visuel |
|---|---|---|---|
| Monde 1 niveau 1 | 5S | mouvements inutiles, désordre, erreurs simples | postes rangés, trajets plus courts, éclats dorés |
| Niveau 2 | Management visuel | problèmes invisibles | panneaux, couleurs de statut, alertes simples |
| Niveau 3 | Standard work | variabilité opérateur | cadence plus régulière, gestes synchronisés |
| Niveau 4 | Poka-yoke | erreurs humaines | défauts bloqués avant avancement |
| Niveau 5 | Kaizen | petites améliorations fréquentes | micro-upgrades cumulables |
| Niveau 6 | SMED | changement de série long | animation de setup raccourcie |
| Niveau 7 | Kanban | surstock/WIP | cartes tirées, files plafonnées |
| Niveau 8 | 5 Pourquoi | problème récurrent | arbre de cause racine |
| Niveau 9 | Ishikawa | défaut complexe | diagramme cause-effet simplifié |
| Niveau 10 | TPM/Andon | panne et réaction lente | machine stable, appel aide rapide |
| Monde 2 | VSM/Heijunka/Gemba | flux global, nivellement, observation terrain | carte de flux, lissage, parcours manager |

## 6. Monde 1 — Atelier de montres de luxe

Ambiance : précision, calme, prestige, composants précieux, lots faibles et qualité absolue. Les rebuts sont rares mais très coûteux, les contrôles longs et les retouches sensibles.

### 10 premiers niveaux

| # | Nom | Problème Lean introduit | Objectif principal | Objectifs secondaires | Outil débloqué |
|---|---|---|---|---|---|
| 1 | Premier calibre | Atelier désordonné et trajets inutiles | Atteindre 60 de score Lean | Appliquer 5S, livrer 20 commandes | 5S |
| 2 | Loupes et signaux | Défauts difficiles à repérer | Réduire rebuts de 18 % à 12 % | Installer management visuel, garder stress < 45 % | Management visuel |
| 3 | Geste horloger | Variabilité des temps opérateur | Réduire lead time de 20 % | Maintenir satisfaction > 80 % | Standard work |
| 4 | Micro-vis critique | Erreurs humaines évitables | Passer les rebuts sous 8 % | Installer poka-yoke, CNQ < 500 € | Poka-yoke |
| 5 | Série limitée | Petits kaizens successifs | Gagner 3 étoiles | Réaliser 3 kaizens, OEE > 70 % | Kaizen |
| 6 | Changement de bracelet | Setup trop long entre références | Réduire changement de série de 30 % | Livrer 40 commandes | SMED |
| 7 | Composants précieux | Stock coûteux et surprotection | Réduire WIP moyen de 35 % | Aucun retard majeur | Kanban |
| 8 | Panne récurrente | Même défaut revient sans cause claire | Identifier la cause racine | Utiliser 5 Pourquoi, zéro récidive | 5 Pourquoi |
| 9 | Défaut esthétique | Causes multiples qualité | Réduire retouches de 40 % | Construire Ishikawa complet | Ishikawa |
| 10 | Certification maison | Synthèse flux, qualité, personnes | Score Lean > 85 | Stress < 35 %, satisfaction > 92 % | Gemba walk |

## 7. Monde 2 — Usine automobile

Ambiance : cadence, volume, robotisation, logistique et synchronisation. Les lignes sont plus longues, les stocks intermédiaires deviennent visibles et les pannes ont un effet de chaîne.

### 10 premiers niveaux

| # | Nom | Problème Lean introduit | Objectif principal | Objectifs secondaires | Outil clé |
|---|---|---|---|---|---|
| 1 | Démarrage de ligne | Cadence et goulot simple | Livrer 100 commandes à 90 % service | Identifier poste goulot | Management visuel |
| 2 | Stock tampon | WIP excessif entre postes | Réduire stock de 30 % | Lead time -15 % | Kanban |
| 3 | Robot capricieux | Panne machine fréquente | OEE > 72 % | Déclencher Andon en < 10 s | Andon |
| 4 | Maintenance autonome | Arrêts mineurs répétés | Réduire pannes de 40 % | Former opérateur TPM | TPM |
| 5 | Ligne déséquilibrée | Attentes opérateurs | Équilibrer cadence poste par poste | Attente -25 % | Standard work |
| 6 | Changement modèle | Setup carrosserie long | SMED -35 % | Service > 95 % | SMED |
| 7 | Défaut en série | Mauvaise pièce montée en lots | Rebuts < 5 % | Installer poka-yoke | Poka-yoke |
| 8 | Logistique saturée | Flux internes trop longs | Distance -30 % | Stress < 50 % | VSM |
| 9 | Mix produit instable | Pics et creux de demande | Lisser production sans retard | WIP stable | Heijunka |
| 10 | Audit excellence flux | Synthèse ligne complète | Score Lean > 88 | OEE > 82 %, satisfaction > 90 % | Gemba walk |

## 8. Direction UX et visuelle

- Vue 2.5D isométrique avec caméra orthographique.
- Couleurs premium : fonds doux, accents or/cuivre pour montres, bleu/acier pour automobile.
- Machines et opérateurs lisibles à petite taille.
- Flux matérialisé par objets, cartes Kanban, jauges au-dessus des postes.
- Rouge = anomalie, jaune = attention, vert/bleu = flux stable.
- Les KPI sont des cartes courtes, avec tendance et icône.
- Les actions Lean sont des cartes tactiles utilisables à la souris ou au doigt.

## 9. Prototype Unity livré

Le prototype minimal contient :

- Une grille d'atelier générée au runtime.
- Trois postes : préparation composants, assemblage mouvement, contrôle qualité.
- Deux opérateurs animés qui se déplacent entre postes.
- Un flux d'ordres client passant de poste en poste.
- Des défauts, retouches, coût de non-qualité et pannes simples.
- Un tableau d'indicateurs Lean.
- Une action 5S applicable une fois, avec réduction de temps, trajets, défauts et stress.
- Un score Lean et des étoiles.
- Des ScriptableObjects prêts pour machines, niveaux, missions, mondes et outils.
- Une sauvegarde PlayerPrefs de la progression prototype.

## 10. Architecture de code

```text
Assets/FlowForge/Scripts
├── Core
│   ├── GameBootstrap.cs          # Point d'entrée runtime, crée caméra/lumière/simulation/UI
│   ├── SimulationController.cs   # Orchestration du flux et des ordres
│   ├── ScoreSystem.cs            # Conversion KPI -> score/étoiles
│   └── SaveSystem.cs             # Sauvegarde progression simple
├── Data
│   ├── MachineConfig.cs          # ScriptableObject machine/poste
│   ├── LevelConfig.cs            # ScriptableObject niveau
│   ├── MissionConfig.cs          # ScriptableObject objectif
│   ├── LeanToolConfig.cs         # ScriptableObject outil Lean
│   └── WorldConfig.cs            # ScriptableObject monde
├── Lean
│   └── LeanImprovementSystem.cs  # Application des outils Lean
├── Simulation
│   ├── WorkshopGrid.cs           # Grille atelier
│   ├── Workstation.cs            # Machine/poste runtime
│   ├── OperatorAgent.cs          # Avatar opérateur
│   ├── ProductionOrder.cs        # Commande/produit
│   └── MetricsTracker.cs         # KPI Lean
└── UI
    └── LeanDashboardUI.cs        # HUD minimal PC/mobile
```

### Séparation des responsabilités

- **Données** : ScriptableObjects configurables par designers.
- **Logique métier** : simulation, KPI, score, sauvegarde.
- **Présentation** : HUD et feedback visuel.
- **Évolution mondes** : ajouter un `WorldConfig`, ses `LevelConfig`, ses machines et missions sans réécrire la simulation.

## 11. Roadmap prototype vers vertical slice

1. Ajouter une scène Unity versionnée avec prefabs premium.
2. Remplacer UI legacy par UI Toolkit ou uGUI prefab responsive.
3. Ajouter placement de machines par drag-and-drop sur grille.
4. Ajouter files WIP visibles physiquement.
5. Ajouter tutoriels contextuels et challenges quotidiens.
6. Ajouter VSM, Kanban, SMED et cause racine en mini-interactions.
7. Équilibrer niveaux pour sessions de 5 à 12 minutes.
8. Ajouter analytics internes : taux de réussite, temps par niveau, outil le plus utilisé.
