# Папки ядра и архитектурные тесты

Дата: 2026-10-29
Ветка: core-folders

## Задача

Привести структуру проектов и папок к плану ядра: выделить композицию
в `SkyW.Engine`, разнести контракты и заменить ручную проверку слоёв тестом.

## Что сделано

- `src/SkyW.Engine` — новый проект композиции: `Composition/`, `Lifecycle/`,
  `Channels/`, `Output/`; добавлен в `SkyW.slnx`.
- `src/SkyW.Host` — удалены `Bootstrap/` и `Loop/`, остался `Program.cs`;
  ссылки только на `Engine` и `Scenes`.
- `src/SkyW.Sandbox` — переведён с `Host` на `Engine`, добавлены `Scenes`
  и `Content`.
- `src/SkyW.Simulation` — убрана ссылка на `Content`; `Snapshots/` заменён
  на `Replication/`, `Modes/` — на `Execution/`; добавлены `World/`,
  `Commands/`, `Input/`.
- `src/SkyW.Abstractions` — добавлен `Replication/`; обновлён список
  контрактов `Ecs/`, `Replication/`, `Scheduling/`, `Scenes/`, `Rendering/`.
- `src/SkyW.Backends.Friflo` — добавлены `Store/`, `Queries/`, `Commands/`,
  `Registration/`; правило S1 уточнено.
- `AGENTS.md`, `README.md`, `docs/pending_architecture.md`,
  `docs/engine-architecture.md` — слой композиции, репликация вместо снимков,
  новые каталоги.
- `tests/SkyW.Tests.Architecture` — `ProjectGraph.cs` и `LayerRulesTests.cs`:
  10 тестов проверяют граф ссылок вместо ручной проверки.

## Проверка

- `dotnet build SkyW.slnx -c Release` — 0 ошибок, 0 предупреждений.
- `dotnet test --solution SkyW.slnx` — итог 69, сбой 0, успешно 69.
  Код 8 даёт пустой `SkyW.Tests.Stack`, как и ожидалось.
- Архитектурные тесты: 10 из 10.

## Открытые вопросы

- Friflo 3.6.0 требует, чтобы тип компонента реализовал
  `Friflo.Engine.ECS.IComponent`. Это маркер из внешней библиотеки, и он
  конфликтует с правилом «внешние библиотеки только в `Backends`».
  Решение нужно до реализации шва ECS (этап 2).
- Контракты слоя 1 (`IWorld`, `IStateStream`, `ISimulationClock`) ещё
  не написаны: это следующий этап.

## Дальше

Этап 1 плана: `IEngine`, `EngineOptions`, `FrameResult`, затем контракты
`Ecs/`, `Replication/`, `Scheduling/` и минимальный шов `ISceneScript`.
