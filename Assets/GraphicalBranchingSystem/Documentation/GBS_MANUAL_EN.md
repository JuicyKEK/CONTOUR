# GBS — User Manual (EN)

> Русская версия: `GBS_MANUAL_RU.md`

GBS (Graphical Branching System) is a visual story graph. A story is assembled with the mouse in the
graph window: stage nodes, on-enter actions, transition conditions and edges between them. Use as many
graphs as you like: the main story, its chapters/stages, side micro-stories, scripted monster attacks.

What matters in v3.0:

- **actions and conditions live inside the nodes** — no separate SO asset per hint/delay;
- **the world talks to the story through story state keys** (signals and flags) instead of SO channels;
  keys come from one catalog, events are never lost and are saved;
- **scene reactions** — one component with a "key → UnityEvent" list instead of an "SO + listener" pair
  per story step.

---

## 0. Version check

`UnityDev → GBS → Open Graph` — the toolbar must show **`GBS v3.2`**. If not, Unity has not recompiled:
focus the Unity window (or `Ctrl+R`), check the Console for compile errors, reopen the graph window.

---

## 1. System layout

| Layer | Location | Purpose |
|-------|----------|---------|
| Graph model | `GraphicalBranchingSystem/Scripts/Runtime/Data` | `GBSGraphSO` — graph asset: nodes + edges |
| Graph steps | `Scripts/Runtime/Steps` | Inline actions (`GBSAction`) and conditions (`GBSCondition`) |
| Execution | `Scripts/Runtime/Core` | `GBSStarter` (scene component), `GBSGraphRunner`, `GBSBoolEvaluator` |
| Saves | `Scripts/Runtime/Save` | JSON graph progress + story state |
| Editor | `Scripts/Editor` | Graph window, nodes, legacy Story importer |
| Story state | `Assets/Game/Scripts/Story/Runtime/Core` | `StoryState` / `IStoryState` — signals and flags by key |
| Keys | `Assets/Game/Scripts/Story/Runtime/Keys` | `StoryKeyCatalogSO` (catalog), `[StoryKey]` attribute (dropdown) |
| Scene link | `Assets/Game/Scripts/Story/Runtime/Scene` | `StorySceneReactions`, `StorySignalEmitter`, `StoryTriggerZone`, `DoorStoryFlagBridge` |

---

## 2. Quick start

1. Create a key catalog: `Create → Story/Story Key Catalog` (or it is created automatically the first
   time you press `+` next to a key field — `Assets/Game/SO/Story/StoryKeys.asset`).
2. `UnityDev → GBS → Open Graph`. Right-click → `Add Node/Start`, `Add Node/Story`, `Add Node/End`.
3. Connect `Start.Out → Story.In`, the Story node transition output → `End.In`.
4. Story node: `On Enter Actions → Add Action → UI/Show Hint`, type the text right in the node.
5. In the transition row click `None` → `Input/Button Pressed` → pick `F`.
6. `File Name:` → `Save`. The graph appears in `Assets/GraphicalBranchingSystem/Graphs/`.
7. In the scene: an object with `GBSStarter`, the graph in its `Graphs` list. Done.

---

## 3. Editor window

| Toolbar item | Purpose |
|--------------|---------|
| `File Name:` | Graph asset name. If it differs from the asset in `Graph:`, `Save` creates a **new** graph |
| `Graph:` | The `GBSGraphSO` asset being edited |
| `Save` / `Load` / `New` | Save the canvas to the asset / load the asset / clear the canvas |
| `Clear Saves` | Delete the game progress save |

- **Create a node:** right-click → `Add Node/...` or Space.
- **Delete:** select → `Delete`. **Connect:** drag from an output to an input (flow to flow, bool to bool).
- **Copy / paste:** `Ctrl+C` / `Ctrl+X` / `Ctrl+V` / `Ctrl+D` (duplicate) or right-click → `Copy` / `Cut` / `Paste` / `Duplicate`.
  Nodes are copied with all their actions and conditions, plus the edges between the selected nodes. `Paste` puts the
  nodes under the cursor, `Duplicate` next to the originals. Pasting into another graph works too. A second `Start` node is not pasted.
- **Double-click a `GBSGraphSO`** to open it.
- Changes reach the asset **only on `Save`**.

---

## 4. Nodes

