# TinyState

[![NuGet](https://img.shields.io/nuget/vpre/Byte2Pixel.TinyState.svg?logo=nuget)](https://www.nuget.org/packages/Byte2Pixel.TinyState)
[![GitHub release](https://img.shields.io/github/v/release/Byte2Pixel/TinyState?logo=github)](https://github.com/Byte2Pixel/TinyState/releases)
[![Build Status](https://github.com/Byte2Pixel/TinyState/actions/workflows/dotnet.yaml/badge.svg)](https://github.com/Byte2Pixel/TinyState/actions)

> **⚠️ This package is under active development and is not ready for use. Contributions are not being accepted at this time.**

TinyState is a lightweight, fluent, and dependency-free state machine library for .NET. Build robust workflows, UI logic, or domain models with a clean and minimal API.

TinyState is a minimal yet powerful state machine library for .NET developers.
It helps you model transitions and logic flows using a clean, fluent API — without any external dependencies.

✨ Features:

- Compile-time safe fluent configuration
- Async and sync transition hooks (OnEnter, OnExit, OnTransition)
- Optional guards (sync/async) for transitions
- Immutability and runtime safety
- Optional state persistence

## Example Usage

```csharp
var machine = new StateMachine<OrderState, OrderTrigger>(OrderState.Created);

machine
    .Configure(OrderState.Created)
        .Trigger(OrderTrigger.Pay)
            .TransitionTo(OrderState.Paid)
        .Trigger(OrderTrigger.Cancel)
            .TransitionTo(OrderState.Cancelled)
    .OnEnter(() => Console.WriteLine("Order created."));

machine
    .Configure(OrderState.Paid)
        .Trigger(OrderTrigger.Ship)
            .When(() => inventoryAvailable)
            .TransitionTo(OrderState.Shipped)
        .Trigger(OrderTrigger.Cancel)
            .TransitionTo(OrderState.Cancelled)
    .OnEnter(() => Console.WriteLine("Order paid."));

machine
    .Configure(OrderState.Shipped)
        .Trigger(OrderTrigger.Deliver)
            .TransitionTo(OrderState.Delivered)
    .OnEnter(() => Console.WriteLine("Order shipped."));

machine.Configure(OrderState.Delivered);
machine.Configure(OrderState.Cancelled);
```

### Key Points
- After `.Trigger(...)`, you can chain `.When(...)`, `.WhenAsync(...)`, and `.TransitionTo(...)` in any order, but only `.Trigger(...)` or hooks after `.TransitionTo(...)`.
- Guards are optional and can be chained; all must pass for the transition to occur.

---

### Installation
You can install TinyState via NuGet:

```bash
dotnet add package Byte2Pixel.TinyState
```
