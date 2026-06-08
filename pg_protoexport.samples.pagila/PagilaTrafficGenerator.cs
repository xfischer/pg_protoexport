using System.Data;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace pg_protoexport;

// Exercises a representative cross-section of the PostgreSQL v3 wire protocol against
// the pagila demo database (https://github.com/xzilla/pagila), in increasing order of
// complexity.
//
// Run a packet capture on port 5432 (tcpdump / Wireshark + Npcap) while this program
// executes, then feed the resulting .pcapng to pg_protoexport to see every message
// decoded.
//
// Not reachable through stock Npgsql 10 (and therefore intentionally omitted):
//   Flush                  — Npgsql always uses Sync.
//   FunctionCall / *Response — deprecated, no Npgsql API.
//   Cleartext / MD5 password — requires changing pg_hba.conf off SCRAM.
//   GSS / SSPI auth        — requires Kerberos infrastructure.
//   NegotiateProtocolVersion — requires speaking an unknown protocol option.
//   CopyBothResponse       — physical/logical replication only.
internal static class PagilaTrafficGenerator
{
    private static ILogger _log = null!;
    private static Func<int, string, Func<Task>, Task> _scenarioWrapper = null!;

    public static async Task<int> RunAsync(
        ILogger logger,
        CancellationToken ct,
        Func<int, string, Func<Task>, Task>? scenarioWrapper = null)
    {
        _log = logger;
        _scenarioWrapper = scenarioWrapper ?? ((_, _, body) => body());

        var csb = BuildConnectionString();
        _log.LogInformation("Connecting to {Host}:{Port}/{Database} as {User}",
            csb.Host, csb.Port, csb.Database, csb.Username);

        await using var conn = new NpgsqlConnection(csb.ConnectionString);
        conn.Notice += (_, e) => _log.LogInformation(
            "<-- NoticeResponse: [{Severity}] {Message}",
            e.Notice.Severity, e.Notice.MessageText);
        conn.Notification += (_, e) => _log.LogInformation(
            "<-- NotificationResponse: channel='{Channel}' payload='{Payload}' pid={Pid}",
            e.Channel, e.Payload, e.PID);

        bool connected = false;
        await ScenarioRaw(0, "startup handshake (Startup, Authentication, ParameterStatus, ReadyForQuery)", async () =>
        {
            await conn.OpenAsync(ct);
            connected = true;
        });

        if (!connected)
        {
            _log.LogError(
                "Failed to connect. Set PGHOST/PGPORT/PGUSER/PGPASSWORD/PGDATABASE and " +
                "load the pagila schema first (https://github.com/xzilla/pagila).");
            return 1;
        }

        await Scenario(1, "simple query, single statement", c => Scenario01_SimpleQuery(c), conn);
        await Scenario(2, "empty query (EmptyQueryResponse)", c => Scenario02_EmptyQuery(c), conn);
        await Scenario(3, "simple query, batched statements", c => Scenario03_SimpleQueryBatched(c), conn);
        await Scenario(4, "extended query, unnamed statement", c => Scenario04_ExtendedUnnamed(c), conn);
        await Scenario(5, "extended query, named prepared statement", c => Scenario05_PreparedNamed(c), conn);
        await Scenario(6, "prepared non-returning statement (NoData)", c => Scenario06_PreparedNoData(c), conn);
        await Scenario(7, "Execute with MaxRows > 0 (PortalSuspended)", c => Scenario07_MaxRows(c), conn);
        await Scenario(8, "NoticeResponse via DO / RAISE NOTICE", c => Scenario08_NoticeResponse(c), conn);
        await Scenario(9, "ErrorResponse via bad SQL", c => Scenario09_ErrorResponse(c), conn);
        await Scenario(10, "LISTEN / NOTIFY (NotificationResponse)", c => Scenario10_ListenNotify(c, csb, ct), conn);
        await Scenario(11, "COPY OUT (binary)", c => Scenario11_CopyOut(c, ct), conn);
        await Scenario(12, "COPY IN (binary)",
            c => Scenario12_CopyIn(c, ct), conn,
            setup:    c => Scenario12_CopyIn_Setup(c, ct),
            teardown: c => Scenario12_CopyIn_Verify(c, ct));
        await Scenario(13, "CancelRequest", c => Scenario13_CancelRequest(c, ct), conn);

        // Disposing the connection emits Terminate (X).
        return 0;
    }