### 4.1. Start
`Start If` (bool input): empty — the graph starts immediately; connected — waits for the expression.
`Out` (flow) — the first story node.

### 4.2. Story
- `In` — the only input (any number of edges).
- **`On Enter Actions`** — action list. `Add Action` opens a type menu; each action has a header
  (click to change type), `↑ ↓` to reorder, `X` to delete. Fields are edited right in the node.
- **Transitions**: `Add Transition`; a row has a name, `X`, an output port, and below it the `If`
  condition (`None` — transition fires immediately).

Runtime:
1. Entering the node runs the actions **one by one, awaiting each**.
2. Then the node waits for all transition conditions in parallel; the first one satisfied wins.
3. **If several are ready at once, the topmost transition wins** — this is how branching works:
   `Story State/Flag Is ...` on top, an unconditional transition ("else") at the bottom.
4. An exception in one action is logged and does not stop the story.

### 4.3. End
`On Complete Actions` run, then the graph is marked completed (visible to `Graph Completed` nodes of
other graphs, stored in the save).

### 4.4. Condition
Inline condition → bool `Value` output. The value **latches**: once satisfied it stays `true` until the
progress is reset. Waiting starts lazily, when the value is first requested.
An empty condition never fires (for "always true" use `Flow/Always True`).

### 4.5. Boolean Operation
`And`, `Or`, `Not`, `Nand`, `Nor`, `Xor`, `Xnor`; `+`/`-` change the input count. Unconnected inputs are
ignored; no connected inputs → `false`; `Not` inverts the first connected input.

### 4.6. Graph Completed
`GBSGraphSO` field (empty = satisfied). Two ways to use it:

- **`Value` (bool output)** — "the given graph is completed". Connect it to the Start node's `Start If` (this graph
  starts after another one) or to `Logic` node inputs (`AND`/`OR`...). Not needed — leave it unconnected.
- **`In`/`Out` (flow)** — wait for another graph right inside the story flow: the story enters the node, waits,
  then continues through `Out`.

---

## 5. Built-in steps

### 5.1. Actions (`On Enter Actions`, `On Complete Actions`)

| Menu | Fields | Effect |
|------|--------|--------|
| `UI/Show Hint` | Text, Until Node Exit, Duration, Wait Until Hidden | Bottom-screen hint (`IStoryHintView`). `Until Node Exit` — keep it until the node's transition fires; otherwise `Duration` seconds. `Wait Until Hidden` — do not run the next actions until the hint hides (does not keep the hint) |
| `Player/Set Player Control` | Is Enabled | Enable/disable player control: movement and look (`FirstPersonController`) and interaction (`PlayerInteractiveController`) — all `IPlayerControlHandle`. Input buttons (`Input/Button Pressed`, Tab) keep working |
| `Camera/Blend Camera` | Camera Key, Fade Duration | Fade out → camera by key (`CameraDirector`) → fade in |
| `Camera/Play Timeline` | Timeline, Wait For Completion | Cutscene via `CutsceneDirector` |
| `Flow/Wait Seconds` | Seconds | Pause between actions |
| `Story State/Set Flag` | Key, Value | Story state flag (saved) |
| `Story State/Raise Signal` | Key | Signal (e.g. wake a branch of another graph) |
| `Scene/Invoke Scene Reaction` | Key | Invoke scene reactions with the key (`StorySceneReactions`) |
| `Save/Save Game` | — | Checkpoint: save the whole game via `GameSaveController` (see section 10) |
| `Audio Tapes/Mark Tape Completed` | Tape | Mark an audio tape task as completed |
| `Legacy/...` | asset | Old `StoryAction` / `GBSEvent` SO (custom SOs without a built-in equivalent) |

### 5.2. Conditions (transitions, Condition node)

| Menu | Fields | Satisfied when |
|------|--------|----------------|
| `Flow/Always True` | — | Immediately |
| `Time/After Delay` | Seconds | N seconds after waiting starts |
| `Input/Button Pressed` | Button | The player pressed the button |
| `Story State/Signal Raised` | Key, Only After Node Enter | A signal arrived (by default after entering the node; otherwise at any time) |
| `Story State/Flag Is` | Key, Expected | The flag has the expected value (immediately if it already has) |
| `Story State/Value Compare` | Key, Comparison, Value | The key value (counter/flag) satisfies the comparison |
| `Logic/All Of`, `Logic/Any Of` | Conditions | All / any of the nested conditions |
| `Legacy/Story Condition Asset` | asset | Old `StoryCondition` SO |

