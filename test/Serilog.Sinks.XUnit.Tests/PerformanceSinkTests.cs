namespace Serilog.Sinks.XUnit.Tests;

using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using Events;
using NSubstitute;
using Xunit;

/// <summary>
/// Performance tests designed for profiling the sink's hot path.
/// Run these tests with a profiler attached (e.g. dotMemory, PerfView, BenchmarkDotNet) to
/// measure allocation rates, CPU hot paths, and lock-free concurrency behaviour.
/// </summary>
public static class PerformanceSinkTests
{
    /// <summary>
    /// Emits a large number of log events on a single thread so that a profiler can observe
    /// per-call allocation counts and CPU time inside <see cref="TestOutputSink.Emit"/>.
    /// </summary>
    [Fact]
    [Trait("Category", "Performance")]
    public static void Emit_SingleThread_HighVolume_ShouldWriteAllMessages()
    {
        var outputHelper = Substitute.For<ITestOutputHelper>();
        var logger = outputHelper.CreateTestLogger(LogEventLevel.Verbose);

        const int iterations = 10_000;

        for (int i = 0; i < iterations; i++)
        {
            logger.Information("Performance test message {Index} with value {Value}", i, i * 3.14);
        }

        outputHelper.Received(iterations).WriteLine(Arg.Any<string>());
    }

    /// <summary>
    /// Emits log events concurrently from multiple threads to verify that the lock-free
    /// thread-local pooling strategy is safe under parallel workloads.
    /// </summary>
    [Fact]
    [Trait("Category", "Performance")]
    public static async Task Emit_MultiThread_HighVolume_ShouldCompleteWithoutExceptions()
    {
        int callCount = 0;
        var outputHelper = Substitute.For<ITestOutputHelper>();
        outputHelper
            .When(h => h.WriteLine(Arg.Any<string>()))
            .Do(_ => Interlocked.Increment(ref callCount));

        var logger = outputHelper.CreateTestLogger(LogEventLevel.Verbose);

        const int threadCount = 4;
        const int iterationsPerThread = 2_500;
        const int totalIterations = threadCount * iterationsPerThread;

        var ct = TestContext.Current.CancellationToken;
        var tasks = new Task[threadCount];
        for (int t = 0; t < threadCount; t++)
        {
            int threadId = t;
            tasks[t] = Task.Run(() =>
            {
                for (int i = 0; i < iterationsPerThread; i++)
                {
                    logger.Information("Thread {ThreadId} message {Index}", threadId, i);
                }
            }, ct);
        }

        await tasks.Awaiting(t => Task.WhenAll(t)).Should().NotThrowAsync();

        callCount.Should().Be(totalIterations);
    }
}
