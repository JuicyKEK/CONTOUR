# GBS — руководство пользователя (RU)

> English version: `GBS_MANUAL_EN.md`

GBS (Graphical Branching System) — визуальный однонаправленный граф сюжета.
Сюжет собирается мышкой в окне графа: ноды-этапы, действия при входе, условия перехода и рёбра
между ними. Условия и действия — это готовые ScriptableObject-ассеты из папки `Assets/Story`.

---

## 0. Проверка, что установлена актуальная версия

Открой `UnityDev → GBS → Open Graph`. В правой части тулбара должна быть надпись **`GBS v2.7`**.
Если её нет — Unity не пересобрала скрипты:

1. Переключись в окно Unity (пересборка запускается при фокусе).
2. `Edit → Preferences → Asset Pipeline → Auto Refresh` должен быть включён, либо жми `Ctrl+R`.
3. Проверь Console: если есть ошибки компиляции, Unity продолжает работать на **старой** сборке,
   и все новые ноды/кнопки будут выглядеть по-старому.
4. Закрой и заново открой окно графа (ноды строятся в момент создания, старое окно не перерисуется).

---

## 1. Из чего состоит система

| Слой | Где лежит | Что делает |
|------|-----------|------------|
| Модель графа | `GraphicalBranchingSystem/Scripts/Runtime/Data` | `GBSGraphSO` — ассет графа: ноды + рёбра |
| Исполнение | `Scripts/Runtime/Core` | `GBSGraphRunner` (проигрывание), `GBSBoolEvaluator` (булева логика), `GBSStarter` (компонент сцены) |
| Сейвы | `Scripts/Runtime/Save` | `GBSSaveSystem` — JSON-прогресс |
| Редактор | `Scripts/Editor` | окно графа, ноды, сохранение/загрузка, импорт старого Story |
| Контент | `Assets/Story` | `StoryAction` / `StoryCondition` / каналы / сервисы (см. раздел 6) |

---

## 2. Быстрый старт за 6 шагов

1. `UnityDev → GBS → Open Graph`.
2. ПКМ по пустому месту → `Add Node/Start`, затем `Add Node/Story` и `Add Node/End`.
3. Соедини: `Start.Out → Story.In`, затем выход условия перехода Story-ноды → `End.In`.
4. В Story-ноде разверни `On Enter Actions`, нажми `Add Action`, положи туда, например,
   `ShowHintAction` (создаётся через `Create → Story/Actions/Show Hint`).
5. В строке условия перехода положи `StoryCondition` — например `InputButtonCondition` (клавиша F).
6. Впиши имя в поле `File Name:` и нажми `Save`. Ассет графа появится в
   `Assets/GraphicalBranchingSystem/Graphs/`. Положи его в список `Graphs` компонента `GBSStarter`
   на сцене — сюжет заработает при запуске игры.

---

## 3. Окно редактора

| Элемент тулбара | Назначение |
|-----------------|------------|
| `File Name:` | Имя ассета графа. Если оно отличается от ассета в поле `Graph:`, `Save` создаст **новый** граф |
| `Graph:` | Ассет `GBSGraphSO`, с которым работаем (для `Load` и перезаписи) |
| `Save` | Сохранить текущее полотно в ассет |
| `Load` | Загрузить ассет из поля `Graph:` в окно |
| `New` | Очистить полотно и начать новый граф |
| `Clear Saves` | Удалить игровой сейв прогресса (`gbs_story_save.json`) |
| `GBS v2.7` | Версия окна |

Действия на полотне:

- **Создать ноду:** ПКМ → `Add Node/...` или пробел (окно поиска).
- **Удалить ноду/ребро:** выделить → `Delete`.
- **Соединить:** тянуть от выходного порта к входному. Flow-порты соединяются только с flow,
  булевы — только с булевыми.
- **Двойной клик по ассету `GBSGraphSO`** в Project открывает граф в окне.
- **ПКМ по Story/End-ноде** содержит дубли кнопок: `GBS/Add Transition`, `GBS/Remove Last Transition`,
  `GBS/Add Action`, `GBS/Remove Last Action`, `GBS/Add Event`, `GBS/Remove Last Event`.

---

## 4. Ноды и все их поля

### 4.1. Start (старт графа)

