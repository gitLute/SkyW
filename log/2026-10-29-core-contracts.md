# Контракты ядра и типизированный шов ECS

Дата: 2026-10-29
Ветка: core-folders

## Задача

Написать контракты слоя 1 и шов ECS по `docs/pending_architecture.md`
разделы 3-8, не протащив Friflo в `SkyW.Abstractions`.

## Что сделано

- `src/SkyW.Ecs` — типизированный шов: `IComponent`, `ITag` (наследуют
  маркеры Friflo), `EntityRef`, `ComponentId`, `IEntityStore`, `IWorld`,
  `IQuery<T1..T3>`, `EntityAction`, `ICommandQueue`.
- `src/SkyW.Abstractions/Scheduling` — `SimulationStage`, `IStage`,
  `ISimulationClock`, `ThreadingMode`.
- `src/SkyW.Abstractions/Replication` — `VisualId`, `StateBatchKind`,
  `StateBatchHeader`, `IStateStream`.
- `src/SkyW.Abstractions/Rendering/IOutput.cs`, `Scenes/PrefabId`,
  `IPrefab`, `IPrefabManager`.
- `src/SkyW.Engine` — `IEngine`, `EngineOptions`, `FrameResult`.
- `src/SkyW.Scenes` — `Scripts/ISceneScript`, `Building/SceneContext`.
- Ссылки на `SkyW.Ecs` добавлены в `Engine`, `Simulation`, `Scenes`,
  `Backends.Friflo`, `Tests.Unit`; проект внесён в `SkyW.slnx`.
- `tests/SkyW.Tests.Architecture` — правила для `SkyW.Ecs` и пакета Friflo:
  он разрешён только `SkyW.Ecs` и `SkyW.Backends.Friflo`.
- `tests/SkyW.Tests.Unit/Ecs` — 6 тестов маркера, `EntityRef`,
  `ComponentId`, `VisualId`, заголовка батча; `Engine` — 2 теста
  значений по умолчанию.
- `AGENTS.md`, `README.md`, `docs/answers.md`,
  `docs/pending_architecture.md` — исключение для `SkyW.Ecs` и статус этапа.

## Проверка

- `dotnet build SkyW.slnx -c Release` — 0 ошибок, 0 предупреждений.
- `dotnet test --solution SkyW.slnx` — итог 79, сбой 0, успешно 79.
  Код 8 даёт пустой `SkyW.Tests.Stack`.
- Проба вне репозитория (`/tmp/opencode/seam-probe`): структура
  `Health : SkyW.Ecs.IComponent` добавлена через Friflo `AddComponent`,
  найдена `Query<Health>`, запись через `ref` в `ForEachEntity` видна
  (`value=15`); маркер наследует `Friflo.Engine.ECS.IComponent`.

## Открытые вопросы

- Если файл импортирует и `Friflo.Engine.ECS`, и `SkyW.Ecs`, имя
  `IComponent` неоднозначно. Бэкенду нужен псевдоним; прикладной код
  импортирует только `SkyW.Ecs`.
- Ёмкости арен заданы одним `BatchBytes`; число арен и правило вытеснения
  определяются на этапе 4 вместе с `StateStream`.
- `IEntityStore.Create` пока публичен: он нужен командам и бэкенду,
  фасад `IWorld` его не раскрывает. Права стоит перепроверить на этапе 2.

## Дальше

Этап 2: `FrifloEntityStore`, запросы, dirty-биты; замер на 10 000
сущностях и 1000 шагах с нулевыми аллокациями.
