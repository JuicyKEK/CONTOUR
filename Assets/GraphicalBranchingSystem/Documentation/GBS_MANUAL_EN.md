# GBS — User Manual (EN)

> Русская версия: `GBS_MANUAL_RU.md`

GBS (Graphical Branching System) is a visual, one-directional story graph.
You build the story with the mouse: stage nodes, on-enter actions, transition conditions and edges
between them. Conditions and actions are ready-made ScriptableObject assets from `Assets/Story`.

---

## 0. Check that the current version is running

Open `UnityDev → GBS → Open Graph`. The toolbar must show **`GBS v2.7`** on the right.
If it does not, Unity has not recompiled the scripts:

1. Focus the Unity window (recompilation starts on focus).
2. `Edit → Preferences → Asset Pipeline → Auto Refresh` must be enabled, or press `Ctrl+R`.
3. Check the Console: on compile errors Unity keeps running the **old** assembly, so all new
   nodes/buttons still look old.
4. Close and reopen the graph window (node UI is created at node creation time; an open window is not redrawn).

---

## 1. System layout

| Layer | Location | Purpose |
|-------|----------|---------|
| Graph model | `GraphicalBranchingSystem/Scripts/Runtime/Data` | `GBSGraphSO` — graph asset: nodes + edges |
| Execution | `Scripts/Runtime/Core` | `GBSGraphRunner` (playback), `GBSBoolEvaluator` (boolean logic), `GBSStarter` (scene component) |
| Saves | `Scripts/Runtime/Save` | `GBSSaveSystem` — JSON progress |
| Editor | `Scripts/Editor` | graph window, nodes, save/load, legacy Story import |
| Content | `Assets/Story` | `StoryAction` / `StoryCondition` / channels / services (see section 6) |

---

## 2. Quick start in 6 steps

1. `UnityDev → GBS → Open Graph`.
2. Right-click on empty space → `Add Node/Start`, then `Add Node/Story` and `Add Node/End`.
3. Connect `Start.Out → Story.In`, then the transition output of the Story node → `End.In`.
4. In the Story node expand `On Enter Actions`, press `Add Action` and assign, for example,
   a `ShowHintAction` asset (`Create → Story/Actions/Show Hint`).
5. In the transition row assign a `StoryCondition`, e.g. `InputButtonCondition` (key F).
6. Type a name into `File Name:` and press `Save`. The asset appears in
   `Assets/GraphicalBranchingSystem/Graphs/`. Add it to the `Graphs` list of the `GBSStarter`
   component in your scene — the story now runs in play mode.

---

## 3. Editor window

| Toolbar item | Purpose |
|--------------|---------|
| `File Name:` | Graph asset name. If it differs from the asset in `Graph:`, `Save` creates a **new** graph |
| `Graph:` | The `GBSGraphSO` asset used for `Load` and overwriting |
| `Save` | Save the canvas into the asset |
| `Load` | Load the asset from `Graph:` into the window |
| `New` | Clear the canvas and start a new graph |
| `Clear Saves` | Delete the gameplay progress file (`gbs_story_save.json`) |
| `GBS v2.7` | Window version |

Canvas actions:

- **Create node:** right-click → `Add Node/...`, or press space (search window).
- **Delete node/edge:** select → `Delete`.
- **Connect:** drag from an output port to an input port. Flow ports only connect to flow ports,
  boolean ports only to boolean ports.
- **Double-click a `GBSGraphSO` asset** in Project to open it in the window.
- **Right-click a Story/End node** for duplicate commands: `GBS/Add Transition`,
  `GBS/Remove Last Transition`, `GBS/Add Action`, `GBS/Remove Last Action`,
  `GBS/Add Event`, `GBS/Remove Last Event`.

---

## 4. Nodes and every field

### 4.1. Start

| Port/field | Type | Description |
|------------|------|-------------|
| `Start If` | input, bool | Start condition. **Empty → the graph starts immediately.** If connected, the graph waits until the expression becomes true |
| `Out` | output, flow | First story node |

Typical use: connect `Graph Completed.Value` → `Start If` so a side story starts only after the main one is finished.

### 4.2. Story node

Fields:

| Element | Description |
|---------|-------------|
| Name (title text) | Readability and logs only |
| `In` (input, flow) | Entry point, accepts any number of edges |
| `On Enter Events` (list of `GBSEvent`) | Instant fire-and-forget events raised on enter. `Add Event` / `X` |
| `On Enter Actions` (list of `StoryAction`) | Actions executed **strictly one by one, each awaited**. `Add Action` / `X` |
| `Add Transition` | Adds a transition condition |
| Transition row | `If` (input, bool) · name · `StoryCondition` field · `X` · **output, flow** |

Runtime order:

1. Enter the node → all `GBSEvent`s are raised.
2. All `StoryAction`s run sequentially (each is awaited).
3. Transition conditions are checked:
   - if one is **already satisfied** at check time, the transition happens immediately;
   - otherwise all conditions wait in parallel, the **first one to complete wins**, the rest are cancelled.
4. The story follows **the edge that leaves the winning condition**.

Condition source per row (by priority):

1. connected boolean port `If` (expression built from `Condition` / `Boolean Operation` / `Graph Completed` nodes);
2. the `StoryCondition` field in the row;
3. nothing assigned → the condition completes instantly (a "default" branch).

> Put the default branch **last** — when several are ready at the same time, the one higher in the list wins.

### 4.3. End node

| Element | Description |
|---------|-------------|
| `In` (input, flow) | Reaching it completes the graph |
| `On Complete Events` (list of `GBSEvent`) | Raised on completion. `Add Event` / `X` |

After End the graph is marked completed — other graphs see this via `Graph Completed`, and it is stored in the save file.

### 4.4. Condition node (condition as a boolean signal)

| Element | Description |
|---------|-------------|
| `StoryCondition` field | Any condition asset from `Assets/Story` |
| `Value` (output, bool) | `true` once the condition has fired |

The value is **latched**: once fired it stays `true` until progress is reset
(`ClearProgress` / `Clear Saves`). That is what makes `AND` / `NOT` and saving possible.
Waiting starts lazily, the first time the expression is needed.

### 4.5. Boolean Operation node

| Element | Description |
|---------|-------------|
| Dropdown | `And`, `Or`, `Not`, `Nand`, `Nor`, `Xor`, `Xnor` |
| `-` / `+` | Remove/add an input (1 to 16) |
| `In 0..N` (inputs, bool) | Operands |
| `Value` (output, bool) | Result |

Rules: unconnected inputs are ignored; with no connected inputs the result is `false`.
`Not` inverts the **first** connected input.

### 4.6. Graph Completed node

| Element | Description |
|---------|-------------|
| `GBSGraphSO` field | Target graph. **Empty means "already completed"** (no waiting) |
| `In` (input, flow) | Lets you place the node in the middle of the story |
| `Out` (output, flow) | Where to go after the target graph is finished |
| `Value` (output, bool) | `true` if the target graph is completed |

Two usages:

- **as a condition:** `Value` → `Start If` or a transition `If`;
- **as an in-flow wait:** the story arrives at `In`, the node waits for the target graph, then continues via `Out`.

---

## 5. `GBSEvent` vs `StoryAction` vs `StoryCondition`

| | `GBSEvent` (`Create → GBS/Events/New Base Event`) | `StoryAction` (`Create → Story/Actions/...`) | `StoryCondition` (`Create → Story/Conditions/...`) |
|---|---|---|---|
| What it is | A plain SO signal "something happened" | An SO action with logic and **awaiting** | An SO condition: "wait until…" |
| Duration | Instant, fire-and-forget | May take time (`await`): fade, timeline, hint | Waits as long as needed |
| Who listens | `UnityGameEventListener` component in the scene (`UnityEvent` in the inspector) | Nobody, the action does the work itself | The graph runner |
| Where in a node | `On Enter Events` (Story), `On Complete Events` (End) | `On Enter Actions` | Transition row / `Condition` node |
| When to use | Poke a scene script right now (open a door, turn on lights) | You need an ordered, timed sequence | You need to wait for the player/an event/a timer |

In short: **events "shout at the scene", actions "do and wait", conditions "wait and move on"**.

---

## 6. ScriptableObject catalogue from `Assets/Story`

### 6.1. Actions (`StoryAction`) — assigned to `On Enter Actions`