| Порт/поле | Тип | Описание |
|-----------|-----|----------|
| `Start If` | вход, bool | Условие старта. **Пусто → граф стартует сразу.** Если подключено выражение — граф ждёт, пока оно станет истинным |
| `Out` | выход, flow | Первая нода сюжета |

Типовое использование: подключить `Graph Completed` (порт `Value`) → `Start If`, чтобы микро-сюжет
стартовал только после завершения основного.

### 4.2. Story (сюжетная нода)

Поля:

| Элемент | Описание |
|---------|----------|
| Имя (текст в заголовке) | Только для читаемости и логов |
| `In` (вход, flow) | **Единственный вход ноды.** Принимает сколько угодно рёбер |
| `On Enter Events` (список `GBSEvent`) | Мгновенные события-«выстрелы», поднимаются при входе. `Add Event` / `X` |
| `On Enter Actions` (список `StoryAction`) | Действия, выполняются **строго по очереди с ожиданием** каждого. `Add Action` / `X` |
| `Add Transition` | Добавляет условие перехода |
| Строка условия перехода | имя · `X` · **выход, flow**; под строкой - поле `StoryCondition` |

Порядок работы в рантайме:

1. Вход в ноду → поднимаются все `GBSEvent`.
2. Последовательно выполняются все `StoryAction` (каждый — с `await`).
3. Проверяются условия перехода:
   - если какое-то условие **уже выполнено** на момент проверки — переход происходит немедленно;
   - иначе все условия ждут параллельно, побеждает **выполнившееся первым**, остальные отменяются.
4. Сюжет уходит **по ребру, выходящему из победившего условия**.

Источник условия для строки перехода - только поле `StoryCondition` в этой строке.

* поле заполнено - переход ждёт выполнения этого условия;
* поле пустое - переход срабатывает мгновенно (ветка «по умолчанию»).

Булевых входов у переходов больше нет: вход в Story-ноду ровно один - flow-порт `In`.
Составные условия («И», «ИЛИ») собираются ассетами `AllOfCondition` / `AnyOfCondition`.

> Ветку «по умолчанию» ставь **последней** в списке — при одновременной готовности побеждает та,
> что выше по списку.

### 4.3. End (финал графа)

| Элемент | Описание |
|---------|----------|
| `In` (вход, flow) | Приход сюда завершает граф |
| `On Complete Events` (список `GBSEvent`) | Поднимаются при завершении. `Add Event` / `X` |

После входа в End граф помечается пройденным — это состояние видят ноды `Graph Completed`
других графов и оно попадает в сейв.

### 4.4. Condition (условие как булев сигнал)

| Элемент | Описание |
|---------|----------|
| Поле `StoryCondition` | Любой ассет-условие из `Assets/Story` |
| `Value` (выход, bool) | `true`, как только условие сработало. Подключается к `Start If` или к входам `Boolean Operation` |

Особенность: значение **защёлкивается**. Один раз сработав, оно остаётся `true` до сброса прогресса
(`ClearProgress` / `Clear Saves`). Именно это позволяет строить `AND`, `NOT` и сохранять состояние.
Ожидание условия стартует лениво — в момент, когда выражение впервые понадобилось.

### 4.5. Boolean Operation (булева алгебра)

| Элемент | Описание |
|---------|----------|
| Выпадающий список | `And`, `Or`, `Not`, `Nand`, `Nor`, `Xor`, `Xnor` |
| `-` / `+` | Убрать/добавить вход (от 1 до 16) |
| `In 0..N` (входы, bool) | Операнды |
| `Value` (выход, bool) | Результат |

Правила: неподключённые входы игнорируются; если подключённых входов нет — результат `false`.
`Not` инвертирует **первый** подключённый вход.

### 4.6. Graph Completed (завершён ли другой граф)

| Элемент | Описание |
|---------|----------|
| Поле `GBSGraphSO` | Целевой граф. **Если пусто — считается выполненным** (то есть ожидания нет) |
| `In` (вход, flow) | Позволяет вставить ноду в середину сюжета |
| `Out` (выход, flow) | Куда идти после завершения целевого графа |
| `Value` (выход, bool) | `true`, если целевой граф пройден |

Два способа применения:

- **как условие:** `Value` → `Start If` стартовой ноды;
- **как ожидание в потоке:** сюжет приходит по `In`, нода ждёт завершения указанного графа,
  затем сюжет продолжается по `Out`.

