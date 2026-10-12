// Алиасы пространств имён, обязательные для всего проекта.
//
// Алиас Hx обязан отличаться от первого сегмента неймспейса, на который он
// указывает: `using Hexa = Hexa.NET.Mathematics;` даёт CS0576 с невнятным
// сообщением, не указывающим на реальную причину
// (docs/stack-constraints.md раздел 4).
//
// Алиас объявлен здесь, а не по месту использования, чтобы правило нельзя было
// нарушить локально: это рекомендация из того же раздела.
//
// Импортировать Hexa.NET.Mathematics и JoltPhysicsSharp вместе нельзя: четыре имени
// дают CS0104 — BoundingBox, BoundingSphere, Ray, MathUtil.

global using Hx = Hexa.NET.Mathematics;

// Внутренний формат векторов, кватернионов и трансформов — только System.Numerics
// (docs/stack-constraints.md раздел 3). Это единственный тип-мост стека, потому что
// JoltPhysicsSharp использует System.Numerics, Silk.NET.Maths переводится в него
// явно через ToSystem() и FromSystem().
global using System.Numerics;