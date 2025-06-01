using Xunit;
using Xunit.v3;

namespace Serilog.Sinks.XUnit;

using System;
using System.IO;
using Core;
using Events;
using Formatting;
using Xunit.Sdk;

/// <summary>
/// A sink to direct Serilog output to the XUnit test output
/// </summary>
public class TestOutputSink : ILogEventSink
{
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
        if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));

        var renderSpace = new StringWriter();
        _textFormatter.Format(logEvent, renderSpace);
        var message = renderSpace.ToString().Trim();
        _testOutputHelper?.WriteLine(message);
    }
}