---

## 5. `GBSEvent` vs `StoryAction` vs `StoryCondition`

| | `GBSEvent` (`Create → GBS/Events/New Base Event`) | `StoryAction` (`Create → Story/Actions/...`) | `StoryCondition` (`Create → Story/Conditions/...`) |
|---|---|---|---|
| Что это | Простой SO-сигнал «событие произошло» | SO-действие с логикой и **ожиданием** | SO-условие: «жди, пока…» |
| Время выполнения | Мгновенно, fire-and-forget | Может занимать время (`await`): фейд, таймлайн, подсказка | Ждёт сколько угодно |
| Кто слушает | Компонент `UnityGameEventListener` на сцене (`UnityEvent` в инспекторе) | Никто, действие само что-то делает | Раннер графа |
| Где в ноде | `On Enter Events` (Story), `On Complete Events` (End) | `On Enter Actions` | Поле `Condition` строки перехода / нода `Condition` |
| Когда использовать | Дёрнуть сценный скрипт «здесь и сейчас» (открыть дверь, включить свет) | Нужен сценарий с длительностью и порядком | Нужно дождаться игрока/события/таймера |

Коротко: **события — «крикнуть на сцену», действия — «сделать и дождаться», условия — «дождаться и пойти дальше»**.

---

## 6. Каталог ScriptableObject из `Assets/Story`

### 6.1. Действия (`StoryAction`) — кладутся в `On Enter Actions`

| Ассет | Меню создания | Поля | Что делает |
|-------|---------------|------|------------|
| `SetPlayerControlAction` | `Story/Actions/Set Player Control` | `Is Enabled` | Включает/выключает управление игрока (все `IPlayerControlHandle`) |
| `CameraBlendAction` | `Story/Actions/Blend Camera` | `Target Camera Key`, `Fade Duration` | Затемнение → переключение камеры по ключу (`CameraDirector`) → осветление |
| `PlayTimelineAction` | `Story/Actions/Play Timeline` | `Timeline` (PlayableAsset), `Wait For Completion` | Проигрывает катсцену через `CutsceneDirector`; при `Wait For Completion` ждёт конца |
| `ShowHintAction` | `Story/Actions/Show Hint` | `Text`, `Duration`, `Wait Until Hidden` | Показывает подсказку через `StoryHintView` |
| `SetBlackboardFlagAction` | `Story/Actions/Set Blackboard Flag` | `Key`, `Value` | Пишет флаг в blackboard контекста (читается `BlackboardFlagCondition`, попадает в сейв) |
| `RaiseEventChannelAction` | `Story/Actions/Raise Event Channel` | `Channel` (`StoryEventChannelSO`) | «Стреляет» в канал события — его ждёт `StoryEventCondition` в другой ветке/графе |
| `UnityEventAction` | `Story/Actions/Unity Event` | — | Вызывает подписчиков. На сцене вешается `StoryEventChannelListener` с этим же ассетом, и уже там настраивается `UnityEvent` на сценные объекты |

### 6.2. Условия (`StoryCondition`) — кладутся в строку перехода или в ноду `Condition`

| Ассет | Меню создания | Поля | Когда срабатывает |
|-------|---------------|------|-------------------|
| `AlwaysTrueCondition` | `Story/Conditions/Always True` | — | Мгновенно |
| `DelayCondition` | `Story/Conditions/Delay` | `Seconds` | Через N секунд после начала ожидания |
| `InputButtonCondition` | `Story/Conditions/Input Button Pressed` | `Button` (`F`, `E`, `R`, `Tab`, `MouseLeftDown`, `MouseLeftUp`, `ESC`) | По нажатию клавиши игроком |
| `StoryEventCondition` | `Story/Conditions/Story Event` | `Event Channel` (`StoryEventChannelSO`) | При `Raise()` канала (из `RaiseEventChannelAction` или из геймплейного скрипта) |
| `BoolChannelCondition` | `Story/Conditions/Bool Channel State` | `Channel` (`StoryBoolChannelSO`), `Expected Value` | Мгновенно, если канал уже равен `Expected Value`, иначе — при следующем совпадении |
| `BlackboardFlagCondition` | `Story/Conditions/Blackboard Flag` | `Key`, `Expected Value` | **Только мгновенная проверка**: если флаг уже нужный — срабатывает, иначе не срабатывает никогда. Использовать в связке с другими условиями |
| `AllOfCondition` | `Story/Conditions/Composite/All Of` | `Conditions[]` | Когда выполнились все вложенные |
| `AnyOfCondition` | `Story/Conditions/Composite/Any Of` | `Conditions[]` | Когда выполнилось любое вложенное |