---

## 6. Story state and keys

`StoryState` stores integer values by string key:

- **signal** (`Signal`) — an event counter: "player entered the room", "NPC caught the player";
- **flag** (`Flag`) — yes/no: "door is open", "player took the task".

Which one: a **signal** is an event the story is waiting for *now* ("player pressed the radio"): every raise is a
new one, `Signal Raised` by default reacts only to signals after the node was entered, and `Value Compare` can
count them ("found ≥ 3"). A **flag** is a fact needed *later* or for branching ("took the key"): `Flag Is` is
satisfied at once if the flag is already set, and writing the same value again changes nothing.

Why this beats SO channels:

- **events are not lost** — if the player opened the door before the story started waiting, the value is
  already in the state; `Signal Raised` without `Only After Node Enter`, or `Flag Is`, fire immediately;
- **saved** together with graph progress — after loading, the story knows what already happened;
- **non-linearity**: a player choice is stored as a flag and read by any graph (`Flag Is`, `Value Compare`).

**Keys** are strings picked from `StoryKeyCatalogSO` catalogs (one per project or one per chapter). Every key
field has `▾` — pick a key: **first the catalog, then a key from it**; and `+` — add the typed key (visible only when
the key is in no catalog — the typo guard; with several catalogs it asks which one). The field tooltip shows the
key's catalog. Inside a catalog, group keys with `/`: `Door_1_3/Entered` becomes a submenu. Keys are **shared**
by the whole story: the same key in two catalogs is one and the same flag/signal (the tooltip warns), so give keys
unique names, e.g. with a chapter prefix.

---

## 7. Scene link

### World → story
| Way | How |
|-----|-----|
| `StorySignalEmitter` | Component with a key; call `Raise()` / `SetFlagTrue()` / `SetFlagFalse()` from any UnityEvent |
| `StorySignalInteract` | On an object with `IInteraction` (door, tape, item, NPC): the player presses E → a signal or a flag. No UnityEvent — `PlayerInteractiveController` notifies the object. Fires on every attempt (a locked door too); for results use bridges like `DoorStoryFlagBridge` |
| `StoryTriggerZone` | Trigger zone: player entered → signal and/or flag. Sets `Is Trigger` and `Ignore Raycast` when added |
| `DoorStoryFlagBridge` | Keeps a flag equal to a door state (open / locked) |
| NPC | Signal key fields on `NpcController` (player caught, chase started), `NpcDocumentCheckBehaviour`, `NpcPatrolRoute` points (`ArrivalSignal`) |
| Code | `[Inject] IStoryState` (`RaiseSignal`, `SetFlag`) or `StorySignals.Raise(key)` where a story may be absent |

### Story → world
`StorySceneReactions` — a "key → UnityEvent" list. One component can hold all reactions of a chapter or
location: enable objects, take control away, command NPCs/monsters (`ForceChase`, `SetAggressiveOn`,
`SetPatrolRoute`, `ForceIdle`...), play an animation. In the graph use `Scene/Invoke Scene Reaction`
with the same key. Several reactions may share a key (different components/scenes) — all are invoked.

`IStoryState` is provided by `GBSStarter` — without it in the scene `StorySignalEmitter` /
`StoryTriggerZone` report a DI error.

---

## 8. Custom actions and conditions

A new step is a `[Serializable]` subclass. It appears in the node menu automatically; `GBSMenu` sets the
menu path. Scene services come from `context.Resolve<T>()` (any JuicyDI bean) — no `StoryContext` edits.

```csharp
[Serializable, GBSMenu("Monsters/Start Attack")]
public class StartMonsterAttack : GBSAction
{
    [SerializeField] private string m_GroupId;

    public override UniTask ExecuteAsync(StoryContext context, CancellationToken token)
    {
        context.Resolve<IMonsterDirector>()?.StartAttack(m_GroupId);
        return UniTask.CompletedTask;
    }
}
```

A condition subclasses `GBSCondition` with `WaitAsync(GBSConditionContext, token)`; for story state
conditions use the `WaitStateAsync(state, predicate, token)` helper.
Do **not** rename step classes or move them to another namespace without `[MovedFrom]` — the graph stores
the type name.

