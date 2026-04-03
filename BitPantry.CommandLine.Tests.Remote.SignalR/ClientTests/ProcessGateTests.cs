using FluentAssertions;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientTests;

/// <summary>
/// Tests for the simplified ProcessGate async mutex.
/// ProcessGate provides mutual exclusion - only one caller can hold the lock at a time.
/// </summary>
[TestClass]
public class ProcessGateTests
{
    private ProcessGate _processGate;

    [TestInitialize]
    public void Setup()
    {
        _processGate = new ProcessGate();
    }

    /// <summary>
    /// Test Validity Check:
    ///   Invokes code under test: YES (calls LockAsync and Dispose)
    ///   Breakage detection: YES (if lock fails to acquire, test would fail)
    ///   Not a tautology: YES (exercises actual lock acquisition)
    /// </summary>
    [TestMethod]
    public async Task LockAsync_AcquiresAndReleases_Succeeds()
    {
        // Acquire lock and immediately release via using
        using (await _processGate.LockAsync())
        {
            // If we reach this point, the lock was acquired successfully
        }
        
        // Verify we can acquire again after release
        using (await _processGate.LockAsync())
        {
            // Lock successfully re-acquired
        }
    }

    /// <summary>
    /// Test Validity Check:
    ///   Invokes code under test: YES (two concurrent callers to LockAsync)
    ///   Breakage detection: YES (if both acquire simultaneously, timing check fails)
    ///   Not a tautology: YES (verifies mutual exclusion behavior)
    /// </summary>
    [TestMethod]
    public async Task LockAsync_SecondCaller_BlocksUntilFirstReleases()
    {
        var firstLockAcquired = false;
        var secondLockAcquiredBeforeFirstReleased = false;

        var task1 = Task.Run(async () =>
        {
            using (await _processGate.LockAsync())
            {
                firstLockAcquired = true;
                await Task.Delay(300); // Hold lock for a bit
            }
        });

        // Give first task time to acquire lock
        await Task.Delay(50);
        firstLockAcquired.Should().BeTrue("First lock should be acquired");

        var task2 = Task.Run(async () =>
        {
            // Try to acquire - should block until first releases
            using (await _processGate.LockAsync())
            {
                // If we got here while task1 is still running, the lock isn't working
                if (!task1.IsCompleted)
                    secondLockAcquiredBeforeFirstReleased = true;
            }
        });

        await Task.WhenAll(task1, task2);

        secondLockAcquiredBeforeFirstReleased.Should().BeFalse(
            "Second caller should not acquire lock until first caller releases");
    }

    /// <summary>
    /// Test Validity Check:
    ///   Invokes code under test: YES (calls LockAsync with cancellation)
    ///   Breakage detection: YES (if cancellation doesn't work, test hangs or doesn't throw)
    ///   Not a tautology: YES (verifies cancellation behavior)
    /// </summary>
    [TestMethod]
    public async Task LockAsync_CancellationRequested_ThrowsOperationCanceled()
    {
        // First, hold the lock so the second attempt will block
        using (await _processGate.LockAsync())
        {
            // Now try to acquire with a token that will cancel before we release
            using var cts = new CancellationTokenSource(50);

            Func<Task> action = async () =>
            {
                using (await _processGate.LockAsync(cts.Token)) { }
            };

            // The action should throw because it can't acquire the lock before cancellation
            await action.Should().ThrowAsync<OperationCanceledException>();
        }
    }

    /// <summary>
    /// Test Validity Check:
    ///   Invokes code under test: YES (acquire, release, acquire again)
    ///   Breakage detection: YES (if release doesn't work, second acquire hangs)
    ///   Not a tautology: YES (verifies lock can be reacquired after release)
    /// </summary>
    [TestMethod]
    public async Task LockAsync_AfterDispose_CanReacquire()
    {
        using (await _processGate.LockAsync())
        {
            // Holding the lock
        }

        // Should be able to acquire again immediately after disposal
        Func<Task> action = async () => 
        { 
            using (await _processGate.LockAsync()) 
            { 
            } 
        };
        
        await action.Should().NotThrowAsync();
    }

    /// <summary>
    /// Test Validity Check:
    ///   Invokes code under test: YES (multiple sequential callers)
    ///   Breakage detection: YES (if serialization fails, counts would overlap)
    ///   Not a tautology: YES (verifies multiple callers serialize correctly)
    /// </summary>
    [TestMethod]
    public async Task LockAsync_MultipleConcurrentCallers_SerializeCorrectly()
    {
        var maxConcurrent = 0;
        var currentConcurrent = 0;

        async Task RunWithLock()
        {
            using (await _processGate.LockAsync())
            {
                var current = Interlocked.Increment(ref currentConcurrent);
                if (current > maxConcurrent)
                    Interlocked.Exchange(ref maxConcurrent, current);
                
                await Task.Delay(50); // Hold lock briefly
                Interlocked.Decrement(ref currentConcurrent);
            }
        }

        var tasks = new List<Task>
        {
            Task.Run(RunWithLock),
            Task.Run(RunWithLock),
            Task.Run(RunWithLock)
        };

        await Task.WhenAll(tasks);

        // With a mutex, max concurrent should always be 1
        maxConcurrent.Should().Be(1, "Only one caller should hold the lock at a time");
    }
}