> `AllOf` / `AnyOf` — это «логика внутри ассета». В графе то же самое нагляднее делается нодой
> `Boolean Operation` (и там доступны `Not`, `Xor`, `Nand`, `Nor`).

### 6.3. Каналы и сценные мосты

| Ассет/компонент | Меню/место | Поля | Назначение |
|-----------------|-----------|------|------------|
| `StoryEventChannelSO` | `Story/Events/Story Event Channel` | — | Одноразовый сигнал: сцена → сюжет и обратно |
| `StoryBoolChannelSO` | `Story/Events/Bool State Channel` | `Default Value` | «Живое» bool-состояние (дверь открыта, рубильник поднят). Подписка сразу отдаёт текущее значение |
| `StoryEventChannelListener` | компонент сцены | `Channel` (`UnityEventAction`), `On Raised` (`UnityEvent`) | Мост: действие `UnityEventAction` из ноды → методы сценных объектов |
| `StoryBoolChannelListener` | компонент сцены | `Channel`, `Push Initial Value On Enable`, `Initial Value` | Мост: сценный источник → `StoryBoolChannelSO`. Методы `SetValue(bool)` / `ChangeValue()` можно дёргать из `UnityEvent` источника |
| `DoorOpenStateToBoolChannelBridge` | компонент сцены | `Door`, `Field` (`IsOpen`/`IsLocked`), `Channel` | Готовый мост от двери проекта к bool-каналу |
| `UnityGameEventListener` (GBS) | компонент сцены | `Event` (`GBSEvent`), `Response` (`UnityEvent`) | Мост: `GBSEvent` из ноды → методы сценных объектов |

### 6.4. Сервисы и вью (компоненты сцены, нужны действиям)

| Компонент | Поля | Кому нужен |
|-----------|------|-----------|
| `CameraDirector` | `Cameras[]` — пары `Key` + `Camera` | `CameraBlendAction` (по `Target Camera Key`) |
| `CutsceneDirector` | `Director` (`PlayableDirector`) | `PlayTimelineAction` |
| `StoryHintView` | `Root` (GameObject), `Text` (`TMP_Text`) | `ShowHintAction` |
| `ScreenFaderView` | `Canvas Group` | `CameraBlendAction` (fade) |
| Реализации `IPlayerControlHandle` | — | `SetPlayerControlAction` |

Все они помечены `[JDIMonoController]` и приходят в граф через JuicyDI — просто положи их на сцену.

### 6.5. Легаси (старая механика Story)

| Ассет/компонент | Статус |
|-----------------|--------|
| `StoryNodeSO` (`Story/Story Node`), `StoryBranch` | Заменены Story-нодой графа. Нужны только для импорта |
| `StoryManager` | Заменён `GBSStarter`. Не держи оба на сцене одновременно |

Импорт: выдели стартовую `StoryNodeSO` в Project → `UnityDev → GBS → Import Story From StoryNodeSO`.
Получишь граф с расставленными нодами, Start и End; ассеты действий и условий переиспользуются.

---

## 7. Компонент сцены `GBSStarter`

| Поле | Описание |
|------|----------|
| `Graphs` | Список ассетов `GBSGraphSO`: большой сюжет + микро-сюжеты |
| `Load On Start` | Загружать сохранённый прогресс при старте |
| `Auto Save On Node Enter` | Автосейв при входе в каждую ноду и при завершении графа |

Все графы запускаются одновременно; те, у кого `Start If` подключён, ждут своё условие.
Публичные методы (и контекстное меню компонента): `Save Progress`, `Load Progress`, `Clear Progress`.

---

## 8. Сейвы

- Файл: `%UserProfile%/AppData/LocalLow/<Company>/<Product>/gbs_story_save.json`
  (точный путь: `UnityDev → GBS → Saves → Show Save Path`).
- Что сохраняется: текущая нода каждого графа, флаг завершённости, защёлкнутые условия,
  булевы флаги blackboard.