    // Pooling=false and MaxAutoPrepare=0 are load-bearing: pooling would hide the startup
    // handshake on subsequent runs, and auto-prepare would silently turn scenarios 1-3
    // into extended-protocol traffic, defeating the point of the simple-query examples.
    internal static NpgsqlConnectionStringBuilder BuildConnectionString()
    {
        var csb = new NpgsqlConnectionStringBuilder
        {
            Host           = Environment.GetEnvironmentVariable("PGHOST")     ?? "localhost",
            Port           = int.TryParse(Environment.GetEnvironmentVariable("PGPORT"), out var p) ? p : 5432,
            Username       = Environment.GetEnvironmentVariable("PGUSER")     ?? "postgres",
            Password       = Environment.GetEnvironmentVariable("PGPASSWORD") ?? "postgres",
            Database       = Environment.GetEnvironmentVariable("PGDATABASE") ?? "pagila",
            Pooling = false,
            MaxAutoPrepare = 0,
        };
        csb.SslMode = Enum.TryParse<SslMode>(Environment.GetEnvironmentVariable("PGSSLMODE"), true, out var sslMode)
            ? sslMode
            : SslMode.Disable;
        return csb;
    }

    private static Task Scenario(
        int index, string title,
        Func<NpgsqlConnection, Task> body,
        NpgsqlConnection conn,
        Func<NpgsqlConnection, Task>? setup = null,
        Func<NpgsqlConnection, Task>? teardown = null)
        => ScenarioRaw(index, title, () => body(conn),
            setup: setup is null ? null : () => setup(conn),
            teardown: teardown is null ? null : () => teardown(conn));

    // setup and teardown run OUTSIDE _scenarioWrapper, so under capture-and-generate
    // their traffic is not written to the per-scenario .pcapng. Use them for
    // prerequisites and verification that aren't part of the wire-protocol demo.
    private static async Task ScenarioRaw(
        int index, string title,
        Func<Task> body,
        Func<Task>? setup = null,
        Func<Task>? teardown = null)
    {
        if (setup is not null)
            await setup();

        await _scenarioWrapper(index, title, async () =>
        {
            _log.LogInformation("=== {Index:D2}. {Title} ===", index, title);
            try
            {
                await body();
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "scenario '{Title}' failed unexpectedly", title);
            }
        });