---

## 9. `GBSStarter` component

| Field | Description |
|-------|-------------|
| `Graphs` | Graph assets: the big story + micro-stories. All start together; those with `Start If` wait |
| `Load On Start` | Load progress on start |
| `Context Provider` | Usually empty. Your own `GBSStoryContextProvider` subclass to fully replace the context (tests, stubs) |
| `Context Builder` | Default context build. Services (hint, cameras, input…) are taken from JuicyDI at the moment of use; the checkboxes are a startup check: Console error if a checked service is missing |

Methods (also in the context menu): `Save Progress`, `Load Progress`, `Clear Progress`.
Do not keep `GBSStarter` and the legacy `StoryManager` in one scene.

---

## 10. Saves

Saving is **not automatic** — only on demand, at checkpoints. The shared `GameSaveController` (one per
scene) does it: `SaveGame()` goes through every mechanic taking part in saves (`ISaveParticipant`): the GBS
story, audio tapes, infection zones. A new mechanic joins the pool automatically — its JuicyDI bean just
implements `ISaveParticipant`.

How to save:
- **from the story** — the `Save/Save Game` action in a checkpoint node;
- **from code** — `[Inject] IGameSaveService` → `SaveGame()`;
- **manually** — the `GameSaveController` context menu → `Save Game`.

Loading happens on scene start, each mechanic on its own (`GBSStarter` — `Load On Start`).
Returning to a checkpoint = reloading the scene.

- Files: `Application.persistentDataPath/Saves/` (`gbs_story_save.json` — the story).
- The story saves: the current node of each graph, completion, latched conditions, **story state**.
- Graph progress is stored at the node where the save ran: after loading, the story resumes there and runs
  **its** actions again. A dedicated checkpoint node with `Save Game` as its first action works well.
- Delete all saves: `UnityDev → Saves → Delete All Saves`; open the folder: `UnityDev → Saves → Open Saves Folder`.
  Story only: `UnityDev → GBS → Saves → Clear Saves`.

---

## 11. Recipes

**Branch on a player choice.** In the choice node — transitions `Input/Button Pressed` (agreed → `Set Flag
TookTask = true`) and `After Delay` (ignored). Later, in any graph: two transitions — `Flag Is TookTask =
true` on top, an unconditional one ("else") below.

**"Door is open AND the player pressed F".** A transition with `Logic/All Of` of `Flag Is Door_Open` and
`Button Pressed F`; or `Condition` nodes → `Boolean Operation (And)` → the graph's `Start If`.

**Scripted monster attack.** A separate "Attack" graph: `Start If` ← `Graph Completed` (chapter) /
`Condition (Signal Raised Alpha/Alarm)`. Nodes: `Invoke Scene Reaction Attack_Start` (in the scene —
`SetAggressiveOn`, `ForceChase` on monsters) → transitions `Signal Raised Npc_PlayerCaught` (defeat) /
`Flag Is Generator_On` (survived) → `Invoke Scene Reaction Attack_End` (`ForceIdle`).
The monster's `NpcController` has `Player Caught Signal = Npc_PlayerCaught`.

**Micro-story after the main one.** In the micro-graph: `Graph Completed` (main graph) → `Start If`.

**Pause until another graph ends.** `Graph Completed` inside the flow: `Story → GraphCompleted.In`,
`GraphCompleted.Out → next Story`.

---

## 12. Moving from the legacy Story (StoryNodeSO + SO channels)

1. Select the starting `StoryNodeSO` → `UnityDev → GBS → Import Story From StoryNodeSO`.
   The resulting graph already uses built-in steps for hints/delays/camera/buttons, and SO channels
   became `Legacy/<asset name>` keys (added to the catalog right away).
2. In the scene replace `StoryManager` with `GBSStarter` and put the graph into `Graphs`.
3. **No scene rework is needed** — bridges keep it working:
   - `StoryEventChannelSO.Raise()` also raises the `Legacy/<name>` signal;
   - `StoryBoolChannelSO.SetValue()` writes the `Legacy/<name>` flag;
   - `StoryEventChannelListener` answers `Invoke Scene Reaction Legacy/<UnityEventAction name>`.
