# Async/Await Best Practices (POCU)

## Table of Contents
1. [Always Use CancellationToken](#1-always-use-cancellationtoken)
2. [Avoid async void](#2-avoid-async-void-except-event-handlers)
3. [ConfigureAwait in Library Code](#3-configureawait-in-library-code)
4. [Proper Task Composition](#4-proper-task-composition)
5. [ValueTask for Hot Paths](#5-valuetask-for-hot-paths)
6. [Timeout Patterns](#6-timeout-patterns)
7. [Retry Logic](#7-retry-logic)
8. [Proper Disposal with Async](#8-proper-disposal-with-async)
9. [Checklist](#checklist)

## 1. Always Use CancellationToken

```csharp
public class DataProcessor
{
    private readonly IDataService mDataService;

    public async Task<Result> Process(CancellationToken ct = default)
    {
        await Task.Delay(1000, ct);
        ct.ThrowIfCancellationRequested();

        Result result = await mDataService.LoadData(ct);
        return result;
    }

    // [WRONG] WRONG: No cancellation support
    public async Task<Result> ProcessBad()
    {
        await Task.Delay(1000);
        Result result = await mDataService.LoadData();
        return result;
    }
}
```

## 2. Avoid async void (Except Event Handlers)

```csharp
public class DataLoader
{
    private readonly ILogger mLogger;

    // [WRONG] WRONG: Exceptions unhandled
    public async void LoadData()
    {
        await fetch();
        throw new Exception("Lost!"); // Crashes app
    }

    // [CORRECT] CORRECT: Returns Task, No Async suffix
    public async Task LoadData()
    {
        await fetch();
        throw new Exception("Catchable");
    }

    // [CAUTION] EXCEPTION: Event handlers only
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

    private async Task fetch()
    {
        // Implementation
    }
}
```

## 3. ConfigureAwait in Library Code

```csharp
public class LibraryService
{
    private readonly IDataRepository mRepository;

    // Library code: ConfigureAwait(false) required
    public async Task<string> GetData()
    {
        // [CORRECT] Don't capture synchronization context
        string data = await mRepository.Fetch().ConfigureAwait(false);
        return processData(data);
    }

    private string processData(string data)
    {
        Debug.Assert(data != null);
        return data.Trim();
    }
}

public class UIController
{
    private readonly TextField mTextField;

    // UI/Application code: default captures context
    public async Task UpdateUI()
    {
        // [CORRECT] Default captures context (ConfigureAwait(true))
        string data = await GetData();
        mTextField.Text = data; // Safe: on UI thread
    }
}
```

## 4. Proper Task Composition

### Parallel Execution

```csharp
public class DashboardService
{
    private readonly IUserService mUserService;
    private readonly IOrderService mOrderService;

    // [CORRECT] POCU: Run tasks in parallel
    public async Task<(Users, Orders)> LoadMultiple()
    {
        Task<Users> task1 = mUserService.GetUsers();
        Task<Orders> task2 = mOrderService.GetOrders();

        await Task.WhenAll(task1, task2);

        return (await task1, await task2);
    }
}
```

### Sequential with Dependency

```csharp
public class OrderProcessor
{
    private readonly IRepository mRepository;

    // [CORRECT] POCU: Sequential execution
    public async Task<Result> Process()
    {
        Data data = await loadData();
        ProcessedData processed = await processData(data);
        return await save(processed);
    }

    private async Task<Data> loadData()
    {
        return await mRepository.GetData();
    }

    private async Task<ProcessedData> processData(Data data)
    {
        Debug.Assert(data != null);
        return await mRepository.Process(data);
    }

    private async Task<Result> save(ProcessedData data)
    {
        Debug.Assert(data != null);
        return await mRepository.Save(data);
    }
}
```

### First Wins

```csharp
public class CacheService
{
    private readonly ICacheProvider mCache;
    private readonly INetworkProvider mNetwork;

    // [CORRECT] POCU: Use first completed result
    public async Task<string> LoadFromMultiple()
    {
        Task<string> task1 = mCache.Load();
        Task<string> task2 = mNetwork.Load();

        Task<string> completed = await Task.WhenAny(task1, task2);
        return await completed;
    }
}
```

## 5. ValueTask for Hot Paths

```csharp
public class CachedRepository
{
    private readonly Dictionary<string, int> mCache;
    private readonly IDataSource mSource;

    // [CORRECT] POCU: Allocation-free for cached results
    public ValueTask<int> GetCachedValue(string key)
    {
        Debug.Assert(key != null);

        int value;
        if (mCache.TryGetValue(key, out value))
        {
            return new ValueTask<int>(value); // No allocation
        }

        return new ValueTask<int>(fetchFromSource(key));
    }

    private async Task<int> fetchFromSource(string key)
    {
        int value = await mSource.Fetch(key);
        mCache[key] = value;
        return value;
    }

    // [WRONG] WRONG: Task allocates even for cached values
    public Task<int> GetCachedValueBad(string key)
    {
        int value;
        if (mCache.TryGetValue(key, out value))
        {
            return Task.FromResult(value); // Allocates!
        }

        return fetchFromSource(key);
    }
}
```

## 6. Timeout Patterns

```csharp
public class TimeoutService
{
    private readonly IDataLoader mLoader;
    private readonly ILogger mLogger;

    // [CORRECT] POCU: Use using statement, Explicit type
    public async Task<Data> LoadWithTimeout(TimeSpan timeout)
    {
        using (CancellationTokenSource cts = new CancellationTokenSource(timeout))
        {
            try
            {
                return await mLoader.Load(cts.Token);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                mLogger.Warning("Operation timed out");
                throw new TimeoutException("Operation timed out");
            }
        }
    }
}
```

## 7. Retry Logic

```csharp
public class RetryService
{
    private readonly ILogger mLogger;

    // [CORRECT] POCU: Exponential backoff retry
    public async Task<T> ExecuteWithRetry<T>(
        Func<Task<T>> operation,
        int maxAttempts = 3,
        TimeSpan? initialDelayOrNull = null)
    {
        Debug.Assert(operation != null);
        Debug.Assert(maxAttempts > 0);

        TimeSpan delay;
        if (initialDelayOrNull.HasValue)
        {
            delay = initialDelayOrNull.Value;
        }
        else
        {
            delay = TimeSpan.FromSeconds(1);
        }

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                mLogger.Warning($"Attempt {attempt} failed, retrying...", ex);
                await Task.Delay(delay);
                delay = TimeSpan.FromTicks(delay.Ticks * 2); // Exponential backoff
            }
        }

        return await operation(); // Final attempt
    }
}
```

## 8. Proper Disposal with Async

```csharp
public class AsyncResource : IAsyncDisposable
{
    private HttpClient mClient;
    private bool mbIsDisposed;

    public async ValueTask DisposeAsync()
    {
        if (mbIsDisposed)
        {
            return;
        }

        if (mClient != null)
        {
            await mClient.DisposeAsync();
            mClient = null;
        }

        mbIsDisposed = true;
    }
}

// [CORRECT] POCU: Use using statement
public class ResourceConsumer
{
    public async Task UseResource()
    {
        await using (AsyncResource resource = new AsyncResource())
        {
            await resource.Execute();
        }
    }
}
```

## Checklist

Before completing async implementation:

- [ ]  async  Task, Task<T>, ValueTask, ValueTask<T>  (async void Prohibited)
- [ ] No Async suffix (POCU )
- [ ]    CancellationToken  
- [ ]  async  CancellationToken 
- [ ] .Result, .Wait(), .GetAwaiter().GetResult() Prohibited ( )
- [ ]   ConfigureAwait(false) 
- [ ]   async  
- [ ]   hot path ValueTask 
- [ ]     
- [ ]     
- [ ] var  Use explicit types
- [ ] using   Use using statement
