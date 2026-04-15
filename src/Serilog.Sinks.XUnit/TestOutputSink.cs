using Xunit;

namespace Serilog.Sinks.XUnit;

using System;
using System.IO;
using System.Text;
using Core;
using Events;
using Formatting;

/// <summary>
/// A sink to direct Serilog output to the XUnit test output
/// </summary>
public class TestOutputSink : ILogEventSink
{
    // Thread-local StringWriter (backed by a reusable StringBuilder) avoids per-emit heap allocations
    // and is inherently lock-free because each thread operates on its own instance.
    [ThreadStatic]
    private static StringWriter? _threadLocalWriter;

    // Capacity used when creating or resetting the thread-local StringBuilder.
    private const int InitialCapacity = 512;

    // If a single message causes the StringBuilder to grow beyond this threshold, the writer is
    // replaced on the next call so the oversized buffer is released to the GC.
    private const int MaxRetainedCapacity = 4096;

    private readonly ITestOutputHelper _testOutputHelper;
    private readonly ITextFormatter _textFormatter;

    /// <summary>
    /// Creates a new instance of <see cref="TestOutputSink"/>
    /// </summary>
    /// <param name="testOutputHelper">An <see cref="ITestOutputHelper"/> implementation that can be used to provide test output</param>
    /// <param name="textFormatter">The <see cref="ITextFormatter"/> used when rendering the message</param>
    public TestOutputSink(ITestOutputHelper testOutputHelper, ITextFormatter textFormatter)
    {
        _testOutputHelper = testOutputHelper ?? throw new ArgumentNullException(nameof(testOutputHelper));
        _textFormatter = textFormatter ?? throw new ArgumentNullException(nameof(textFormatter));
    }

    /// <summary>
    /// Emits the provided log event from a sink 
    /// </summary>
    /// <param name="logEvent">The event being logged</param>
    public void Emit(LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);

        // Reuse the thread-local writer and its underlying StringBuilder to avoid allocations.
        // If a previous message grew the buffer beyond the retention threshold, replace the writer
        // so the oversized buffer is released to the GC.
        var writer = _threadLocalWriter;
        if (writer is null || writer.GetStringBuilder().Capacity > MaxRetainedCapacity)
        {
            _threadLocalWriter = writer = new StringWriter(new StringBuilder(InitialCapacity));
        }

        var sb = writer.GetStringBuilder();
        sb.Clear();

        _textFormatter.Format(logEvent, writer);

        // Trim trailing whitespace in-place on the StringBuilder (avoids allocating a full trimmed copy).
        int end = sb.Length;
        while (end > 0 && char.IsWhiteSpace(sb[end - 1]))
            end--;

        // Trim leading whitespace.
        int start = 0;
        while (start < end && char.IsWhiteSpace(sb[start]))
            start++;

        // A single ToString call produces the final string; no intermediate trimmed copy needed.
        _testOutputHelper.WriteLine(start < end ? sb.ToString(start, end - start) : string.Empty);
    }
}