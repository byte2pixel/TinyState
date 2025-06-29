# TinyState

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