| Asset | Create menu | Fields | Behaviour |
|-------|-------------|--------|-----------|
| `SetPlayerControlAction` | `Story/Actions/Set Player Control` | `Is Enabled` | Enables/disables player control (all `IPlayerControlHandle`s) |
| `CameraBlendAction` | `Story/Actions/Blend Camera` | `Target Camera Key`, `Fade Duration` | Fade out → switch camera by key (`CameraDirector`) → fade in |
| `PlayTimelineAction` | `Story/Actions/Play Timeline` | `Timeline` (PlayableAsset), `Wait For Completion` | Plays a cutscene via `CutsceneDirector`; waits for the end if requested |
| `ShowHintAction` | `Story/Actions/Show Hint` | `Text`, `Duration`, `Wait Until Hidden` | Shows a hint through `StoryHintView` |
| `SetBlackboardFlagAction` | `Story/Actions/Set Blackboard Flag` | `Key`, `Value` | Writes a flag into the context blackboard (read by `BlackboardFlagCondition`, stored in saves) |
| `RaiseEventChannelAction` | `Story/Actions/Raise Event Channel` | `Channel` (`StoryEventChannelSO`) | Raises an event channel awaited by `StoryEventCondition` elsewhere |
| `UnityEventAction` | `Story/Actions/Unity Event` | — | Invokes its subscribers. Put a `StoryEventChannelListener` in the scene with the same asset and wire its `UnityEvent` to scene objects |

### 6.2. Conditions (`StoryCondition`) — assigned to a transition row or a `Condition` node

| Asset | Create menu | Fields | Fires when |
|-------|-------------|--------|-----------|
| `AlwaysTrueCondition` | `Story/Conditions/Always True` | — | Instantly |
| `DelayCondition` | `Story/Conditions/Delay` | `Seconds` | N seconds after waiting starts |
| `InputButtonCondition` | `Story/Conditions/Input Button Pressed` | `Button` (`F`, `E`, `R`, `Tab`, `MouseLeftDown`, `MouseLeftUp`, `ESC`) | The player presses the key |
| `StoryEventCondition` | `Story/Conditions/Story Event` | `Event Channel` (`StoryEventChannelSO`) | The channel is raised (by `RaiseEventChannelAction` or gameplay code) |
| `BoolChannelCondition` | `Story/Conditions/Bool Channel State` | `Channel` (`StoryBoolChannelSO`), `Expected Value` | Instantly if the channel already equals `Expected Value`, otherwise on the next match |
| `BlackboardFlagCondition` | `Story/Conditions/Blackboard Flag` | `Key`, `Expected Value` | **Instant check only**: fires if the flag already matches, never otherwise. Use together with other conditions |
| `AllOfCondition` | `Story/Conditions/Composite/All Of` | `Conditions[]` | All nested conditions completed |
| `AnyOfCondition` | `Story/Conditions/Composite/Any Of` | `Conditions[]` | Any nested condition completed |

> `AllOf` / `AnyOf` are "logic inside an asset". In the graph the same is more readable with a
> `Boolean Operation` node (which also offers `Not`, `Xor`, `Nand`, `Nor`).

### 6.3. Channels and scene bridges

| Asset/component | Menu/place | Fields | Purpose |
|-----------------|-----------|--------|---------|
| `StoryEventChannelSO` | `Story/Events/Story Event Channel` | — | One-shot signal: scene ↔ story |
| `StoryBoolChannelSO` | `Story/Events/Bool State Channel` | `Default Value` | Live boolean state (door open, lever up). A subscription immediately receives the current value |
| `StoryEventChannelListener` | scene component | `Channel` (`UnityEventAction`), `On Raised` (`UnityEvent`) | Bridge: `UnityEventAction` from a node → scene object methods |
| `StoryBoolChannelListener` | scene component | `Channel`, `Push Initial Value On Enable`, `Initial Value` | Bridge: scene source → `StoryBoolChannelSO`. Call `SetValue(bool)` / `ChangeValue()` from the source's `UnityEvent` |
| `DoorOpenStateToBoolChannelBridge` | scene component | `Door`, `Field` (`IsOpen`/`IsLocked`), `Channel` | Ready-made bridge from the project's door to a boolean channel |
| `UnityGameEventListener` (GBS) | scene component | `Event` (`GBSEvent`), `Response` (`UnityEvent`) | Bridge: `GBSEvent` from a node → scene object methods |

