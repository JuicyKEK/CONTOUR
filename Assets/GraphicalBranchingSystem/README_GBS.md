# GBS — Graphical Branching System (v3.0)

Визуальный граф сюжета для Unity: основной сюжет, его этапы, побочные микро-сюжеты и сюжетные
нападения монстров собираются графами в окне `UnityDev → GBS → Open Graph`.

Подробные руководства:
- `Documentation/GBS_MANUAL_RU.md` — полная инструкция на русском (ноды, шаги, ключи, рецепты, переезд)
- `Documentation/GBS_MANUAL_EN.md` — the same manual in English

## Коротко

- **Ноды:** Start, Story, End, Condition, Boolean Operation, Graph Completed.
- **Действия и условия** — встроенные шаги прямо в нодах (`GBSAction` / `GBSCondition`,
  `[SerializeReference]`), без SO-ассета на каждый. Свой шаг — `[Serializable]` класс-наследник,
  сам появляется в меню ноды (путь — атрибут `GBSMenu`).
- **Состояние сюжета** — сигналы и флаги по ключам (`StoryState`), сохраняется вместе с прогрессом.
  Ключи выбираются из каталога `StoryKeyCatalogSO` (поле с `[StoryKey]` — выпадающий список и
  проверка опечаток).
- **Связь со сценой:** мир → сюжет — `StorySignalEmitter`, `StoryTriggerZone`, `DoorStoryFlagBridge`,
  ключи сигналов у NPC, `IStoryState` в коде; сюжет → мир — `StorySceneReactions` (ключ → UnityEvent).
- **Сцена:** `GBSStarter` (графы, сейвы, отдаёт `IStoryState`).

## Структура

```
Scripts/Runtime/Data     — модель графа (GBSGraphSO, ноды, связи)
Scripts/Runtime/Steps    — встроенные действия и условия
Scripts/Runtime/Core     — исполнение (GBSStarter, GBSGraphRunner, GBSBoolEvaluator)
Scripts/Runtime/Save     — сейвы прогресса
Scripts/Editor           — окно графа, ноды, импорт старого Story
Assets/Game/Scripts/Story — состояние сюжета, ключи, компоненты сцены, старые SO-типы
```

## Переезд со старого Story

`UnityDev → GBS → Import Story From StoryNodeSO` переносит цепочку `StoryNodeSO` в граф со
встроенными шагами; SO-каналы становятся ключами `Legacy/<имя ассета>` и продолжают работать со
старой разводкой сцены. Подробно — раздел 12 руководства.
