# Combat Quickstart Lite

Open `CombatQuickstartLite.unity` and press Play. Two soldiers share a behavior asset: patrol, chase the player, fire a burst, retreat when hurt, and heal at their camp. Every AI action and consideration is built into Wise Feline Lite. This scene also works in Ultimate.

Move with WASD or the arrow keys, aim with the mouse, and hold the left mouse button to fire. Cyan player shots deal 8 damage every 0.25 seconds. Amber soldier shots deal 10 damage. Projectiles ignore their own team. The player respawns when its health runs out; soldiers retreat and recover.

Open **Window > NoOpArmy > Wise Feline** and select SoldierAlpha or SoldierBravo to watch the scores. Both use `AI/CombatQuickstart.asset`, but their health and patrol routes are independent. Their `BlackboardInitialValues` components provide the scene references for `Player`, `Camp` and `PatrolPoints`, and start `Health` at 100.

| Action | Built-in class | Reason to choose it |
| --- | --- | --- |
| Patrol | `MoveAndPatrol` | Baseline score 0.25 |
| Chase | `MoveToGameObjectAction` | The known player is nearby and the soldier has enough health |
| Attack | `CreateGameObjectAction` | The player is within roughly 3 m; drains Health at 6 per second |
| Retreat | `MoveToGameObjectAction` | Low Health makes returning to Camp more useful than fighting |
| Rest | `NoopAction` | Near Camp with Health below full; restores 10 per second |

Chase reads the `Player` GameObject key directly. Its distance consideration uses range 0 to 15 m, inverted distance and a linear rising curve. Near the edge of that range, Patrol's baseline can still win; the crossover depends on health and score compensation. There is no line-of-sight check and no AI Tags component. Walls still block navigation and projectiles, but they do not hide the player from the Lite soldier. The Ultimate scene in `Demos/CombatQuickstart` adds tag discovery and line of sight.

The copied arena, art, animations, prefabs, NavMesh and behavior are in this folder. The three non-AI scripts for player input, projectile collision and health are shared with the Ultimate scene in `Demos/CombatQuickstartShared/Scripts`. The Lite scene does not depend on the Ultimate-only demo folder.

The manual's [Combat Quickstart](https://www.nooparmygames.com/WF-UtilityAI-Unity/articles/combat-quickstart.html) explains the loop and edition differences. Follow the [exercises](https://www.nooparmygames.com/WF-UtilityAI-Unity/articles/combat-quickstart-exercises.html) to add guard orders, a second patrol route and equipment repair without code. Copy the scene, behavior and blackboard definition into your own folder before experimenting, and assign the copied assets to your soldiers.