        if (teardown is not null)
            await teardown();
    }

    // ------------------------------------------------------------------------
    // 1. Simple query, single statement.
    //    No parameters, no Prepare => Npgsql sends a single Q (Query) message.
    //    Wire: Q  →  T (RowDescription), D (DataRow), C (CommandComplete), Z (ReadyForQuery).
    // ------------------------------------------------------------------------
    private static async Task Scenario01_SimpleQuery(NpgsqlConnection conn)
    {
        await using var cmd = new NpgsqlCommand("SELECT count(*) FROM actor", conn);
        var n = await cmd.ExecuteScalarAsync();
        _log.LogInformation("  actor count = {N}", n);
    }

    // ------------------------------------------------------------------------
    // 2. Empty query.
    //    A Query message whose text is empty (here: just a semicolon) produces
    //    EmptyQueryResponse (I) instead of CommandComplete.
    //    Wire: Q  →  I (EmptyQueryResponse), Z.
    // ------------------------------------------------------------------------
    private static async Task Scenario02_EmptyQuery(NpgsqlConnection conn)
    {
        await using var cmd = new NpgsqlCommand(";", conn);
        await cmd.ExecuteNonQueryAsync();
    }

    // ------------------------------------------------------------------------
    // 3. Simple query, batched statements.
    //    Multi-statement CommandText, no parameters => Npgsql still uses simple
    //    protocol, sending ONE Q with all three statements.
    //    Wire: Q  →  T/D/C per statement, then a single Z at the end.
    //    Not working with Npgsql as only extended queries are used
    // ------------------------------------------------------------------------
    private static async Task Scenario03_SimpleQueryBatched(NpgsqlConnection conn)
    {
        const string sql = """
            SELECT 'hello' AS greeting;
            SELECT now()   AS server_time;
            SELECT count(*) FROM film;
            """;
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var rdr = await cmd.ExecuteReaderAsync();
        int rs = 0;
        do
        {
            rs++;
            while (await rdr.ReadAsync())
                _log.LogInformation("  result {Rs}: {Value}", rs, rdr.GetValue(0));
        } while (await rdr.NextResultAsync());
    }

    // ------------------------------------------------------------------------
    // 4. Extended query, unnamed statement.
    //    Adding a parameter ($1) forces the extended protocol. Without Prepare,
    //    Npgsql uses empty names for the statement AND the portal.
    //    Wire: P(name="") → B(portal="", stmt="") → D('P', "") → E(MaxRows=0) → S
    //         →  1 (ParseComplete), 2 (BindComplete), T, D, C, Z.
    // ------------------------------------------------------------------------
    private static async Task Scenario04_ExtendedUnnamed(NpgsqlConnection conn)
    {
        await using var cmd = new NpgsqlCommand(
            "SELECT title, release_year FROM film WHERE film_id = $1", conn);
        cmd.Parameters.AddWithValue(42);
        await using var rdr = await cmd.ExecuteReaderAsync();
        while (await rdr.ReadAsync())
            _log.LogInformation("  film 42 = '{Title}' ({Year})",
                rdr.GetString(0), rdr.IsDBNull(1) ? (object)"<null>" : rdr.GetValue(1));
    }

    // ------------------------------------------------------------------------
    // 5. Extended query, named prepared statement.
    //    PrepareAsync allocates a server-side statement (Npgsql picks the name,
    //    typically "_p0"). Subsequent Executes reuse the plan.
    //    Wire during PrepareAsync:
    //        P(name="_p0") → D('S', "_p0") → S
    //          → 1, t (ParameterDescription), T (RowDescription), Z.
    //    Wire per Execute:
    //        B → E(MaxRows=0) → S  →  2, D, C, Z.
    // ------------------------------------------------------------------------
    private static async Task Scenario05_PreparedNamed(NpgsqlConnection conn)
    {
        await using var cmd = new NpgsqlCommand(
            "SELECT title FROM film WHERE film_id = $1", conn);
        var idParam = cmd.Parameters.AddWithValue(1);
        await cmd.PrepareAsync();

        foreach (int id in new[] { 1, 7, 13 })
        {
            idParam.Value = id;
            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
                _log.LogInformation("  film {Id} = '{Title}'", id, rdr.GetString(0));
        }
    }

    // ------------------------------------------------------------------------
    // 6. Prepared statement that returns no rows.
    //    UPDATE has no result columns, so Describe(Statement) replies with NoData
    //    instead of RowDescription. This is the only realistic way to surface
    //    NoData via a typed client.
    //    Wire during PrepareAsync:
    //        P → D('S', name) → S  →  1, t (ParameterDescription), n (NoData), Z.
    //    Wire on Execute:
    //        B → E(0) → S  →  2, C("UPDATE N"), Z.
    // ------------------------------------------------------------------------
    private static async Task Scenario06_PreparedNoData(NpgsqlConnection conn)
    {
        await using var cmd = new NpgsqlCommand(
            "UPDATE actor SET last_update = last_update WHERE actor_id = @id", conn);
        cmd.Parameters.Add(new NpgsqlParameter<int>("id", 1));
        await cmd.PrepareAsync();
        var affected = await cmd.ExecuteNonQueryAsync();
        _log.LogInformation("  rows affected = {Affected}", affected);
    }

    // ------------------------------------------------------------------------
    // 7. Execute with MaxRows > 0 (PortalSuspended).
    //    CommandBehavior.SingleRow makes Npgsql send Execute with MaxRows=1.
    //    Because the query has more rows pending, the server replies with one
    //    DataRow then PortalSuspended (s) instead of CommandComplete. This is
    //    what makes the pg_protoexport HTML exporter render "(max 1 rows)".
    //    Wire: P → B → D('P', "") → E(MaxRows=1) → S
    //          → 1, 2, T, D (one row), s (PortalSuspended), Z.
    // ------------------------------------------------------------------------
    private static async Task Scenario07_MaxRows(NpgsqlConnection conn)
    {
        await using var cmd = new NpgsqlCommand(
            "SELECT film_id, title FROM film ORDER BY film_id", conn);
        await using var rdr = await cmd.ExecuteReaderAsync(CommandBehavior.SingleRow);
        while (await rdr.ReadAsync())
            _log.LogInformation("  first row: {Id} = '{Title}'", rdr.GetInt32(0), rdr.GetString(1));
    }

    // ------------------------------------------------------------------------
    // 8. NoticeResponse via DO / RAISE NOTICE.
    //    A DO block that raises a notice causes the server to emit NoticeResponse
    //    (N) between DataRow/CommandComplete. Surfaced via NpgsqlConnection.Notice.
    //    Wire: Q  →  N (NoticeResponse), C, Z.
    // ------------------------------------------------------------------------
    private static async Task Scenario08_NoticeResponse(NpgsqlConnection conn)
    {
        await using var cmd = new NpgsqlCommand(
            "DO $$ BEGIN RAISE NOTICE 'hello from pagila'; END $$;", conn);
        await cmd.ExecuteNonQueryAsync();
    }

    // ------------------------------------------------------------------------
    // 9. ErrorResponse via bad SQL.
    //    Reference a non-existent table; the server replies with ErrorResponse (E)
    //    and then ReadyForQuery. Npgsql translates this to PostgresException.
    //    Wire: Q  →  E (ErrorResponse), Z.
    // ------------------------------------------------------------------------
    private static async Task Scenario09_ErrorResponse(NpgsqlConnection conn)
    {
        try
        {
            await using var cmd = new NpgsqlCommand("SELECT * FROM not_a_table_at_all", conn);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (PostgresException ex)
        {
            _log.LogInformation("  caught expected error: {SqlState} {Message}",
                ex.SqlState, ex.MessageText);
        }
    }

    // ------------------------------------------------------------------------
    // 10. LISTEN / NOTIFY (NotificationResponse).
    //     Connection A listens; a second short-lived connection B sends NOTIFY.
    //     A's next round-trip causes the server to deliver the queued
    //     NotificationResponse (A — confusing naming, same letter as the
    //     authentication-request message but in a different context).
    //     Wire on connection A: LISTEN via simple query, then any later
    //     interaction sees the asynchronous A message before its own Z.
    //     Wire on connection B: full Startup → Auth → ... → NOTIFY (simple Q)
    //     → Terminate. Distinct TCP flow.
    // ------------------------------------------------------------------------
    private static async Task Scenario10_ListenNotify(
        NpgsqlConnection conn, NpgsqlConnectionStringBuilder csb, CancellationToken ct)
    {
        await using (var listen = new NpgsqlCommand("LISTEN demo_channel", conn))
            await listen.ExecuteNonQueryAsync(ct);

        await using (var notifier = new NpgsqlConnection(csb.ConnectionString))
        {
            await notifier.OpenAsync(ct);
            await using var notify = new NpgsqlCommand(
                "NOTIFY demo_channel, 'hello-from-other-conn'", notifier);
            await notify.ExecuteNonQueryAsync(ct);
        }

        // Give the broadcast a moment to land, then issue any cheap query so the
        // server delivers the pending notification before the next ReadyForQuery.
        await Task.Delay(200, ct);
        await using (var ping = new NpgsqlCommand("SELECT 1", conn))
            await ping.ExecuteScalarAsync(ct);

        await using (var unlisten = new NpgsqlCommand("UNLISTEN demo_channel", conn))
            await unlisten.ExecuteNonQueryAsync(ct);
    }

    // ------------------------------------------------------------------------
    // 11. COPY OUT (binary).
    //     COPY ... TO STDOUT puts the server into COPY-out mode.
    //     Wire: simple Q with the COPY statement  →
    //           H (CopyOutResponse), d (CopyData) × N rows, c (CopyDone), C, Z.
    //     pg_protoexport's core parser does not yet decode H/d/c — the bytes
    //     will appear as raw / undecoded in the export but are still present.
    // ------------------------------------------------------------------------
    private static async Task Scenario11_CopyOut(NpgsqlConnection conn, CancellationToken ct)
    {
        await using var stream = await conn.BeginRawBinaryCopyAsync(
            "COPY (SELECT actor_id, first_name FROM actor ORDER BY actor_id LIMIT 5) " +
            "TO STDOUT (FORMAT BINARY)",
            ct);
        var buffer = new byte[8192];
        long total = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer, ct)) > 0)
            total += read;
        _log.LogInformation("  copy-out streamed {Bytes} bytes", total);
    }

    // ------------------------------------------------------------------------
    // 12. COPY IN (binary).
    //     COPY ... FROM STDIN puts the server into COPY-in mode, then the
    //     client streams CopyData/CopyDone.
    //     Wire: simple Q with the COPY statement  →  G (CopyInResponse).
    //           Client: d (CopyData) × N rows, c (CopyDone).
    //           Server: C ("COPY N"), Z.
    //     The temp-table prerequisite and the post-copy row-count verification
    //     run via Scenario(..., setup:, teardown:) so they fall OUTSIDE the
    //     per-scenario capture window and don't pollute the .pcapng.
    // ------------------------------------------------------------------------
    private static async Task Scenario12_CopyIn_Setup(NpgsqlConnection conn, CancellationToken ct)
    {
        await using var setup = new NpgsqlCommand(
            "CREATE TEMP TABLE IF NOT EXISTS tmp_demo (id int, name text)", conn);
        await setup.ExecuteNonQueryAsync(ct);
    }

    private static async Task Scenario12_CopyIn(NpgsqlConnection conn, CancellationToken ct)
    {
        await using var importer = await conn.BeginBinaryImportAsync(
            "COPY tmp_demo (id, name) FROM STDIN (FORMAT BINARY)", ct);
        for (int i = 1; i <= 3; i++)
        {
            await importer.StartRowAsync(ct);
            await importer.WriteAsync(i, ct);
            await importer.WriteAsync($"name-{i}", ct);
        }
        await importer.CompleteAsync(ct);
    }

    private static async Task Scenario12_CopyIn_Verify(NpgsqlConnection conn, CancellationToken ct)
    {
        await using var verify = new NpgsqlCommand("SELECT count(*) FROM tmp_demo", conn);
        var n = await verify.ExecuteScalarAsync(ct);
        _log.LogInformation("  imported rows = {N}", n);
    }

    // ------------------------------------------------------------------------
    // 13. CancelRequest.
    //     A long-running query is interrupted with NpgsqlCommand.Cancel(), which
    //     opens a SEPARATE short-lived TCP connection, sends only a
    //     CancelRequest (a startup-shaped message with the cancel-code 80877102
    //     plus the target backend's process id and secret key), then closes.
    //     Two flows appear in the capture: the main flow ends with
    //     E (ErrorResponse, SQLSTATE 57014) + Z; the secondary flow contains
    //     just the cancel handshake.
    // ------------------------------------------------------------------------
    private static async Task Scenario13_CancelRequest(NpgsqlConnection conn, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand("SELECT pg_sleep(2)", conn);
        var queryTask = cmd.ExecuteNonQueryAsync(ct);

        await Task.Delay(300, ct);
        cmd.Cancel();

        try
        {
            await queryTask;
            _log.LogWarning("  query was NOT cancelled (server too fast?)");
        }
        catch (PostgresException ex) when (ex.SqlState == "57014")
        {
            _log.LogInformation("  caught expected cancellation: {Message}", ex.MessageText);
        }
        catch (OperationCanceledException)
        {
            _log.LogInformation("  query cancelled (OperationCanceledException)");
        }
    }
}