4. Then one step at a time: move the listener's UnityEvent into `StorySceneReactions` under a proper key,
   replace the channel `Raise` with a `StorySignalEmitter`, change the key in the graph, delete the SO assets.

Old graphs (v2.x) open as is: when loaded into the window, SO actions/conditions are converted into
built-in steps; after `Save` the old fields are cleared. A graph that was never re-saved still runs in game.

---

## 13. Troubleshooting

| Symptom | Cause / fix |
|---------|-------------|
| No `GBS v3.2` | Unity has not recompiled: `Ctrl+R`, Console, reopen the window |
| `Save Game`: "no GameSaveController" | Add `GameSaveController` to the scene |
| My step is missing from `Add Action` | The class must be `[Serializable]`, non-abstract, with a parameterless constructor |
| The story does not start | No Start node / `Start.Out` not connected / graph not in `Graphs` / graph already completed in the save (`Clear Saves`) |
| The story is stuck on a node | No condition was satisfied, or the winning transition has no edge — see the Warning in the Console |
| `Invoke Scene Reaction`: "no reactions with key" | No `StorySceneReactions` with the key in loaded scenes, or a typo |
| DI error on `StorySignalEmitter` / `StoryTriggerZone` | No `GBSStarter` in the scene |
| A key field shows `+` | The key is not in the catalog — a typo or a new key (press `+`) |
| A condition "stuck" true | `Condition` nodes latch by design; `Clear Saves` |
| `Show Hint`: "no IStoryHintView" / JuicyDI `Can not resolve` errors | The `StoryHintView` prefab must be under the scene Canvas. If the root holding `JDI` goes to `DontDestroyOnLoad` (`DontDestroer`), the current `MainJDIController` is required — it also registers objects of the original scene |
| The hint is invisible, Console says "Root or Text not assigned" | `StoryHintView` fields are empty — use the `Prefabs/Story/StoryHintView` prefab |
| `Blend Camera`: "no camera with key" | Add a camera with this key to `CameraDirector` (and the player camera, to switch back to it) |

---

## 14. What's new in v3.2

- Node `Copy` / `Cut` / `Paste` / `Duplicate` in the graph window (hotkeys and the right-click menu).
- `Show Hint`: the `Until Node Exit` checkbox keeps the hint until the node's transition fires.
- Key picking: first a `StoryKeyCatalogSO` catalog, then a key; `+` asks for the catalog; the field tooltip shows the key's catalog.
- `StorySignalInteract`: a story signal/flag when the player interacts with any `IInteraction`, no UnityEvent.
- The story context no longer captures services at startup: hint, cameras, input are taken from JuicyDI at the
  moment of use — found even if their scene was loaded after `GBSStarter`.
- `Set Player Control` also disables player movement/look (`FirstPersonController` is now an `IPlayerControlHandle`).
- `Show Hint`: a new hint is no longer hidden by the previous hint's timer.
- `Blend Camera` / `CameraDirector`: an unknown camera key no longer disables all cameras (black screen) — Warning.

## 15. What's new in v3.1

- Fixed: fields of built-in actions/conditions in nodes could not be edited.
- Key presses in node text fields no longer trigger graph hotkeys (Space etc.).
- Auto-save on node enter removed: saving only on demand via the shared `GameSaveController`;
  in the graph — the `Save/Save Game` action.
- `UnityDev → Saves` menu: delete all saves, open the saves folder.

## 16. What's new in v3.0

- Actions and conditions are built-in steps inside nodes (`[SerializeReference]`), type menu, `↑ ↓ X`.
- Story state (signals/flags by key) instead of SO channels; stored in the save.
- Key catalog + `[StoryKey]` dropdown with typo checks.
- `StorySceneReactions`, `StorySignalEmitter`, `StoryTriggerZone`, `DoorStoryFlagBridge`; signal keys on NPCs.
- `context.Resolve<T>()` — any service in custom steps without editing `StoryContext`.
- End node: `On Complete Actions` instead of `GBSEvent`.
- When several transitions are ready at once the topmost wins (previously an unconditional transition
  always won, even if an upper one was already satisfied).
- An action exception no longer stops the graph. Saves are written atomically into `Saves/`.
- The importer converts the legacy story into built-in steps; `Legacy/<name>` bridges for the old scene.
- Fixed: `UnityGameEventListener` did not unsubscribe in `OnDisable`.
