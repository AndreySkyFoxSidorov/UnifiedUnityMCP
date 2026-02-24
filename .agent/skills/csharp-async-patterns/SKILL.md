---
name: csharp-async-patterns
description: Modern C# asynchronous programming patterns using async/await, proper CancellationToken usage, and error handling in async code. Use when guidance needed on async/await best practices, Task composition and coordination, ConfigureAwait usage, ValueTask optimization, or async operation cancellation patterns. Pure .NET framework patterns applicable to any C# application.
requires:
  - csharp-code-style
---

# C# Async/Await Patterns

## Overview

C# Asynchronous Programming Patterns (POCU Standards Applied)

**Foundation Required**: `csharp-code-style` (mPascalCase, No Async suffix, No var)

**Core Topics**:
- async/await basics
- CancellationToken patterns
- ConfigureAwait usage
- Asynchronous error handling
- Task composition and coordination
- ValueTask optimization

## Quick Start

```csharp
public class DataService
{
    private readonly IDataRepository mRepository;
    private readonly ILogger mLogger;

    public DataService(IDataRepository repository, ILogger logger)
    {
        mRepository = repository;
        mLogger = logger;
    }

    // [CORRECT] POCU: No Async suffix
    public async Task<Data> LoadData(CancellationToken ct = default)
    {
        try
        {
            Data data = await mRepository.Fetch(ct);
            return processData(data);
        }
        catch (OperationCanceledException)
        {
            mLogger.Info("Operation cancelled");
            throw;
        }
    }

    private Data processData(Data data)
    {
        Debug.Assert(data != null);
        // Processing logic
        return data;
    }
}
```

## Key Rules (POCU)

### Async Method Naming

```csharp
// [WRONG] WRONG: Using Async suffix
public async Task<Order> GetOrderAsync(int id);
public async Task SaveOrderAsync(Order order);

// [CORRECT] CORRECT: No Async suffix
public async Task<Order> GetOrder(int id);
public async Task SaveOrder(Order order);
```

### Prohibit async void

```csharp
// [WRONG] WRONG: async void
public async void LoadData()
{
    await mRepository.Fetch();
}

// [CORRECT] CORRECT: async Task
public async Task LoadData()
{
    await mRepository.Fetch();
}

// [CAUTION] EXCEPTION: Only event handlers allow async void
private async void OnButtonClick(object sender, EventArgs e)
{
    try
    {
        await ProcessClick();
    }
    catch (Exception ex)
    {
        mLogger.Error(ex, "Click handler failed");
    }
}
```

### Use Explicit Types

```csharp
// [WRONG] WRONG: Using var
var result = await GetOrder(1);
var tasks = new List<Task>();

// [CORRECT] CORRECT: Explicit type
Order result = await GetOrder(1);
List<Task> tasks = new List<Task>();
```

### Use using Statement

```csharp
// [WRONG] WRONG: using declaration
using CancellationTokenSource cts = new CancellationTokenSource(timeout);

// [CORRECT] CORRECT: using statement
using (CancellationTokenSource cts = new CancellationTokenSource(timeout))
{
    return await LoadData(cts.Token);
}
```

## CancellationToken Patterns

```csharp
public class OrderProcessor
{
    private readonly IOrderRepository mRepository;

    // Always support CancellationToken
    public async Task<Order> ProcessOrder(int orderId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        Order order = await mRepository.GetOrder(orderId, ct);
        Debug.Assert(order != null);

        await validateOrder(order, ct);
        await calculateTotal(order, ct);
        await mRepository.SaveOrder(order, ct);

        return order;
    }

    private async Task validateOrder(Order order, CancellationToken ct)
    {
        Debug.Assert(order != null);
        await mRepository.ValidateInventory(order.Items, ct);
    }

    private async Task calculateTotal(Order order, CancellationToken ct)
    {
        Debug.Assert(order != null);
        decimal total = 0;
        foreach (OrderItem item in order.Items)
        {
            ct.ThrowIfCancellationRequested();
            total += item.Price * item.Quantity;
        }
        order.Total = total;
    }
}
```

## Reference Documentation

### [Best Practices](references/best-practices.md)
Essential patterns for async/await:
- CancellationToken usage pattern
- ConfigureAwait guidelines
- Avoiding async void
- Exception handling

### [Code Examples](references/code-examples.md)
Comprehensive code examples:
- Basic asynchronous tasks
- Parallel execution patterns
- Timeout and retry
- Advanced patterns

### [Anti-Patterns](references/anti-patterns.md)
Common mistakes to avoid:
- Blocking with .Result, .Wait()
- Missing error handling in fire-and-forget
- Not propagating CancellationToken

## Key Principles

1. **Async All the Way**: Maintain asynchronous call chain
2. **Always Support Cancellation**: CancellationToken is required for long-running tasks
3. **ConfigureAwait in Libraries**: Use ConfigureAwait(false) in library code
4. **No Async Suffix**: POCU Standard - Prohibit Async suffix
5. **No async void**: Prohibit async void except for event handlers
6. **Explicit Types**: Use explicit types instead of var
