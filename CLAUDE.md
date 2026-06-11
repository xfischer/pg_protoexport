# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this project does

pg_protoexport reads network capture files (.pcapng, .pcap), extracts PostgreSQL wire protocol conversations, and generates output in several formats: LaTeX diagrams (using the `bytefield` package), PQTrace-style tab-separated text, Markdown files with embedded Mermaid or PlantUML diagrams, and a self-contained "guided reading" HTML report.

It also performs **live capture** in-process via SharpPcap: `LiveCaptureSession` writes a `.pcapng` file from a live NIC for the duration of a workload, removing the need to start `tcpdump`/Wireshark by hand.

## Build and test commands

```bash
# Build entire solution
dotnet build pg_protoexport.slnx

# Build just the CLI
dotnet build pg_protoexport/pg_protoexport.csproj

# Run all tests
dotnet test pg_protoexport.tests/pg_protoexport.tests.csproj

# Run a single test by name
dotnet test pg_protoexport.tests/pg_protoexport.tests.csproj --filter "FullyQualifiedName~TestMethodName"

# Run the CLI. Port is now a `--port` option; if omitted, the dominant PostgreSQL
# port is auto-detected from each capture's TCP SYN handshake.
dotnet run --project pg_protoexport -- latex <file.pcapng> <output.tex>
dotnet run --project pg_protoexport -- latex <file.pcapng> <output.tex> --port 5432 --exact --row-bytes 32
dotnet run --project pg_protoexport -- pqtrace <file.pcapng> <output.txt>
dotnet run --project pg_protoexport -- mermaid <file.pcapng> sequenceDiagram <output.md>
dotnet run --project pg_protoexport -- mermaid <file.pcapng> packet <output.md>
dotnet run --project pg_protoexport -- plantuml <file.pcapng> sequenceDiagram <output.md>
dotnet run --project pg_protoexport -- plantuml <file.pcapng> packet <output.md>
dotnet run --project pg_protoexport -- html <file.pcapng> <output.html>
# ascii is a two-mode branch (capture_file comes before the mode keyword, like mermaid/plantuml).
# --console / --max-width are BRANCH options (declared on AsciiBranchSettings so they show in
# `ascii --help`), so they must be placed BEFORE the mode keyword, not after it.
dotnet run --project pg_protoexport -- ascii <file.pcapng> fields <output.txt>
dotnet run --project pg_protoexport -- ascii <file.pcapng> sequenceDiagram <output.txt>
dotnet run --project pg_protoexport -- ascii <file.pcapng> --console sequenceDiagram   # write to stdout, no file
dotnet run --project pg_protoexport -- ascii <file.pcapng> --console --max-width 120 fields

# Batch export: run every exporter + variant over every .pcapng in a directory.
# Produces <output-dir>/<input-stem>/capture.{tex,pqtrace.txt,ascii.txt,ascii.seq.txt,mermaid.{seq,pkt}.md,
# plantuml.{seq,pkt}.md,html} + capture_assets/ per input.
dotnet run --project pg_protoexport -- batchexport <input-dir>
dotnet run --project pg_protoexport -- batchexport docs/examples/captures docs/examples/exports
dotnet run --project pg_protoexport -- batchexport docs/examples/captures docs/examples/exports --port 5432 --recursive

# Print version (from Nerdbank.GitVersioning) + runtime info. `--version` / `-v` work too.
dotnet run --project pg_protoexport -- version

# Interactive guided tour: walks through every command (starting with ascii) and can run each
# against the bundled sample capture. Non-interactive / piped / --no-run prints the tour only.
dotnet run --project pg_protoexport -- demo
dotnet run --project pg_protoexport -- demo --no-run

# Live capture (writes a .pcapng from a live NIC; needs Npcap/libpcap)
dotnet run --project pg_protoexport -- capture <output.pcapng>
dotnet run --project pg_protoexport -- capture <output.pcapng> --host localhost --port 5432 --duration 30s
dotnet run --project pg_protoexport -- capture <output.pcapng> --quiet         # suppress per-packet console echo
dotnet run --project pg_protoexport -- capture --list-devices

# Pagila sample: capture + run pagila workload in one command
# Writes one .pcapng per scenario, including scenario 00 (startup handshake)
dotnet run --project pg_protoexport.samples.pagila -- capture-and-generate pagila.pcapng
```

## Architecture

The solution has eleven projects plus two samples:

