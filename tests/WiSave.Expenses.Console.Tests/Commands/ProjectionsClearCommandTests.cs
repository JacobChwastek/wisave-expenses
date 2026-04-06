using WiSave.Expenses.Console.Commands;
using WiSave.Expenses.Console.Execution;
using WiSave.Expenses.Console.Operations;
using WiSave.Expenses.Console.Shell;

namespace WiSave.Expenses.Console.Tests.Commands;

public class ProjectionsClearCommandTests
{
    [Fact]
    public async Task ExecuteAsync_returns_cancellation_message_when_confirmation_is_rejected()
    {
        var operations = new StubProjectionClearOperations();
        var consoleOutput = new TestConsoleOutput("no");
        var sut = new ProjectionsClearCommand(operations, consoleOutput);

        var result = await sut.ExecuteAsync(
            new CommandExecutionContext(new Dictionary<string, string?>(), allowPrompting: true),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("Projection clear cancelled.", result.Message);
        Assert.False(operations.WasCalled);
    }

    private sealed class StubProjectionClearOperations : IProjectionClearOperations
    {
        public bool WasCalled { get; private set; }

        public Task<ProjectionClearResult> RunAsync(string? connectionString, CancellationToken ct)
        {
            WasCalled = true;
            return Task.FromResult(new ProjectionClearResult("projections", ["accounts"]));
        }
    }

    private sealed class TestConsoleOutput(params string[] inputs) : IConsoleOutput
    {
        private readonly Queue<string> inputQueue = new(inputs);

        public void Write(string value)
        {
        }

        public void WriteLine(string? value)
        {
        }

        public string? ReadLine()
            => inputQueue.Count > 0 ? inputQueue.Dequeue() : null;

        public void Clear()
        {
        }
    }
}