### 6.4. Services and views (scene components required by actions)

| Component | Fields | Required by |
|-----------|--------|-------------|
| `CameraDirector` | `Cameras[]` — `Key` + `Camera` pairs | `CameraBlendAction` (via `Target Camera Key`) |
| `CutsceneDirector` | `Director` (`PlayableDirector`) | `PlayTimelineAction` |
| `StoryHintView` | `Root` (GameObject), `Text` (`TMP_Text`) | `ShowHintAction` |
| `ScreenFaderView` | `Canvas Group` | `CameraBlendAction` (fade) |
| `IPlayerControlHandle` implementations | — | `SetPlayerControlAction` |

They are all marked `[JDIMonoController]` and are injected through JuicyDI — just put them in the scene.

### 6.5. Legacy (old Story mechanic)

| Asset/component | Status |
|-----------------|--------|
| `StoryNodeSO` (`Story/Story Node`), `StoryBranch` | Replaced by the graph Story node. Kept for import only |
| `StoryManager` | Replaced by `GBSStarter`. Do not keep both in one scene |

Import: select the starting `StoryNodeSO` in Project → `UnityDev → GBS → Import Story From StoryNodeSO`.
You get a laid-out graph with Start and End nodes; action/condition assets are reused as they are.

---

## 7. `GBSStarter` scene component

| Field | Description |
|-------|-------------|
| `Graphs` | List of `GBSGraphSO` assets: the main story + side stories |
| `Load On Start` | Load saved progress on start |
| `Auto Save On Node Enter` | Autosave on every node enter and on graph completion |

All graphs start at once; those with a connected `Start If` wait for their condition.
Public methods (also in the component context menu): `Save Progress`, `Load Progress`, `Clear Progress`.

---

## 8. Saves

- File: `%UserProfile%/AppData/LocalLow/<Company>/<Product>/gbs_story_save.json`
  (exact path: `UnityDev → GBS → Saves → Show Save Path`).
- Stored: current node of every graph, completion flag, latched conditions, boolean blackboard flags.
- Clearing: `UnityDev → GBS → Saves → Clear Saves`, the `Clear Saves` toolbar button, or
  `GBSStarter.ClearProgress()`.

---

## 9. Recipes

**"Did it / refused / ignored" fork**
Three transition rows in a Story node: `StoryEventCondition` (quest done), `InputButtonCondition` (refuse),
`DelayCondition` (timeout). Each row has its own edge to its own node.

**"Door is open AND the player pressed F"**
`Condition` (door `BoolChannelCondition`) and `Condition` (`InputButtonCondition`) → `Boolean Operation` (`And`)
→ its `Value` into the `If` port of the transition row.

**"The player did NOT take the quest"**
`Condition` (`StoryEventCondition` "quest taken") → `Boolean Operation` (`Not`) → transition `If`.
Add a timer condition as well, otherwise `Not` is true immediately.

**Side story after the main one**
In the side graph: a `Graph Completed` node holding the main graph asset → `Value` → its `Start` node `Start If`.
Put both graphs into `Graphs` of `GBSStarter`.

**Pause mid-story until another graph finishes**
Insert a `Graph Completed` node into the flow: `Story output → GraphCompleted.In`, `GraphCompleted.Out → next Story`.

---

## 10. Troubleshooting

| Symptom | Cause / fix |
|---------|-------------|
| No `GBS v2.7` label, nodes look old | Unity did not recompile: `Ctrl+R`, enable Auto Refresh, check Console, reopen the window |
| `X` / `Add ...` buttons do nothing | Use right-click on the node → `GBS/...` (same commands) and verify the version is `v2.7` |
| The story does not start | No `Start` node; `Start.Out` is not connected; the graph is not in `Graphs`; the graph is already marked completed in the save (`Clear Saves`) |
| The story stalls on a node | No condition fired, or the winning condition has no edge — see the Warning in the Console |
| A condition stays true forever | `Condition` nodes latch by design; reset progress (`Clear Saves` / `ClearProgress`) |
| `BlackboardFlagCondition` never fires | It only checks on entry; combine it with a waiting condition through `Boolean Operation` |
