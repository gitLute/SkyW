# Ограничения и особенности сборки SkyW

Статус: проверенный фактами документ. Описывает то, что **реально мешает
работать** с набором зависимостей Silk.NET + Friflo ECS + Jolt + Hexa.NET.Math.
Архитектурные требования вынесены отдельно: **`engine-architecture.md`**.

Документ отвечает на вопрос «что сломается и как это обойти», а не «как
устроен движок».

---

## 0. Назначение и границы

Здесь собрано то, что **невозможно узнать из документации библиотек** и что
стоит узнавать только экспериментом: обязательный порядок инициализации,
конфликты имён, ограничения API. Каждое утверждение помечено источником:

- **Проверено** — воспроизведено запуском на машине разработки.
- **Из документации** — следует из исходников или описания пакета.
- **Риск** — не проверено, но следует из версий и дат релизов.

Если пункт помечен «Проверено», он воспроизводится тестом из раздела 8.

### Стек

| Слой | Пакет | Версия | Лицензия |
|---|---|---|---|
| Окно, ввод, рендер | `Silk.NET` | 2.23.0 | MIT |
| Математика (расширенная) | `Hexa.NET.Math` | 2.0.8 | MIT |
| Физика | `JoltPhysicsSharp` | 2.23.0 | MIT |
| ECS | `Friflo.Engine.ECS` | 3.6.0 | MIT |
| Сериализация | `Friflo.Json.Fliox` | 1.0.4 (транзитивно) | MIT |

Все пять — MIT: коммерческих ограничений и copyleft в стеке нет.

---

## 1. Обязательный целевой фреймворк: net9.0 или новее

`JoltPhysicsSharp` 2.23.0 публикуется **только** под `net9.0` и `net10.0`.
Это ограничивает весь проект, а не только модуль физики.

**Проверено:** сборка и запуск стека проходят на .NET 9.0.20 и .NET 10.0.12.

Остальные пакеты ограничений не накладывают:

| Пакет | Поддерживаемые фреймворки |
|---|---|
| `JoltPhysicsSharp` 2.23.0 | `net9.0`, `net10.0` |
| `Hexa.NET.Math` 2.0.8 | `net6.0`…`net10.0`, `netstandard2.0`, `netstandard2.1` |
| `Silk.NET` 2.23.0 | `netcoreapp3.1`, `net5.0`, `netstandard2.0`, `netstandard2.1` |
| `Friflo.Engine.ECS` 3.6.0 | `net6.0`, `net7.0`, `net8.0`, `netstandard2.1` |

**Особенность.** `Friflo.Engine.ECS` — единственный отставший пакет: максимальный
фреймворк `net8.0`, последний релиз 2026-03-23. На `net9.0`/`net10.0`
резолвится его `net8.0`-сборка, и это работает, но именно Friflo первым
упрётся, если понадобится что-то, специфичное для .NET 10.

**Следствие.** `net8.0` для SkyW невозможен. Выбор между `net9.0` и `net10.0`
делается один раз; рекомендуется `net10.0`, потому что `netstandard2.1`-путь
Silk.NET берётся в обоих случаях одинаково.

---

## 2. Jolt обязан быть инициализирован до любого другого вызова

**Проверено.** Это главная причина падения стека, и ни README, ни страница пакета
про неё не пишут.

Первый вызов в нативную библиотеку без инициализации обрушивает процесс
сегфолтом — **не исключение C#, а падение нативного кода**:

```
1 ObjectLayerPairFilterTable
код возврата = 139
```

`139` — это `SIGSEGV`. Управляемое исключение не выбрасывается, catch не поможет,
сообщения об ошибке не будет.

Обязательная последовательность:

```csharp
if (!Jolt.Foundation.Init(false))   // false = float, true = double
    return;

// ... вся работа с физикой ...

Jolt.Foundation.Shutdown();
```

**Проверено:** после добавления `Foundation.Init` тот же тест проходит полностью.

### Почему это легко пропустить

- Документация пакета сводится к одной строке `dotnet add package`.
- Ошибка проявляется не сразу, а при первом обращении к физике, то есть обычно
  уже в работающем приложении.
