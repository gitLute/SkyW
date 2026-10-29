# Конвенция плотного кода

Дата: 2026-10-29
Ветка: core-folders

## Задача

Записать в `AGENTS.md` конвенцию плотного кода и правило необходимости
интерфейса, затем привести существующий код к этой конвенции.

## Что сделано

- `AGENTS.md` — раздел 1.4 «Плотный код»: пустые строки только между типами
  и методами, группы свойств под комментарием из 1-2 слов, однострочные
  `if/for/foreach/while/using` без скобок, primary constructor для тела из
  одних присваиваний; пункт 4.9 — когда интерфейс оправдан; 4 строки
  в таблице частых ошибок.
- `src/SkyW.Abstractions` — `StateBatchHeader`, `VisualId`, `PrefabId`,
  `ILog`, `ILogSink`, `IStage`, `ISimulationClock`, `IStateStream`, `IOutput`
  приведены к плотному виду; `StateBatchHeader` и `VisualId` — primary ctor.
- `src/SkyW.Ecs` — `EntityRef`, `ComponentId`, `IWorld`, `IQuery`,
  `IEntityStore`, `ICommandQueue`, `EntityAction`.
- `src/SkyW.Engine`, `src/SkyW.Scenes` — `EngineOptions`, `IEngine`,
  `SceneContext`, `ISceneScript`, `IPrefabManager`.
- `src/SkyW.Diagnostics` — `EngineLog`, `FileLogSink`, `RingBufferLogSink`,
  `ConsoleLogSink` (primary ctor), `LogFormatter`.
- `tests` — `ProjectGraph` (primary ctor, expression-bodied методы),
  `LayerRulesTests`, `TestDoubles`, `EcsContractTests` (`Probe` — primary ctor)
  и остальные тесты: убраны пустые строки внутри методов, свёрнуты
  однострочные циклы и `using`-блоки.

Поведение не менялось: правки механические, кроме `ProjectGraph`, где
переименованы локальные словари под параметры primary ctor.

## Проверка

- `dotnet build SkyW.slnx -c Release` — 0 ошибок, 0 предупреждений.
- `dotnet test --solution SkyW.slnx` — итог 79, сбой 0, успешно 79.
  Код 8 даёт пустой `SkyW.Tests.Stack`.
- Скрипт-проверка по `src`, `tests`, `tools`: не осталось ни одного блока
  `if/for/foreach/while/using/lock` с единственным оператором в скобках,
  ни пустых строк внутри тел методов.

## Открытые вопросы

- `IPrefab` и `IPrefabManager` нарушают пункт 4.9: реализация одна и в том
  же проекте. Удалять без подтверждения владельца нельзя — вопрос вынесен.
- Замер производительности не делался: задача про оформление, горячие пути
  не менялись.

## Дополнение (правило уточнено)

- `AGENTS.md` 1.4: формулировка расширена — любая конструкция, открывающая
  блок, с одним оператором пишется одной строкой без скобок (`if`, `for`,
  `foreach`, `while`, `do`, `using`, `lock`, `fixed`); одно выражение в теле
  метода/аксессора — expression-bodied (`=>`); `checked`/`unchecked` блоком
  требуют скобок по грамматике.
- Код: `FileLogSink.Roll` — `else` одной строкой; 6 тестовых методов с
  единственным `Assert` переведены в expression-bodied; `Flush` и
  `RecordingLogSink.Write` остаются на `lock` (блок необходим).
- Проверка: build 0/0, тесты 79, сбой 0, код 8.

## Дальше

Этап 2: `FrifloEntityStore`, запросы, dirty-биты; замер на 10 000
сущностях и 1000 шагах с нулевыми аллокациями.
