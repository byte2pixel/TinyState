# Contributing to TinyState

Thank you for your interest in contributing to TinyState! Your help is greatly appreciated. Please follow these guidelines to help us maintain a high-quality project.

> **Important:** TinyState must remain dependency-free. Do not add any external NuGet or other dependencies to the `TinyState` library project. If you believe a dependency is absolutely necessary, please open an issue for discussion first.

## Getting Started

- Fork the repository and clone your fork.
- Create a new branch for your feature or bugfix.
- Make your changes in the appropriate files.
- Write or update tests to cover your changes.
- Ensure all tests pass locally before submitting a pull request.

## Development

- This project uses [.NET 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).
- Run `dotnet build` and `dotnet test` to build and test the solution.
- Tests are located in the `tests/TinyState.Tests` directory and use the [TUnit](https://github.com/egil/TUnit) testing framework.

## Pull Requests

- Use the pull request template provided in `.github/pull_request_template.md`.
- Clearly describe your changes and reference any related issues.
- Check that your code builds and passes all tests in CI.
- Update documentation as needed.
- Be responsive to feedback and requested changes.

## Code Style

- Follow the existing code style and conventions.
- Use meaningful commit messages.
- Add XML documentation for public APIs and important methods.

## Reporting Issues

- Search existing issues before opening a new one.
- Provide as much detail as possible, including steps to reproduce, expected behavior, and screenshots if applicable.

## License

By contributing, you agree that your contributions will be licensed under the MIT License.

Thank you for helping make TinyState better!