- Упасть может не само создание мира, а любой более ранний вызов — фильтры
  слоёв создаются раньше `PhysicsSystem`.

**Требование к движку.** Инициализация обязана выполняться внутри обёртки
физического мира, а не в прикладном коде. Иначе рано или поздно появится путь
выполнения, который её пропустит.

### Выбор точности — решение на весь проект

`Foundation.Init(true)` переводит нативную часть в режим двойной точности, и
тогда в API меняются типы:

| `Init(false)` | `Init(true)` |
|---|---|
| `System.Numerics.Vector3` | `JoltPhysicsSharp.RVector3` |
| `System.Numerics.Matrix4x4` | `JoltPhysicsSharp.RMatrix4x4` |

Переключение после старта проекта — это переписывание всех вызовов физики.
`Hexa.NET.Math` закрывает оба режима (`Vector3D`, `Matrix4x4D`), так что сама по
себе смена режима не требует новой математики, но требует правок в коде.

Для проекта с тайлами и масштабом до километров хватает `false`. Переходить на
`true` нужно при мире больше примерно 10 км по одной оси или при требовании
детерминизма.

---

## 3. Единственный тип-мост стека — `System.Numerics`

Это то, ради чего стек собран именно так, и это надо понимать до написания кода.

**Проверено по исходникам:**

- `JoltPhysicsSharp` использует `System.Numerics.Vector3`, `Quaternion`,
  `Matrix4x4` как основные типы. Собственных `Vec3`, `Quat`, `Mat44` у него
  **нет вообще**. Двойная точность реализована отдельно через `RVector3` и
  `RMatrix4x4`.
- `Hexa.NET.Math` — надстройка над `System.Numerics`: базовые `Vector2/3/4`,
  `Matrix4x4`, `Quaternion` берутся из BCL, свои типы имеют суффикс `D`
  (`Vector3D`, `Matrix4x4D`, `QuaternionD`, `PlaneD`), целочисленные — `Point2/3/4`.
- `Friflo.Engine.ECS` оперирует `System.Numerics.Vector3` в компонентах.
- `Silk.NET.Maths` держит **собственный** набор дженерик-типов: `Vector3D<T>`,
  `Matrix4X4<T>`, `Quaternion<T>`, `Plane<T>`, плюс `Scalar` с дженерик-математикой.

Мост Silk → BCL прописан явно, в файле `SystemNumericsExtensions.cs`:
двусторонние методы `ToSystem()` и `FromSystem()` для семи основных типов.

```csharp
Matrix4X4<float> m = Matrix4X4.CreatePerspectiveFieldOfView<float>(fov, aspect, near, far);
System.Numerics.Matrix4x4 native = m.ToSystem();   // то, что ждёт OpenGL и Jolt
```

**Следствие для проекта.** Внутренний формат трансформа и векторов — только
`System.Numerics`. Дженерик-типы Silk.NET.Maths используются там, где нужна
`Half` или целочисленная точность, и точечно, с переводом на границе.

**Проверено:** `Matrix4X4.CreateRotationY<float>(...)` вернул матрицу, которая
после `ToSystem()` дала `M11 = 0.7071` — математика сходится.

### Две мелочи API Silk.NET.Maths

Обе обнаружены при первой сборке и обе выглядят как «неправильная» версия кода
из `System.Numerics`:

- Фабрики матриц — **статические дженерик-методы на не-дженерик классе**:
  `Matrix4X4.CreateRotationY<float>(...)`, а не `new Matrix4X4<float>()`.
- У `System.Numerics.Matrix4x4` **нет метода `Determinant`**. Проверка
  обратимости — через `Matrix4x4.Invert`.

---

## 4. Конфликты имён между Hexa.NET.Math и Jolt

**Проверено.** Одновременный импорт `Hexa.NET.Mathematics` и `JoltPhysicsSharp`
даёт `CS0104` ровно на четырёх именах:

| Имя | Конфликт |
|---|---|
| `BoundingBox` | `JoltPhysicsSharp.BoundingBox` против `Hexa.NET.Mathematics.BoundingBox` |
| `BoundingSphere` | то же |
| `Ray` | то же |
| `MathUtil` | то же |