- **pg_protoexport.core** - Library with parsing and domain models. No dependency on CLI, export formats, or SharpPcap. Also exposes `ProtocolStateProjector` / `ProtocolStateSnapshot` under `Sequence/` for folding a packet stream into per-message observable state (connection state, transaction status, prepared statements, portals, server parameters, backend PID) used by the HTML exporter. Defines the parse/capture contracts `IPcapService` and `IPcapPortDetector` (interfaces only — no SharpPcap in their signatures) and `IPcapExporter` (the format-agnostic export contract). Grants its internals to `pg_protoexport.capture` (and the test project) so the capture host can reach `internal` parser members.
- **pg_protoexport.capture** - SharpPcap-backed capture I/O, split out of core so the parsing/export libraries carry no SharpPcap (libpcap/Npcap) dependency. References `pg_protoexport.core` + `SharpPcap`. Hosts the read-side `PcapService` (implements `IPcapService`), `PcapPortDetector` (implements `IPcapPortDetector`, with `AddPcapPortDetector()`), the live-capture primitive `LiveCaptureSession` (and `ILiveCaptureSessionFactory`, `LiveCaptureSessionFactory`, `LiveCaptureOptions`, `PcapDevicePicker`), and the `AddPcapService()` / `AddLiveCapture()` DI extensions. Only the CLI, tests, and samples reference it; no exporter project does.
- **pg_protoexport.cli.abstractions** - Shared CLI contract referenced by every exporter project and the CLI host. References `pg_protoexport.core` + `Spectre.Console.Cli` (so core itself stays Spectre-free). Contains `IExporterCliModule` (each exporter contributes one to self-register its command(s) and batch variants), `BatchVariant`, the `ExportSettings` base class, and `ExportApp` / `IExportApp` (the dispatcher). This is the seam that keeps the `pg_protoexport` CLI project immune to new exporters.
- **pg_protoexport.export.latex** - LaTeX exporter with T4 templates, `IPcapToLatexService`, `LatexRenderOptions`, `LatexHelper`, and `ITextTransformer`.
- **pg_protoexport.export.pqtrace** - PQTrace text exporter with `IPcapToPqTraceService`.
- **pg_protoexport.export.mermaid** - Mermaid exporter with `IPcapToMermaidService`. Two modes: sequence diagrams and packet diagrams.
- **pg_protoexport.export.plantuml** - PlantUML exporter with `IPcapToPlantUmlService`. Same two modes as Mermaid; packet mode renders messages as `@startjson` trees because PlantUML has no native packet diagram type.
- **pg_protoexport.export.html** - HTML "guided reading" report exporter with `IPcapToHtmlService`. Depends on the Mermaid exporter (embeds its `sequenceDiagram` output).
- **pg_protoexport.export.ascii** - ASCII exporter with `IPcapToAsciiService`. Two modes (a multi-mode branch like mermaid/plantuml): `fields` renders each parsed field as a labelled, content-sized box (forces `PcapPostgresOptions.RecordFieldMetadata` on, like HTML, because the renderer needs per-field offsets/lengths), and `sequenceDiagram` renders a two-lifeline (Client/Server) ASCII conversation with one arrow per packet (reusing `PostgresPacketSequence.BuildSequenceLines` + `SessionEndpoints`). A `--console`/`-c` flag writes either mode to stdout instead of a file: the render is buffered and flushed in one shot after all log lines, and `ExportApp` skips its "Output path"/"File written" logs when the output path is empty so the diagram is the last thing on stdout. `--console` and `--max-width` are declared on `AsciiBranchSettings` (the branch settings type) rather than the leaf `AsciiSettings`, so they surface in `ascii --help`'s OPTIONS section; because Spectre parses branch options before the sub-command, they must be placed **before** the mode keyword (`ascii file.pcapng --console sequenceDiagram`).
- **pg_protoexport** - CLI executable using Spectre.Console.Cli. Generic host: registers the host-level `version`, `capture`, `batchexport`, and `demo` commands (and wires Spectre's `--version`/`-v` via `SetApplicationVersion`, sourced from `VersionInfo.Informational` which reads the Nerdbank.GitVersioning `AssemblyInformationalVersionAttribute`), then discovers every `IExporterCliModule` via DI and lets each register its own command(s). Each exporter project owns its CLI command + settings, so adding an exporter requires no edits here beyond one `Add{Format}Exporter()` composition line. Every exporter project carries a `Spectre.Console.Cli` dependency as a result. Every command/branch/sub-command carries a `WithDescription` (branches set theirs via the inner `IConfigurator<T>.SetDescription` since `AddBranch` only returns a `WithAlias`-capable configurator). `DemoCommand` (`Cli/Commands/DemoCommand.cs`) is an interactive guided tour that injects `IAnsiConsole` (registered in DI via `AddSingleton(console)`) and `IExportApp`: it walks every command starting with `ascii`, prints the exact CLI line, and — in an interactive terminal — runs each against the bundled `SampleData/extendedQuery.pcapng` (ascii renders to the console via `--console`; other exporters write to a temp file previewed inline). `--no-run` or a non-interactive/piped stdin prints the tour without prompting or executing. The interactive Spectre API (`SelectionPrompt`/`Confirm`/`TextPrompt`/`Panel`/`Rule`) comes transitively from `Spectre.Console.Cli` → `Spectre.Console`.
- **pg_protoexport.tests** - xUnit v2 tests with NSubstitute for mocking and Spectre.Console.Testing.
- **BasicSample** - Minimal console app showing how to use core + exporters without DI. Single verb `read`: parses a bundled `extendedQuery.pcapng` and renders LaTeX/Mermaid/PlantUML output via the legacy `MarkdownMermaidGenerator` / `MarkdownPlantUmlGenerator` helpers. No live database, no Npgsql.
- **pg_protoexport.samples.pagila** - Console app that drives a representative pagila workload (13 PostgreSQL wire-protocol scenarios in `PagilaTrafficGenerator`) against a local pagila DB. Two verbs: `generate` (run the workload only) and `capture-and-generate` (wrap each scenario in its own `LiveCaptureSession`, producing one `.pcapng` per scenario plus scenario 00 for the startup handshake). `PagilaTrafficGenerator.RunAsync` accepts an optional `Func<int, string, Func<Task>, Task>? scenarioWrapper` so the orchestrator (`Program.RunCaptureAndGenerateAsync`) can interpose the per-scenario capture lifecycle; the default wrapper is a no-op for callers who just want the workload to run.

### Pipeline

1. **PcapService** (implements `IPcapService`; lives in `pg_protoexport.capture`, the interface is in core) - Reads capture files via SharpPcap, parses TCP payloads into `IEnumerable<PostgresPacket>`. Each packet contains a list of `PostgresMessageBase`-derived message objects matching the PostgreSQL message formats spec. Message parsing is dispatched via a `switch` on message name in `PcapService.TryReadMessage`. Handles TCP reassembly (leftover buffer from previous packet).

   **Non-TLV startup-phase dispatch.** PostgreSQL's startup-phase frames (`StartupMessage`, `SSLRequest`, `GSSENCRequest`, `CancelRequest`) carry no 1-byte type prefix and are discriminated by reading the int32 request code at offset 4 (after the length field). `PcapService.DispatchStartupPhaseFrontend` switches on that code (`SSLRequestMessage.MagicCode = 80877103`, `GSSENCRequestMessage.MagicCode = 80877104`, `CancelRequestMessage.MagicCode = 80877102`, anything else with `length > 8` is a `StartupMessageMessage`). Unknown codes fall back to `UnknownMessage` with a warning logged.

   **Backend single-byte probe replies.** After the client sends an SSL/GSSENC probe, the server replies with a single ASCII byte (`'S'`/`'N'` for SSL, `'G'`/`'N'` for GSSENC). `PcapReadState` tracks the last probe kind per client port via a `StartupProbeKind` enum; `PcapService.TryReadMessage` intercepts the byte before normal catalog lookup and routes it to `SSLResponseMessage` / `GSSENCResponseMessage` with an `Accepted` flag. If the probe state is set but the byte doesn't match the expected reply characters (truncated capture), the flag is cleared with a warning and normal dispatch resumes.

   **CancelRequest correlation.** When `BackendKeyDataMessage` is observed, `PcapReadState.RegisterCancelKey` records `(pid, secret) → clientPort`. When a `CancelRequestMessage` arrives (on a different TCP connection), its `CorrelatedClientPort` is populated from `LookupCancelTargetClientPort`. The HTML exporter resolves that port to the first card of the targeted session and renders a "Jump to query session" link.

   **Copy sub-protocol.** The six COPY messages have dedicated model types: `CopyInResponseMessage` (`'G'`), `CopyOutResponseMessage` (`'H'`), `CopyBothResponseMessage` (`'W'`), `CopyDataMessage` (`'d'`, bidirectional), `CopyDoneMessage` (`'c'`, bidirectional), `CopyFailMessage` (`'f'`, frontend only). The three response types share an abstract `CopyResponseBase` with `OverallFormat` (0=text, 1=binary) and `List<short> ColumnFormats`. `CopyDataMessage` stores only the first 64 bytes of payload in `PreviewBytes` (with the full payload size in `DataLength`) to keep memory and rendered output bounded for MB-sized replication chunks. Example captures: [docs/examples/captures/pagila-11-copy-out-binary.pcapng](docs/examples/captures/pagila-11-copy-out-binary.pcapng) (`COPY ... TO STDOUT`) and [pagila-12-copy-in-binary.pcapng](docs/examples/captures/pagila-12-copy-in-binary.pcapng) (`COPY ... FROM STDIN`).

   **Auto-detection of the PostgreSQL port.** `IPcapPortDetector` (registered via `AddPcapPortDetector()`) reads each capture's TCP headers and returns the server port — either authoritatively from the first SYN-only packet, or by packet-count majority for mid-conversation captures. The CLI's `--port` option is now optional; `ExportApp.RunExport` and `BatchExportCommand` call the detector when no port is supplied.

   **LiveCaptureSession** (`pg_protoexport.capture/Services/LiveCaptureSession.cs`) is the write-side counterpart: an `IAsyncDisposable` handle returned by `StartAsync(LiveCaptureOptions, ILoggerFactory?, CancellationToken)`. It uses `PcapDevicePicker` to resolve a NIC from `LiveCaptureOptions.Host` (loopback IPs → loopback adapter; remote IPs → the interface that routes to that host via a UDP-socket trick; explicit `DeviceName` always wins), opens it with BPF filter `tcp port {Port}`, and pipes `OnPacketArrival` into a `CaptureFileWriterDevice` opened with the device's actual `LinkType`. When `LiveCaptureOptions.EchoPackets` is set, each captured frame is also logged in the same one-line format `PcapService` uses for file reads. `DisposeAsync` stops capture, drains briefly, and closes both writer and device — so an `await using` block guarantees a flushed `.pcapng` even if the workload throws. `ILiveCaptureSessionFactory` is the DI seam that captures `ILoggerFactory`; registered by `AddLiveCapture()`. `PcapDevicePicker.Enumerate()` powers the CLI's `--list-devices`.

   **Timing safeguards in LiveCaptureSession.** Two delays are baked in so short captures don't silently lose packets. (1) `StartAsync` waits `StartupSettleMs` (150ms) after `device.StartCapture()` returns, because SharpPcap's `StartCapture` returns before the background capture thread has actually entered `pcap_loop` — for sessions where the workload completes in <100ms (e.g. a single `SELECT count(*)`), the first packets fly past before the thread is ready. (2) `DisposeAsync` drains for `min(ReadTimeoutMs + 50, 750)`ms *before* calling `StopCapture`, because libpcap batches packets in a kernel buffer and only delivers them to `OnPacketArrival` when the buffer fills OR `ReadTimeoutMs` expires; stopping the capture while packets are still buffered silently drops them. The cap at 750ms keeps dispose responsive even if `ReadTimeoutMs` is misconfigured high.

2. **Exporters** - Each exporter consumes `IEnumerable<PostgresPacket>` and produces output in its own format:
   - **PcapToLatexService** (`IPcapToLatexService` in export.latex) - Maps each `PostgresMessageBase` subclass to an `ITextTransformer` (T4 template) via `FindTextTransformer` pattern match. In article mode, uses `ITextTransformer.EstimateBytefieldRowCount()` to track vertical space and insert page breaks. Accepts a `LatexRenderOptions` record per call that toggles between "nice" layout and byte-exact layout (`Exact`, `RowWidthBytes`); the options flow through `GenerationState.Render` to every template's `Render` property.
   - **PcapToPqTraceService** (`IPcapToPqTraceService` in export.pqtrace) - Writes tab-separated text output.
   - **PcapToMermaidService** (`IPcapToMermaidService` in export.mermaid) - Two methods: `PcapToSequenceDiagram` (one diagram per session, split at `ReadyForQuery`/`Terminate`, merges consecutive same-direction single-count messages with `/`) and `PcapToPacketDiagram` (one Mermaid `packet` block per grouped message).
   - **PcapToPlantUmlService** (`IPcapToPlantUmlService` in export.plantuml) - Same two-method shape as Mermaid; sequence mode uses identical session/merge logic. Packet mode emits `@startjson` trees with `"field": "value (N bytes)"` entries and JSON arrays for repeating structures (DataRow columns, RowDescription field descriptors, Parse OIDs, Bind parameter values, StartupMessage parameters, FieldList entries).
   - **PcapToHtmlService** (`IPcapToHtmlService` in export.html) - Builds one self-contained HTML report. Pipeline: call `IPcapToMermaidService.PcapToSequenceDiagram` and extract every \`\`\`mermaid fenced block, then run `ProtocolStateProjector.Project` to emit one `HtmlMessageCard` per message containing direction, code, length, a plain-English headline (`BuildHeadline` switch on the message type), the `ParsedFields` (offset/length/display) needed for hover-to-highlight byte tinting, an authored rationale string keyed off the message name (with `AuthenticationGenericMessage.AuthenticationName` as a more specific fallback), and the `ProtocolStateSnapshot` *after* this message. Cards + diagrams + glossary + rationales are JSON-serialized and substituted into the embedded `Assets/template.html` (`{{TITLE}}`, `{{INLINE_DATA}}`). Writes the HTML next to a `<output>_assets/` directory containing `styles.css`, `app.js`, and a vendored `mermaid.min.js` — all from `EmbeddedResource` files in the project. `glossary.json` and `rationales.json` are also embedded; both can be hand-edited and rebuilt to update copy without touching code. The exporter requires per-field metadata (offset/length/display), so `AddHtmlExporter()` runs a `PostConfigure<PcapPostgresOptions>` that sets `RecordFieldMetadata = true`.

3. **CLI dispatcher (`ExportApp` / `IExportApp` in `pg_protoexport.cli.abstractions`)** - Thin coordinator around `IPcapService`, `IPcapPortDetector`, and `IEnumerable<IPcapExporter>`. Lives in the shared abstractions project (not the CLI exe) so exporter command classes can depend on it. Two entry points: `RunExport(name, inputFile, outputPath, port, mode?, options?)` parses the pcapng (auto-detecting the port via `IPcapPortDetector` when `port` is null) and dispatches to the named exporter; `RunExportPrebuilt(packets, name, outputPath, mode?, options?)` skips parsing and is used by `BatchExportCommand` so one parsed capture can fan out to all variants without paying parse cost N times. Both go through a shared `RunExportInternal` that runs the exporter inside a try/catch/finally and logs packet/message counters from `IExportResult`. The CLI exe registers it as `AddSingleton<IExportApp, ExportApp>()`.

4. **`batchexport` command** - Top-level Spectre command in [pg_protoexport/Cli/Commands/BatchExportCommand.cs](pg_protoexport/Cli/Commands/BatchExportCommand.cs) that walks `<input-dir>` for `.pcapng`/`.pcap` (recursive optional), parses each file once, then writes a per-input subfolder `<output-dir>/<stem>/` containing one output per exporter variant: `capture.tex` (latex standalone), `capture.pqtrace.txt`, `capture.ascii.txt` (fields), `capture.ascii.seq.txt` (sequence diagram), `capture.mermaid.{seq,pkt}.md`, `capture.plantuml.{seq,pkt}.md`, `capture.html` (+ `capture_assets/`). The variant list is **not** declared in this command — it is aggregated at construction from every registered exporter's `IExporterCliModule.BatchVariants` (`modules.SelectMany(m => m.BatchVariants)`), so a new exporter's variants appear automatically. The port can be supplied via `--port`; if omitted, `IPcapPortDetector` infers it per file. Failures during parse or any single variant are caught, logged, and reported in the final `OK/Total` summary; one bad file does not abort the run, but the process exits non-zero if any file falls short.

### T4 templates

LaTeX output for each message type is generated by T4 text templates in `pg_protoexport.export.latex/Templates/`. The `.tt` files are preprocessed (TextTemplatingFilePreprocessor) into `.cs` files at design time. Each template class implements `ITextTransformer`. When modifying template output, edit the `.tt` file and regenerate (Visual Studio or `dotnet-t4` CLI).

Each template branches on its `Render` property (a `LatexRenderOptions`): when `Render.Exact` is false the template emits the original compact layout; when true it dispatches to helpers in `LatexHelper` (`pg_protoexport.export.latex/LatexHelper.cs`) that emit `\bitbox{N}` with `N` equal to the on-the-wire byte count, wrap long content across `bytefield` rows of width `Render.RowWidthBytes`, and LaTeX-escape literal UTF-8 strings (no truncation). The per-template params records under `Templates/Model/` expose `Render` to the template.

### Extensibility points

Services are extensible via options configured through DI:

- `PcapPostgresOptions.MessageCatalog` - Register custom front-end/back-end message definitions (`AddPcapService()`)
- `PcapPostgresOptions.CustomMessageProcessor` - Handle parsing of custom messages (`AddPcapService()`)
- `PcapPostgresOptions.RecordFieldMetadata` - When `true`, the parser records `(name, offset, length)` for each field it reads, exposed on `PostgresMessageBase.ParsedFields`. Off by default to keep existing exporters byte-identical and avoid per-field allocations. `AddHtmlExporter()` turns it on automatically.
- `PcapToLatexOptions.CustomTemplateProvider` - Provide custom LaTeX templates for any message type (`AddLatexExporter()`)
- `PcapToLatexOptions.CustomHeaderProvider` - Replace the LaTeX document header (`AddLatexExporter()`)
- `PcapToLatexOptions.DefaultExact` / `DefaultRowWidthBytes` - Default `LatexRenderOptions` applied when callers do not pass their own; CLI flags override these.

### Adding a new exporter

Everything for a new exporter lives in its own project; the `pg_protoexport` CLI is touched only by one composition line.

1. Create a new project (e.g. `pg_protoexport.export.foo`) referencing `pg_protoexport.core` **and** `pg_protoexport.cli.abstractions` (the latter pulls in `Spectre.Console.Cli`).
2. Create a service interface and implementation consuming `IEnumerable<PostgresPacket>` and implementing `IPcapExporter` (`Name`, `DefaultExtension`, `Export(packets, outputPath, mode, options)`).
3. Add the CLI surface **in the exporter project**: a `FooSettings : ExportSettings`, a command class (depending on `IExportApp`), and a `FooCliModule : IExporterCliModule` whose `Register` adds the command (top-level for single-mode, or `config.AddBranch<ExportSettings>(...)` with sub-commands for multi-mode like mermaid/plantuml) and whose `BatchVariants` lists its `batchexport` outputs.
4. Create an `AddFooExporter()` `IServiceCollection` extension that registers the service as both `IPcapToFooService` and `IPcapExporter`, plus `AddSingleton<IExporterCliModule, FooCliModule>()`.
5. In the CLI's `Program.BuildServiceCollection`, add the one `.AddFooExporter()` line and a `ProjectReference` to the new project. Nothing else in the CLI changes — `ConfigurePgProtoExport` discovers the module and `BatchExportCommand` picks up its variants automatically.

See `pg_protoexport.export.mermaid` and `pg_protoexport.export.ascii` (multi-mode branches) and `pg_protoexport.export.pqtrace` (single-mode) for the cleanest examples. BasicSample's `MarkdownMermaidGenerator` and `MarkdownPlantUmlGenerator` are pre-exporter sample helpers kept for the no-DI demo — they are not the canonical pattern.

### DI and factory patterns

The project uses Microsoft.Extensions.DependencyInjection with the Options pattern. DI registration is composable: `AddPcapService()` (capture) + `AddPcapPortDetector()` (capture, registers `IPcapPortDetector`) + `AddLiveCapture()` (capture, registers `ILiveCaptureSessionFactory`) + `AddLatexExporter()` (latex) + `AddPqTraceExporter()` (pqtrace) + `AddMermaidExporter()` + `AddPlantUmlExporter()` + `AddHtmlExporter()` + `AddAsciiExporter()`. Each `Add{Format}Exporter()` registers the exporter as both its typed `IPcapTo{Format}Service` and `IPcapExporter`, plus its `IExporterCliModule` (so the CLI host can discover and self-register the exporter's command/variants). `AddHtmlExporter()` depends on `AddMermaidExporter()` having been registered and also flips `PcapPostgresOptions.RecordFieldMetadata` on via `PostConfigure` (as does `AddAsciiExporter()`). `PcapService`, `PcapPortDetector`, `PcapToLatexService`, `PcapToMermaidService`, `PcapToPlantUmlService`, `PcapToHtmlService`, and `PcapToAsciiService` all expose static `Create()` factory methods for use without DI (as shown in BasicSample); `LiveCaptureSession.StartAsync(options, loggerFactory)` plays the same role for live capture.

## Key conventions

- All types use a flat namespace: `namespace pg_protoexport;` (file-scoped), even across assemblies.
- NuGet versions are centrally managed in `Directory.Packages.props` (CPM with transitive pinning).
- Target framework is .NET 10.
- `pg_protoexport.core` and exporter projects expose internals to the test project via `InternalsVisibleTo`.
- SharpPcap requires a pcap library at runtime (libpcap on Linux/Mac, Npcap on Windows).
