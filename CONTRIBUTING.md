# Contributing to pg_protoexport

Thanks for your interest in improving pg_protoexport! Contributions — bug reports,
feature ideas, documentation fixes, and code — are all welcome.

## Ways to contribute

- **Found a bug or have a question?** Open an [issue](https://github.com/xfischer/pg_protoexport/issues)
  or start a [discussion](https://github.com/xfischer/pg_protoexport/discussions).
- **Want to fix or build something?** See the workflow below.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com)
- A pcap library at runtime, needed for reading and recording captures:
  - **Windows:** [Npcap](https://npcap.com/) (install with *WinPcap API-compatible mode* enabled for loopback support)
  - **Linux/macOS:** libpcap (with `CAP_NET_RAW` or root for live capture)

## Build and test

```bash
# Build the whole solution
dotnet build pg_protoexport.slnx

# Run the tests
dotnet test pg_protoexport.tests/pg_protoexport.tests.csproj

# Run a single test
dotnet test pg_protoexport.tests/pg_protoexport.tests.csproj --filter "FullyQualifiedName~TestMethodName"
```

The same build + test commands run in CI (`.NET` workflow). **Please make sure both pass
before opening a pull request.**

## Project layout & conventions

[CLAUDE.md](CLAUDE.md) is the authoritative architecture guide. A few highlights:

- The pipeline is **`PcapService` (core) → exporters**; each exporter lives in its own
  `pg_protoexport.export.*` project and implements `IPcapExporter`.
- **Adding a new exporter** is a documented, self-contained pattern — see the
  *"Adding a new exporter"* section in [CLAUDE.md](CLAUDE.md). The CLI host discovers
  exporters via `IExporterCliModule`, so a new exporter touches the CLI project by exactly
  one composition line.
- All types use a **flat, file-scoped namespace**: `namespace pg_protoexport;`.
- NuGet versions are **centrally managed** in `Directory.Packages.props` (CPM). Add or bump
  versions there, not in individual `.csproj` files.
- Target framework is **.NET 10**.
- Code style is enforced by [.editorconfig](.editorconfig).

## Pull request workflow

1. Fork the repo and create a topic branch off `main` (e.g. `feature/...`, `fix/...`).
2. Make your change, with tests where it makes sense.
3. Run `dotnet build pg_protoexport.slnx` and `dotnet test ...` locally.
4. Update [README.md](README.md) and/or [CLAUDE.md](CLAUDE.md) if you changed behavior or architecture.
5. Open a PR against `main` and fill in the PR template. CI must be green to merge.

## License

By contributing, you agree that your contributions are licensed under the
[PostgreSQL License](LICENSE), the same license that covers this project.
