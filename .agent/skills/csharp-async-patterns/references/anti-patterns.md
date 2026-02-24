# Async Anti-Patterns (POCU)

## Table of Contents
1. [Blocking on Async Code](#1-blocking-on-async-code)
2. [Async Void (Outside Event Handlers)](#2-async-void-outside-event-handlers)
3. [Fire-and-Forget Without Error Handling](#3-fire-and-forget-without-error-handling)
4. [Missing Cancellation Support](#4-missing-cancellation-support)
5. [Over-Using Async](#5-over-using-async)
6. [Not Passing CancellationToken Through](#6-not-passing-cancellationtoken-through)
7. [Capturing Modified Variables in Async Loops](#7-capturing-modified-variables-in-async-loops)
8. [Async in Constructors](#8-async-in-constructors)
9. [Ignoring Task Results](#9-ignoring-task-results)
10. [Mixing Sync and Async Code Poorly](#10-mixing-sync-and-async-code-poorly)

## 1. Blocking on Async Code

### [WRONG] The Problem

```csharp
public class BadService
{
    public void SyncMethod()
    {
        // DEADLOCK RISK!
        Result result = DoWork().Result;
        Result result2 = DoWork().GetAwaiter().GetResult();
        DoWork().Wait();
    }
}
```

**Why it's bad**: Can cause deadlocks, especially in UI applications with synchronization context.

### [CORRECT] The Solution (POCU)

```csharp
public class GoodService
{
    private readonly ILogger mLogger;

    // [CORRECT] POCU: No Async suffix
    public async Task DoWorkProperly()
    {
        Result result = await doWork();
        process(result);
    }

    private async Task<Result> doWork()
    {
        return await mRepository.Load();
    }

    private void process(Result result)
    {
        Debug.Assert(result != null);
        // Process result
    }

    // If you must have sync entry point (not recommended):
    public void SyncEntryPoint()
    {
        doWork().GetAwaiter().GetResult(); // Use with extreme caution
    }
}
```

## 2. Async Void (Outside Event Handlers)

### [WRONG] The Problem

```csharp
// BAD: Exceptions are lost!
public async void LoadData()
{
    await Task.Delay(1000);
    throw new Exception("This crashes the app!");
}
```

**Why it's bad**: Exceptions cannot be caught by caller, leading to application crashes.

### [CORRECT] The Solution (POCU)

```csharp
public class DataLoader
{
    private readonly ILogger mLogger;

    // [CORRECT] POCU: Returns Task, No Async suffix
    public async Task LoadData()
    {
        await Task.Delay(1000);
        throw new Exception("Can be caught by caller");
    }

    // [CAUTION] EXCEPTION: Event handlers MUST be async void
    private async void OnButtonClick(object sender, EventArgs e)
    {
        try
        {
            await LoadData();
        }
        catch (Exception ex)
        {
            mLogger.Error(ex, "Button click failed");
        }
    }
}
```

## 3. Fire-and-Forget Without Error Handling

### [WRONG] The Problem

```csharp
public class BadFireAndForget
{
    public void StartOperation()
    {
        _ = LongRunning(); // Exceptions disappear!
    }
}
```

**Why it's bad**: Exceptions are silently swallowed, making debugging impossible.

### [CORRECT] The Solution (POCU)

```csharp
public class GoodFireAndForget
{
    private readonly ILogger mLogger;

    public void StartOperation()
    {
        _ = safeFireAndForget();
    }

    // [CORRECT] POCU: camelCase for private methods
    private async Task safeFireAndForget()
    {
        try
        {
            await longRunning();
        }
        catch (Exception ex)
        {
            mLogger.Error(ex, "Background operation failed");
            // Optionally: notify user, retry, etc.
        }
    }

    private async Task longRunning()
    {
        await Task.Delay(5000);
        // Long running work
    }
}
```

## 4. Missing Cancellation Support

### [WRONG] The Problem

```csharp
// BAD: No way to cancel
public async Task LongProcess()
{
    for (int i = 0; i < 1000; i++)
    {
        await ProcessItem(i);
    }
}
```

**Why it's bad**: User cannot stop long-running operations, wastes resources.

### [CORRECT] The Solution (POCU)

```csharp
public class CancellableProcessor
{
    private readonly IItemProcessor mProcessor;

    // [CORRECT] POCU: Cancellable
    public async Task LongProcess(CancellationToken ct)
    {
        for (int i = 0; i < 1000; i++)
        {
            ct.ThrowIfCancellationRequested();
            await mProcessor.Process(i, ct);
        }
    }
}
```

## 5. Over-Using Async

### [WRONG] The Problem

```csharp
// BAD: Unnecessary async overhead
public async Task<int> GetValue()
{
    return await Task.FromResult(42);
}

public async Task<string> GetName()
{
    return await Task.FromResult("John");
}
```

**Why it's bad**: Async machinery adds overhead for synchronous operations.

### [CORRECT] The Solution (POCU)

```csharp
public class ValueProvider
{
    // [CORRECT] POCU: Return Task directly or use sync method
    public Task<int> GetValue()
    {
        return Task.FromResult(42);
    }

    // Or better: Just use synchronous method
    public int GetValueSync()
    {
        return 42;
    }

    public string GetNameSync()
    {
        return "John";
    }
}
```

## 6. Not Passing CancellationToken Through

### [WRONG] The Problem

```csharp
public async Task Process(CancellationToken ct)
{
    // BAD: Not passing ct to inner calls
    await Step1();
    await Step2();
    await Step3();
}
```

**Why it's bad**: Cancellation doesn't propagate, operations continue unnecessarily.

### [CORRECT] The Solution (POCU)

```csharp
public class ProcessorWithCancellation
{
    // [CORRECT] POCU: Pass ct through all async calls
    public async Task Process(CancellationToken ct)
    {
        await step1(ct);
        await step2(ct);
        await step3(ct);
    }

    private async Task step1(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Task.Delay(100, ct);
    }

    private async Task step2(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Task.Delay(100, ct);
    }

    private async Task step3(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Task.Delay(100, ct);
    }
}
```

## 7. Capturing Modified Variables in Async Loops

### [WRONG] The Problem

```csharp
// BAD: Variable capture issue
List<Task> tasks = new List<Task>();
for (int i = 0; i < 10; i++)
{
    tasks.Add(Task.Run(async () =>
    {
        await Task.Delay(100);
        Console.WriteLine(i); // All print 10!
    }));
}
```

**Why it's bad**: Loop variable is captured by reference, all tasks see final value.

### [CORRECT] The Solution (POCU)

```csharp
public class LoopCaptureFixed
{
    private readonly ILogger mLogger;

    public async Task ExecuteLoop()
    {
        List<Task> tasks = new List<Task>();

        for (int i = 0; i < 10; i++)
        {
            int index = i; // Copy to local variable
            Task task = Task.Run(async () =>
            {
                await Task.Delay(100);
                mLogger.Info($"Index: {index}"); // Prints 0-9
            });
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
    }
}
```

## 8. Async in Constructors

### [WRONG] The Problem

```csharp
// BAD: Cannot await in constructor
public class BadService
{
    public BadService()
    {
        // Cannot use async/await here!
        Initialize().Wait(); // DEADLOCK RISK
    }
}
```

**Why it's bad**: Constructors cannot be async, blocking causes deadlocks.

### [CORRECT] The Solution (POCU)

```csharp
// [CORRECT] POCU: Factory pattern
public class GoodService
{
    private readonly Config mConfig;

    private GoodService(Config config)
    {
        Debug.Assert(config != null);
        mConfig = config;
    }

    public static async Task<GoodService> Create()
    {
        Config config = await loadConfig();
        GoodService service = new GoodService(config);
        return service;
    }

    private static async Task<Config> loadConfig()
    {
        await Task.Delay(100);
        return new Config();
    }
}

// Or: Lazy initialization
public class LazyService
{
    private readonly Task mInitTask;
    private bool mbIsInitialized;

    public LazyService()
    {
        mInitTask = initialize();
    }

    public async Task EnsureInitialized()
    {
        await mInitTask;
    }

    private async Task initialize()
    {
        await Task.Delay(100);
        mbIsInitialized = true;
    }
}
```

## 9. Ignoring Task Results

### [WRONG] The Problem

```csharp
public async Task Process()
{
    Task.Run(() => BackgroundWork()); // Task ignored!
    await OtherWork();
}
```

**Why it's bad**: Background task exceptions are lost, no way to track completion.

### [CORRECT] The Solution (POCU)

```csharp
public class TaskTracker
{
    private readonly ILogger mLogger;

    public async Task Process()
    {
        // [CORRECT] POCU: Explicit type
        Task backgroundTask = Task.Run(() => backgroundWork());

        await otherWork();

        // Wait for background work
        await backgroundTask;
    }

    private void backgroundWork()
    {
        // Background operation
    }

    private async Task otherWork()
    {
        await Task.Delay(100);
    }

    // Or if truly fire-and-forget:
    public void StartBackground()
    {
        _ = safeFireAndForget();
    }

    private async Task safeFireAndForget()
    {
        try
        {
            await backgroundTask();
        }
        catch (Exception ex)
        {
            mLogger.Error(ex, "Background task failed");
        }
    }

    private async Task backgroundTask()
    {
        await Task.Delay(1000);
    }
}
```

## 10. Mixing Sync and Async Code Poorly

### [WRONG] The Problem

```csharp
// BAD: Mixed sync/async
public void ProcessData()
{
    Data data = LoadData().Result; // Blocking!
    SaveData(data); // Sync
}
```

**Why it's bad**: Loses benefits of async, introduces deadlock risks.

### [CORRECT] The Solution (POCU)

```csharp
public class ConsistentProcessor
{
    private readonly IRepository mRepository;

    // [CORRECT] POCU: Async all the way
    public async Task ProcessData()
    {
        Data data = await mRepository.Load();
        await mRepository.Save(data);
    }

    // Or: Sync all the way
    public void ProcessDataSync()
    {
        Data data = mRepository.LoadSync();
        mRepository.SaveSync(data);
    }
}
```

## Summary Checklist

Avoid these anti-patterns:

- [ ] .Result, .Wait()  Prohibited
- [ ] async void Prohibited (  )
- [ ] Fire-and-forget   
- [ ]    CancellationToken 
- [ ]    CancellationToken 
- [ ] async  I/O-bound  
- [ ]     
- [ ]  async Prohibited
- [ ] Task   Prohibited
- [ ]    sync  async  
- [ ] Async  Prohibited (POCU )
- [ ] var  Use explicit types
- [ ] using   Use using statement
