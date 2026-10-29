# SkyW

2.5D/3D game engine (Silk.NET, Friflo ECS, Jolt, Hexa.NET.Math)

Целевой фреймворк: `net10.0`. SDK закреплён в `global.json`.

## Сборка и запуск

```bash
dotnet build SkyW.slnx -c Release
dotnet test --solution SkyW.slnx
dotnet run --project src/SkyW.Sandbox
```

`dotnet test` на .NET 10 работает через Microsoft.Testing.Platform; раннер объявлен
в `global.json`. Код возврата 8 означает «тестов ещё нет», а не сбой.

Инструменты контента запускаются отдельно, каждая как самостоятельное приложение:

```bash
dotnet run --project tools/SkyW.Tools.Atlas
dotnet run --project tools/SkyW.Tools.Cook
dotnet run --project tools/SkyW.Tools.Terrain
```

## Структура

Каталоги следуют за подсистемами, а не за типами. Подробное обоснование раскладки
и связей между библиотеками — в `docs/`.

| Слой | Проекты | Что внутри |
|---|---|---|
| 1. Контракты | `src/SkyW.Abstractions` | интерфейсы движка; внешних зависимостей нет |
| 2. Подсистемы | `src/SkyW.Mathematics`, `src/SkyW.Diagnostics`, `src/SkyW.Content`, `src/SkyW.Scenes`, `src/SkyW.Ecs` | математика, профилирование, форматы ресурсов, сцены и префабы, типизированный шов ECS |
| 3. Сервисы | `src/SkyW.Simulation`, `src/SkyW.Rendering`, `src/SkyW.Input` | планировщик и репликация, рендер-конвейер, логика ввода |
| 4. Реализации | `src/SkyW.Backends.Silk`, `src/SkyW.Backends.Jolt`, `src/SkyW.Backends.Friflo` | реализации внешних библиотек; Friflo дополнительно нужен `SkyW.Ecs` |
| Композиция | `src/SkyW.Engine` | сборка зависимостей, фазы запуска, каналы, вывод |
| Приложения | `src/SkyW.Host`, `src/SkyW.Sandbox` | тонкая точка входа и демонстрация |

Инструменты лежат в `tools/`, тесты — в `tests/`, контент — в `assets/`, документы — в `docs/`.

Зависимости направлены внутрь: прикладной код обращается к интерфейсам, реализации
их удовлетворяют. Граф ссылок проверяется тестом `tests/SkyW.Tests.Architecture`.

`src/SkyW.Ecs` — единственный проект слоя 2, которому разрешён Friflo: маркер
`IComponent` обязателен для типа компонента. Прикладные компоненты реализуют
`SkyW.Ecs.IComponent`, не импортируя Friflo напрямую.

## Документация

- `docs/engine-architecture.md` — архитектурные требования: SOLID, абстракции над
  внешними зависимостями, раздельные частоты симуляции и рендера, регистры, префабы,
  рендер-конвейер, профилирование.
- `docs/stack-constraints.md` — проверенные особенности сборки стека: обязательный
  `Foundation.Init` у Jolt, конфликты имён, запрет структурных изменений Friflo
  внутри обхода, регрессионный тест стека.
- `docs/graphics-art-direction-lowpoly-pixel-3d.md` — визуальное направление: низкое
  внутреннее разрешение, целочисленное увеличение, атлас спрайтов, билборды, интерфейс
  в своём разрешении.