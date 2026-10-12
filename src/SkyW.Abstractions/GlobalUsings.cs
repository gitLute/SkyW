// Единственный тип-мост стека: JoltPhysicsSharp использует System.Numerics,
// Silk.NET.Maths переводится в него явно, Friflo оперирует им же в компонентах.
// Поэтому объявление доступно во всех подсистемах без повторения using
// (docs/stack-constraints.md раздел 3).
//
// Внешние библиотеки здесь не импортируются: проект не ссылается ни на кого,
// ни Silk.NET.Maths, ни Hexa.NET.Math, ни Friflo не должны просачиваться
// в прикладной код (docs/engine-architecture.md раздел 1.2).

global using System.Numerics;