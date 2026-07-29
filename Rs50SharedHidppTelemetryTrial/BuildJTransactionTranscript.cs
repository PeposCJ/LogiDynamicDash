using System.Globalization;
using System.Text.Json;
using LogiDynamicDash.Hidpp;
using Rs50SharedHidppTransport;

namespace Rs50SharedHidppTelemetryTrial;

internal sealed class BuildJRecordedExchange
    : IRs50HidppDisplayExchange
{
    private readonly IRs50HidppDisplayExchange inner;
    private readonly BuildJTransactionTranscript transcript;
    private readonly TimeProvider timeProvider;
    private bool disposed;

    internal BuildJRecordedExchange(
        IRs50HidppDisplayExchange inner,
        BuildJTransactionTranscript transcript,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(transcript);
        ArgumentNullException.ThrowIfNull(timeProvider);

        this.inner = inner;
        this.transcript = transcript;
        this.timeProvider = timeProvider;
    }

    internal static BuildJRecordedExchange OpenPhysical(
        TimeProvider timeProvider,
        TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(output);

        BuildJTransactionTranscript transcript =
            BuildJTransactionTranscript.CreateLocal(timeProvider);
        IRs50HidppDisplayExchange? inner = null;
        try
        {
            inner = Rs50HidppDeviceExchange.Open();
            output.WriteLine(
                $"Build J local transcript: {transcript.RelativePath}");
            return new(inner, transcript, timeProvider);
        }
        catch
        {
            inner?.Dispose();
            transcript.Dispose();
            throw;
        }
    }

    public byte[] Exchange(Rs50HidppDisplayTransaction transaction)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(transaction);

        int sequence = transcript.RecordRequest(
            transaction,
            timeProvider.GetUtcNow());
        long startTimestamp = timeProvider.GetTimestamp();

        byte[] response;
        try
        {
            response = inner.Exchange(transaction);
        }
        catch (Exception exception)
        {
            TimeSpan elapsed = timeProvider.GetElapsedTime(
                startTimestamp,
                timeProvider.GetTimestamp());
            transcript.RecordFailure(
                sequence,
                transaction,
                exception,
                timeProvider.GetUtcNow(),
                elapsed);
            throw;
        }

        TimeSpan responseElapsed = timeProvider.GetElapsedTime(
            startTimestamp,
            timeProvider.GetTimestamp());
        transcript.RecordResponse(
            sequence,
            transaction,
            response,
            timeProvider.GetUtcNow(),
            responseElapsed);
        return response;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        try
        {
            inner.Dispose();
        }
        finally
        {
            transcript.Dispose();
        }
    }
}

internal sealed class BuildJTransactionTranscript : IDisposable
{
    internal const int MaximumTransactions = 52;

    private readonly TextWriter writer;
    private readonly bool ownsWriter;
    private readonly object writeGate = new();
    private int transactionCount;
    private bool disposed;

    internal BuildJTransactionTranscript(
        TextWriter writer,
        string relativePath,
        bool ownsWriter = false)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        this.writer = writer;
        this.ownsWriter = ownsWriter;
        RelativePath = relativePath;
    }

    internal string RelativePath { get; }

    internal static BuildJTransactionTranscript CreateLocal(
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        const string transcriptDirectory =
            ".tmp/rs50-build-j-transcripts";
        string fileName =
            "rs50-build-j-transcript-" +
            timeProvider.GetUtcNow().ToString(
                "yyyyMMdd-HHmmss-fffffff'Z'",
                CultureInfo.InvariantCulture) +
            ".jsonl";
        string relativePath = Path.Combine(
            transcriptDirectory,
            fileName);
        string absolutePath = Path.GetFullPath(
            relativePath,
            Environment.CurrentDirectory);

        Directory.CreateDirectory(
            Path.GetDirectoryName(absolutePath)!);
        var stream = new FileStream(
            absolutePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.Read,
            bufferSize: 4096,
            FileOptions.WriteThrough);
        var writer = new StreamWriter(stream)
        {
            AutoFlush = true
        };

        return new(writer, relativePath, ownsWriter: true);
    }

    internal int RecordRequest(
        Rs50HidppDisplayTransaction transaction,
        DateTimeOffset timestampUtc)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        lock (writeGate)
        {
            ThrowIfDisposed();
            if (transactionCount >= MaximumTransactions)
            {
                throw new IOException(
                    "Build J exceeded its bounded transcript capacity.");
            }

            int sequence = ++transactionCount;
            WriteLine(
                new
                {
                    schema_version = 1,
                    sequence,
                    event_type = "request",
                    timestamp_utc = timestampUtc.ToUniversalTime(),
                    transaction = transaction.Kind.ToString(),
                    report_hex = Convert.ToHexString(
                        transaction.Request.Span)
                });
            return sequence;
        }
    }

    internal void RecordResponse(
        int sequence,
        Rs50HidppDisplayTransaction transaction,
        ReadOnlySpan<byte> response,
        DateTimeOffset timestampUtc,
        TimeSpan elapsed)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        lock (writeGate)
        {
            ThrowIfDisposed();
            WriteLine(
                new
                {
                    schema_version = 1,
                    sequence,
                    event_type = "response",
                    timestamp_utc = timestampUtc.ToUniversalTime(),
                    elapsed_microseconds =
                        ToWholeMicroseconds(elapsed),
                    transaction = transaction.Kind.ToString(),
                    report_hex = Convert.ToHexString(response),
                    exact_header_match = HasExactResponseHeader(
                        transaction,
                        response)
                });
        }
    }

    internal void RecordFailure(
        int sequence,
        Rs50HidppDisplayTransaction transaction,
        Exception exception,
        DateTimeOffset timestampUtc,
        TimeSpan elapsed)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentNullException.ThrowIfNull(exception);

        lock (writeGate)
        {
            ThrowIfDisposed();
            WriteLine(
                new
                {
                    schema_version = 1,
                    sequence,
                    event_type = "failure",
                    timestamp_utc = timestampUtc.ToUniversalTime(),
                    elapsed_microseconds =
                        ToWholeMicroseconds(elapsed),
                    transaction = transaction.Kind.ToString(),
                    exception_type = exception.GetType().FullName
                });
        }
    }

    public void Dispose()
    {
        lock (writeGate)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            if (ownsWriter)
            {
                writer.Dispose();
            }
        }
    }

    private void WriteLine<T>(T entry)
    {
        writer.WriteLine(JsonSerializer.Serialize(entry));
        writer.Flush();
    }

    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(disposed, this);

    private static long ToWholeMicroseconds(TimeSpan elapsed) =>
        checked(elapsed.Ticks / TimeSpan.TicksPerMicrosecond);

    private static bool HasExactResponseHeader(
        Rs50HidppDisplayTransaction transaction,
        ReadOnlySpan<byte> response)
    {
        if (response.Length < 4)
        {
            return false;
        }

        ReadOnlySpan<byte> request = transaction.Request.Span;
        byte expectedFeature =
            transaction.Kind ==
            Rs50HidppDisplayTransactionKind.DiscoverDisplayFeature
                ? (byte)0
                : request[2];

        return response[0] == 0x12 &&
            response[1] == Rs50HidppDisplayProtocol.BaseDeviceIndex &&
            response[2] == expectedFeature &&
            response[3] == request[3];
    }
}