- Очистка: `UnityDev → GBS → Saves → Clear Saves`, кнопка `Clear Saves` в окне графа или
  `GBSStarter.ClearProgress()`.

---

## 9. Рецепты

**Развилка «выполнил / отказался / проигнорировал»**
Три условия перехода в Story-ноде: `StoryEventCondition` (квест сдан), `InputButtonCondition` (отказ),
`DelayCondition` (таймаут). Из каждой строки — своё ребро в свою ноду.

**«Дверь открыта И игрок нажал F»**
Ноды `Condition` (`BoolChannelCondition` двери) и `Condition` (`InputButtonCondition`) → нода
`Boolean Operation` (`And`) → её `Value` в `Start If` (гейт графа).
В строке перехода тот же смысл даёт ассет `AllOfCondition` с двумя вложенными условиями.

**«Игрок НЕ взял задание»**
`Condition` (`StoryEventCondition` «взял задание») → `Boolean Operation` (`Not`) → `Start If` микро-графа.
Не забудь второе условие-таймер, иначе `Not` истинен сразу.

**Микро-сюжет после основного**
В микро-графе: нода `Graph Completed` с ассетом основного графа → `Value` → `Start If` его `Start`-ноды.
Оба графа положи в `Graphs` у `GBSStarter`.

**Пауза посреди сюжета до конца другого графа**
Вставь ноду `Graph Completed` прямо в поток: `Story.выход → GraphCompleted.In`, `GraphCompleted.Out → следующая Story`.

---

## 10. Траблшутинг

| Симптом | Причина / решение |
|---------|-------------------|
| Нет надписи `GBS v2.7`, ноды выглядят по-старому | Unity не пересобрала скрипты: `Ctrl+R`, включить Auto Refresh, проверить Console, переоткрыть окно |
| Кнопки `X` / `Add ...` не реагируют | Используй ПКМ по ноде → `GBS/...` (те же действия) и убедись, что версия `v2.7` |
| Сюжет не стартует | Нет `Start`-ноды; `Start.Out` ни с чем не соединён; граф не добавлен в `Graphs`; граф уже помечен пройденным в сейве (`Clear Saves`) |
| Сюжет встал на ноде | Ни одно условие не выполнилось, либо у победившего условия нет ребра — смотри Warning в Console |
| Условие «залипло» истинным | Ноды `Condition` защёлкиваются по дизайну; сбрось прогресс (`Clear Saves` / `ClearProgress`) |
| `BlackboardFlagCondition` никогда не срабатывает | Он проверяет флаг только в момент входа; комбинируй его через `Boolean Operation` с ожидающим условием |

## 11. Что нового в v2.7
- Строка условия перехода перестроена в две линии, чтобы ничего не выходило за границы ноды:
  - верхняя: имя перехода - `X` (удалить) - **выходной порт** (ребро на следующую ноду);
  - нижняя: подпись `Condition` и поле `StoryCondition` (кружок выбора ассета теперь всегда внутри ноды).
- Все поля ассетов стали сжимаемыми (flex-shrink), кнопки `X` имеют фиксированную ширину 22 px
  и больше не могут быть обрезаны рамкой ноды.
- В каждом списке (`On Enter Events`, `On Enter Actions`, `On Complete Events`) у строки есть `X`,
  а рядом с кнопкой `Add ...` появилась кнопка `X` - "удалить последний элемент".
- End-нода: список `On Complete Events` с добавлением и удалением элементов.
- Story-нода: кнопка `Add Transition` и рядом `X` - удалить последний переход.
- Минимальная ширина нод увеличена (260 px, секция переходов - 300 px).

## 12. Что нового в v2.7
- У переходов убран булев вход `If`. **Вход в Story-ноду теперь ровно один** - flow-порт `In`.
- Строка перехода: имя · `X` · выходной flow-порт, под строкой - поле `StoryCondition`.
- Пустое поле `StoryCondition` = мгновенный переход. Составные условия собираются ассетами `AllOfCondition` / `AnyOfCondition`.
- Булевы выходы нод `Condition` / `Boolean Operation` / `Graph Completed` теперь применяются для `Start If` и как операнды других логических нод.
- Старые графы грузятся без ошибок: рёбра, ведшие в удалённые порты `If`, пропускаются при загрузке.
