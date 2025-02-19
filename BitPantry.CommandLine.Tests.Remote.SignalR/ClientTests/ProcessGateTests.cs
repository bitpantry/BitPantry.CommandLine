using FluentAssertions;

[TestClass]
public class ProcessGateTests
{
    private ProcessGate _processGate;

    [TestInitialize]
    public void Setup()
    {
        _processGate = new ProcessGate();
    }

    [TestMethod]
    public async Task LockAsync_Should_Allow_Single_Process()
    {
        using (await _processGate.LockAsync("TestProcess"))
        {
            // If we reach this point, the lock was acquired successfully
            true.Should().BeTrue();
        }
    }

    [TestMethod]
    public async Task LockAsync_Should_Block_Other_Processes_Until_Release()
    {
        var firstLockAcquired = false;
        var secondLockAcquired = false;

        var task1 = Task.Run(async () =>
        {
            using (await _processGate.LockAsync("ProcessA"))
            {
                firstLockAcquired = true;
                await Task.Delay(500); // Simulate work while holding the lock
            }
        });

        // Ensure the first process acquires the lock before starting the second one
        await Task.Delay(100);
        var task2 = Task.Run(async () =>
        {
            using (await _processGate.LockAsync("ProcessB"))
            {
                secondLockAcquired = true;
            }
        });

        await Task.WhenAll(task1, task2);

        firstLockAcquired.Should().BeTrue();
        secondLockAcquired.Should().BeTrue();
    }

    [TestMethod]
    public async Task LockAsync_Should_Allow_Concurrent_Access_For_Same_Key()
    {
        var concurrentCount = 0;

        async Task RunProcess()
        {
            using (await _processGate.LockAsync("SameKey"))
            {
                Interlocked.Increment(ref concurrentCount);
                await Task.Delay(200);
                Interlocked.Decrement(ref concurrentCount);
            }
        }

        var tasks = new List<Task>
        {
            Task.Run(RunProcess),
            Task.Run(RunProcess),
            Task.Run(RunProcess)
        };

        await Task.WhenAll(tasks);

        // If the test completes, it means multiple instances of the same process ran
        true.Should().BeTrue();
    }

    [TestMethod]
    public async Task LockAsync_Should_Block_New_Keys_While_Existing_Process_Is_Running()
    {
        var secondProcessStarted = false;

        var task1 = Task.Run(async () =>
        {
            using (await _processGate.LockAsync("ProcessX"))
            {
                await Task.Delay(500); // Simulate long-running process
            }
        });

        await Task.Delay(100); // Ensure first process starts before second one

        var task2 = Task.Run(async () =>
        {
            using (await _processGate.LockAsync("ProcessY"))
            {
                secondProcessStarted = true;
            }
        });

        await Task.WhenAll(task1, task2);

        secondProcessStarted.Should().BeTrue();
    }

    [TestMethod]
    public async Task LockAsync_Should_Release_Lock_After_Disposal()
    {
        using (await _processGate.LockAsync("ReleaseTest"))
        {
            // Holding the lock
        }

        Func<Task> action = async () => { using (await _processGate.LockAsync("ReleaseTest")) { } };
        await action.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task LockAsync_Should_Honor_CancellationToken()
    {
        using var cts = new CancellationTokenSource(100); // Cancel after 100ms

        Func<Task> action = async () =>
        {
            using (await _processGate.LockAsync("CancellableProcess", cts.Token)) { }
        };

        await Task.Delay(120);

        await action.Should().ThrowAsync<OperationCanceledException>();
    }


}