Ошибка выглядит так:

```
error CS0104: 'BoundingBox' is an ambiguous reference between
'JoltPhysicsSharp.BoundingBox' and 'Hexa.NET.Mathematics.BoundingBox'
```

Проверено перебором остальных типов: `Vector3`, `Quaternion`, `Plane`, `Color`,
`Rectangle` конфликтов не дают.

### Обязательное правило проекта

Импортировать эти два пространства имён вместе нельзя. Алиасы обязательны:

```csharp
global using Hx = Hexa.NET.Mathematics;
global using Jolt = JoltPhysicsSharp;
```

**Ловушка в самих алиасах.** Имя алиаса не может совпадать с первым сегментом
неймспейса, на который он указывает:

```csharp
using Hexa = Hexa.NET.Mathematics;   // CS0576
using Hx   = Hexa.NET.Mathematics;   // корректно
```

Ошибка выглядит невнятно — `Namespace '<global namespace>' contains a definition
conflicting with alias 'Hexa'` — и не указывает на реальную причину.

**Рекомендация.** Вынести алиасы в `GlobalUsings.cs` модуля математики,
чтобы правило нельзя было нарушить локально.

**Следствие для архитектуры.** Ещё одна причина прятать обе библиотеки за
собственными интерфейсами движка: прикладной код не должен видеть
`JoltPhysicsSharp.BoundingBox` рядом с `Hx.BoundingBox` и выбирать между ними.

---

## 5. Friflo запрещает менять структуру внутри обхода

**Проверено.** Вызов `AddComponent` или `RemoveComponent` внутри `ForEachEntity`
бросает исключение:

```
Friflo.Engine.ECS.StructuralChangeException: within query loop.
   at Friflo.Engine.ECS.Entity.AddComponent[T](T& component)
   at ... ArchetypeQuery`1.ForEachEntity(ForEachEntity`1 lambda)
```

`AddComponent` внутри обхода **не нужен**: запись в `ref`-параметр сразу попадает
в чанк данных.

```csharp
// Неверно
query.ForEachEntity((ref Transform t, Entity e) =>
{
    t.Position = bodies.GetCenterOfMassPosition(bodyId);
    e.AddComponent(t);                       // StructuralChangeException
});

// Верно
query.ForEachEntity((ref Transform t, in PhysicsBody b) =>
{
    t.Position = bodies.GetCenterOfMassPosition(b.Id);
});
```

**Требование к движку.** Создание и удаление сущностей, добавление и снятие
компонентов разрешены только в точках, где обход не идёт: до старта шага
физики и после его завершения. Внутри шага — только запись в существующие
компоненты.

### Мелочь API Friflo

Метода `Entity.SetComponent` нет. Замена значения компонента делается через
`Entity.AddComponent<T>(in T component)` — вне обхода.

---

## 6. Особенности Hexa.NET.Math

**Полезно:**

- SIMD-интринсики включены под `#if NET8_0_OR_GREATER`, то есть на
  `net9.0`/`net10.0` работает ускоренный путь.
- На `net6.0` и новее **нулевые зависимости**. Обвязка из пяти пакетов
  (`IndexRange`, `Microsoft.Bcl.HashCode`, `Microsoft.Bcl.Numerics`,
  `System.Numerics.Vectors`, `System.Text.Json`) подтягивается только на
  `netstandard2.0`, а `netstandard2.1` тянет один `System.Text.Json`.
  SkyW на netstandard не собирается, поэтому эта зависимость неактуальна —
  но всплывёт, если кто-то решит собрать библиотеку под Godot Mono.
- Две редакции пакета: обычная и `-minimal`. Версионная схема неочевидна:
  релиз `2.0.8-minimal` не всегда выходит вместе с `2.0.8`.
- Класс `MathUtil` объявлен `unsafe` и частичный. Проект собирается без
  `AllowUnsafeBlocks`, пока используются только константы и чистые функции.

**Что стоит учитывать:**

- **Риск.** Последний релиз 2026-03-08, шесть месяцев без изменений.
  Библиотека не заброшена, но и не развивается.
- Функции теневого отображения (`OSM`, `PSM`, `DPSM`, `CSM`) и класс
  `Transform` есть **только в полной редакции**. В минимальной их нет.

**Что пригодится за пределами `System.Numerics`:** `BoundingBox`,
`BoundingFrustum` для отсечения, `MathUtil` для констант и тригонометрии,
`Color` для палитры, кривые Безье и полиномы для процедурного контента, шум
Перлина и симплекс, модели неба Претама и Хосека-Вилки.

---

## 7. Особенности Silk.NET

- Пакет `Silk.NET` тянет за собой **весь** стек: `Core`, `Input`, `OpenAL`,
  `OpenGL`, `Vulkan`, `Vulkan.Extensions.KHR`, `GLFW`, `Windowing`. Даже если
  проект использует только OpenGL, в зависимость попадает Vulkan и OpenAL.
  Для настольного приложения это лишние мегабайты в выходной папке; при
  публикации это учитывать в размере сборки.
- Ставится `netstandard2.1`-сборка, потому что своих `net6.0` и новее у пакета
  нет. На практике это прозрачно.
- `Silk.NET.Maths` — отдельный пакет с обширным набором форм, которых нет в
  BCL: `Box2D`, `Box3D`, `Cube`, `Circle`, `Sphere`, `Ray2D`, `Ray3D`,
  `Rectangle`, матрицы от `2X2` до `5X4`. Все они дженерик.
- Вся математика Silk.NET.Maths — дженерик-типы. **Недженерик-версий
  `Vector3` и `Matrix4x4` в пакете нет.** Это снимает конфликт имён с
  `System.Numerics` при совместном импорте — проверено, компиляция проходит.

---

## 8. Регрессионный тест стека

Тест, которым проверялась совместимость. Что он ловит:

- отсутствие `Foundation.Init` — процесс падает с кодом возврата `139`;
- конфликты имён — ошибка компиляции `CS0104`;
- изменение публичного API после обновления пакета — ошибка компиляции;
- расхождение физики или моста Silk.NET.Maths — исключение с числами.

Правило Friflo о структурных изменениях тест не проверяет намеренно: он сам
написан по правильному образцу, то есть служит образцом, а не ловушкой.

Проверен на .NET 9.0.20 и .NET 10.0.12. Код приведён в том виде, в каком
компилировался и запускался; для запуска нужен лишь вызов `StackCheck.Run()`
из точки входа.

```csharp
using System.Numerics;
using Friflo.Engine.ECS;
using Silk.NET.Maths;
using Hx = Hexa.NET.Mathematics;
using Jolt = JoltPhysicsSharp;

public struct BodyState : IComponent
{
    public Vector3 Position;
    public Vector3 Velocity;
}

internal static class StackCheck
{
    public static void Run()
    {
        // 1. Мост Silk.NET.Maths -> System.Numerics
        Matrix4X4<float> silk = Matrix4X4.CreateRotationY<float>(MathF.PI * 0.25f);
        System.Numerics.Matrix4x4 bridged = silk.ToSystem();

        // 2. Hexa.NET.Math поверх System.Numerics
        Hx.BoundingBox aabb = new(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));

        // 3. Jolt: инициализация обязательна
        if (!Jolt.Foundation.Init(false))
            throw new InvalidOperationException("Foundation.Init не отработал");

        Jolt.ObjectLayer nonMoving = 0;
        Jolt.ObjectLayer moving = 1;

        var objectFilter = new Jolt.ObjectLayerPairFilterTable(2);
        objectFilter.EnableCollision(nonMoving, moving);
        objectFilter.EnableCollision(moving, moving);

        var broadPhase = new Jolt.BroadPhaseLayerInterfaceTable(2, 2);
        broadPhase.MapObjectToBroadPhaseLayer(nonMoving, 0);
        broadPhase.MapObjectToBroadPhaseLayer(moving, 1);

        var settings = new Jolt.PhysicsSystemSettings
        {
            MaxBodies = 1024,
            MaxBodyPairs = 1024,
            MaxContactConstraints = 1024,
            ObjectLayerPairFilter = objectFilter,
            BroadPhaseLayerInterface = broadPhase,
            ObjectVsBroadPhaseLayerFilter =
                new Jolt.ObjectVsBroadPhaseLayerFilterTable(broadPhase, 2, objectFilter, 2),
        };

        using var jobSystem = new Jolt.JobSystemThreadPool();
        using var physics = new Jolt.PhysicsSystem(settings);
        var bodies = physics.BodyInterface;

        using (var floorSettings = new Jolt.BoxShapeSettings(new Vector3(50f, 0.5f, 50f)))
        using (var creation = new Jolt.BodyCreationSettings(
                   floorSettings, new Vector3(0, -0.5f, 0),
                   Quaternion.Identity, Jolt.MotionType.Static, nonMoving))
        {
            bodies.AddBody(bodies.CreateBody(creation).ID, Jolt.Activation.DontActivate);
        }

        Jolt.BodyID ballId;
        using (var ballSettings = new Jolt.SphereShapeSettings(0.5f))
        using (var creation = new Jolt.BodyCreationSettings(
                   ballSettings, new Vector3(0, 10, 0),
                   Quaternion.Identity, Jolt.MotionType.Dynamic, moving))
        {
            Jolt.Body body = bodies.CreateBody(creation);
            bodies.AddBody(body.ID, Jolt.Activation.Activate);
            ballId = body.ID;
        }

        // 4. Friflo ECS: состояние пишется в ref-параметр, без AddComponent
        var world = new EntityStore();
        world.CreateEntity(new BodyState { Position = new Vector3(0, 10, 0) });

        var query = world.Query<BodyState>();
        query.ForEachEntity((ref BodyState state, Entity entity) =>
        {
            for (int step = 0; step < 120; step++)
            {
                Jolt.PhysicsUpdateError error = physics.Update(1f / 60f, 1, jobSystem);
                if (error != Jolt.PhysicsUpdateError.None)
                    throw new InvalidOperationException("Jolt update: " + error);
            }

            state.Position = bodies.GetCenterOfMassPosition(ballId);
            state.Velocity = bodies.GetLinearVelocity(ballId);
        });

        query.ForEachEntity((ref BodyState state, Entity entity) =>
        {
            // Сфера радиуса 0.5 должна остановиться на Y около 0.5
            if (MathF.Abs(state.Position.Y - 0.5f) > 0.01f)
                throw new Exception("Физика не сошлась: " + state.Position);

            if (!aabb.Contains(new Vector3(0, 2, 0)).Equals(Hx.ContainmentType.Disjoint))
                throw new Exception("Hexa.NET.Math работает не так, как ожидалось");

            if (MathF.Abs(bridged.M11 - 0.7071f) > 0.001f)
                throw new Exception("Мост Silk.NET.Maths -> System.Numerics неверен");

            Console.WriteLine($"позиция = {state.Position}, скорость = {state.Velocity}");
        });

        Jolt.Foundation.Shutdown();
    }
}
```

Вывод этого теста на последней строке:

```
позиция = <0, 0.49999997, 0>, скорость = <0, 0, 0>
```

Для справки — вывод диагностической версии, на которой собирались сами
ограничения:

```
[silk]  ToSystem() -> Matrix4x4, M11 = 0.7071
[hexa]  PI = 3.1415927, центр сферы в AABB = Disjoint
[jolt]  Foundation.Init = true
[jolt]  позиция = <0, 0.49999997, 0>
[jolt]  скорость = <0, 0, 0>, модуль 0.000
[ec s ]  сущность 1, на полу = True
```

---

## 9. Обновление зависимостей

**Риск наибольший здесь.** Три из четырёх пакетов активны, но несовместимость
проявляется падением нативного кода, а не ошибкой компиляции.

| Пакет | Последний релиз | Что делать при обновлении |
|---|---|---|
| `JoltPhysicsSharp` | 2026-10-05 | Прогнать тест раздела 8 первым делом |
| `Hexa.NET.Math` | 2026-03-08 | Не обновлять без нужды |
| `Silk.NET` | 2026-01-23 | Проверять отдельно `Silk.NET.Maths` |
| `Friflo.Engine.ECS` | 2026-03-23 | Следить за целевыми фреймворками |

Известная особенность упаковки Jolt: `JoltPhysicsSharp` 2.22.0 и 2.23.0
требуют `JoltPhysics.Native` версии `1.1.0`, а нативная сборка с этой версии
вышла 2026-07-13 — за три месяца до managed-релиза 2.23.0. Полностью
проверенная комбинация — **2.23.0 + native 1.1.0** на .NET 9.0.20 и 10.0.12.

Отдельно зафиксировано: версии 2.19.5, 2.20.1, 2.21.0 и 2.22.0 ведут себя
точно так же, как 2.23.0 — падают с `SIGSEGV` при первом вызове без
`Foundation.Init`. То есть причина падения **не в несовместимости версий**, а
в невызванной инициализации. Откатываться на предыдущую версию при `SIGSEGV`
не нужно: сначала проверить, вызывается ли `Foundation.Init`.

**Правило.** Обновление Jolt идёт отдельным коммитом с прогоном теста раздела 8.
Никогда не в общем коммите с изменением игрового кода: падение нативной
библиотеки невозможно отладить по стеку managed-кода.

### Проверка перед обновлением

1. Прогнать тест раздела 8 на текущих версиях.
2. Обновить один пакет.
3. Прогнать тест снова. Код возврата `139` вместо `0` — несовместимость.
4. Только после этого продолжать.

---

## 10. Чеклист ревью

- [ ] Проект собирается под `net9.0` или новее
- [ ] `Hexa.NET.Mathematics` и `JoltPhysicsSharp` не импортируются вместе без
      алиасов `Hx` и `Jolt`
- [ ] Алиас не совпадает с первым сегментом неймспейса (иначе `CS0576`)
- [ ] `Foundation.Init` вызывается до любого другого вызова Jolt, внутри
      обёртки физического мира, а не в прикладном коде
- [ ] `Foundation.Shutdown` вызывается при выходе
- [ ] Внутри обхода Friflo нет `AddComponent`, `RemoveComponent` и других
      структурных изменений
- [ ] Внутренние типы трансформа — `System.Numerics`, без исключений
- [ ] Физика считает фиксированным шагом, а не по времени кадра
- [ ] Сборка не использует триминг: нативные `.so` тримить нельзя
- [ ] Тест раздела 8 проходит

---

## 11. Как проверялось

Среда проверки: Arch Linux, x86-64, CPU с AVX2, glibc 2.44.
SDK: 9.0.121 и 10.0.112. Рантаймы: 8.0.31, 9.0.20, 10.0.12.
Дата проверки: 2026-10-12.

- Состав и версии пакетов — через API реестра NuGet (`registration5-semver1`).
- Типы и мосты — по исходникам: `JoltPhysicsSharp` (`MathUtil.cs`,
  `RVector3.cs`, `samples/HelloWorld/`), `dotnet/Silk.NET`
  (`src/Maths/Silk.NET.Maths/SystemNumericsExtensions.cs`), `friflo/Friflo.Engine.ECS`,
  `HexaEngine/Hexa.NET.Math`.
- Совместимость — реальной сборкой и запуском, а не чтением описаний. Код из
  раздела 8 вынесен в отдельный проект и проверен в том виде, в каком приведён
  здесь: он компилируется и проходит на обеих версиях .NET.
- Нативная библиотека Jolt для Linux присутствует в пакете
  (`JoltPhysics.Native/runtimes/linux-x64/native/libjoltc.so`), все её
  зависимости разрешаются.

---

## 12. Открытые вопросы

1. **Точность физики.** `Foundation.Init(false)` или `true`? Влияет на типы во
   всём коде физики. Требует решения до начала реализации.
2. **Публикация.** Тянуть ли вендорную сборку `Silk.NET` с неиспользуемыми
   Vulkan и OpenAL, или собирать рендер на отдельных пакетах
   (`Silk.NET.Windowing.Glfw`, `Silk.NET.OpenGL`).
3. **Альтернатива Friflo.** Пакет отстаёт по фреймворкам. Стоит ли закладывать
   замену ECS, учитывая требование абстракции из `engine-architecture.md`.
4. **Форк Hexa.NET.Math.** Библиотека не меняется полгода. Если упрёмся в
   недостающую функцию — писать её самим или форкать?
