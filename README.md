# TinyState

[![NuGet](https://img.shields.io/nuget/vpre/Byte2Pixel.TinyState.svg?logo=nuget)](https://www.nuget.org/packages/Byte2Pixel.TinyState)
[![GitHub release](https://img.shields.io/github/v/release/Byte2Pixel/TinyState?logo=github)](https://github.com/Byte2Pixel/TinyState/releases)
[![Build Status](https://github.com/Byte2Pixel/TinyState/actions/workflows/dotnet.yaml/badge.svg)](https://github.com/Byte2Pixel/TinyState/actions)

> **⚠️ This package is under active development and is not ready for use. Contributions are not being accepted at this time.**

TinyState is a lightweight, fluent, and dependency-free state machine library for .NET. Build robust workflows, UI logic, or domain models with a clean and minimal API.

TinyState is a minimal yet powerful state machine library for .NET developers.
It helps you model transitions and logic flows using a clean, fluent API — without any external dependencies.

✨ Features:

- Fluent configuration syntax: machine.Configure(State.A).When(Event.X).GoTo(State.B)
- Async transition hooks (OnEnter, OnExit, OnTransition)
- Immutability and runtime safety
- Optional state persistence


🛠️ Ideal for:
 
- UI navigation logic
- Domain-driven design (DDD) aggregates
- Game and simulation state control
- Rule-based workflows or user onboarding
- Event-driven architectures

# Important Usage Notes

- **All target states must be configured**: Any state you transition to (using `.GoTo(...)`) must be explicitly configured with `.Configure(State)`, even if you do not add hooks or transitions for that state. This ensures the state machine can safely execute hooks and maintain runtime safety.
- **Hook execution order**: If you provide both async and sync hooks (e.g., `OnEnterAsync` and `OnEnter`), the async hook will always run before the sync hook. This order is not configurable and ensures that asynchronous side effects complete before synchronous logic runs.
- **Exceptions**: The state machine will throw an `InvalidOperationException` if you attempt to fire a trigger that leads to an unconfigured state, or if a transition is not defined for the current state and trigger.

---
