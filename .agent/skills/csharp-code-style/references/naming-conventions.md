# C# Naming Conventions Reference (POCU)

## Complete Naming Rules

### Types

| Type | Convention | Examples |
|------|------------|----------|
| Class | PascalCase | `PlayerManager`, `OrderService` |
| Struct | SPascalCase | `SUserID`, `SPlayerData`, `SVector3` |
| Record | PascalCase | `OrderDto`, `CustomerRecord` |
| readonly record struct | PascalCase (S 불필요) | `UserID`, `Point` |
| Interface | IPascalCase | `IDisposable`, `IOrderRepository` |
| Enum | EPascalCase | `EDirection`, `EOrderStatus` |
| Bit Flags Enum | EPascalCaseFlags | `EVisibilityFlags`, `EPermissionFlags` |
| Delegate | PascalCase | `EventHandler`, `OrderProcessor` |

### Members

| Member | Convention | Examples |
|--------|------------|----------|
| Public Method | PascalCase (동사+명사) | `GetCustomer`, `ProcessOrder` |
| Private Method | camelCase (동사+명사) | `getCustomer`, `processOrder` |
| Property | PascalCase | `FirstName`, `OrderID`, `IsEnabled` |
| Private Field | mPascalCase | `mOrderRepository`, `mRetryCount` |
| Private Bool Field | mbPascalCase | `mbIsEnabled`, `mbHasChildren` |
| Constant | ALL_CAPS | `MAX_RETRY_COUNT`, `DEFAULT_BUFFER_SIZE` |
| Static readonly | ALL_CAPS | `MY_CONST_OBJECT`, `DEFAULT_TIMEOUT` |
| Parameter | camelCase | `orderId`, `customerName` |
| Local Variable | camelCase | `totalAmount`, `currentIndex` |
| Local Bool Variable | bCamelCase | `bIsValid`, `bHasError` |

## Private Field Conventions

### Standard Fields

```csharp
public class OrderService
{
    // Private fields: m + PascalCase
    private readonly IOrderRepository mOrderRepository;
    private readonly ILogger mLogger;
    private int mProcessedCount;
    private string mLastError;

    // Collections
    private readonly List<Order> mOrders;
    private readonly Dictionary<int, Customer> mCustomerCache;
}
```

### Boolean Fields

```csharp
public class GameManager
{
    // Private boolean: mb + PascalCase
    private bool mbIsInitialized;
    private bool mbIsRunning;
    private bool mbHasError;
    private bool mbCanProcess;

    // Properties expose without prefix
    public bool IsInitialized => mbIsInitialized;
    public bool IsRunning => mbIsRunning;
}
```

## Method Naming

### Public Methods: PascalCase

```csharp
public class CustomerService
{
    // Verb + Noun pattern
    public Customer GetCustomer(int id) { }
    public void UpdateCustomer(Customer customer) { }
    public bool DeleteCustomer(int id) { }
    public List<Customer> FindCustomers(string query) { }

    // Boolean returns: Is/Can/Has/Should
    public bool IsValidCustomer(Customer customer) { }
    public bool CanProcess(Order order) { }
    public bool HasPermission(string action) { }
    public bool ShouldRetry(Exception ex) { }
}
```

### Private Methods: camelCase

```csharp
public class OrderProcessor
{
    public void ProcessOrder(Order order)
    {
        validateOrder(order);
        calculateTotal(order);
        saveOrder(order);
    }

    // Private methods: camelCase
    private void validateOrder(Order order)
    {
        Debug.Assert(order != null);
    }

    private decimal calculateTotal(Order order)
    {
        return sumItems(order.Items);
    }

    private decimal sumItems(List<OrderItem> items)
    {
        decimal total = 0;
        foreach (OrderItem item in items)
        {
            total += item.Price * item.Quantity;
        }
        return total;
    }

    private void saveOrder(Order order)
    {
        mRepository.Save(order);
    }
}
```

## Enum Conventions

### Standard Enum: E Prefix

```csharp
// All enums start with E
public enum EOrderStatus
{
    None,
    Pending,
    Processing,
    Completed,
    Cancelled
}

public enum EDirection
{
    None,
    North,
    South,
    East,
    West
}

public enum ELogLevel
{
    Debug,
    Info,
    Warning,
    Error,
    Fatal
}
```

### Bit Flags: EFlags Suffix

```csharp
[Flags]
public enum EVisibilityFlags
{
    None = 0,
    Visible = 1 << 0,      // 1
    Hidden = 1 << 1,       // 2
    Collapsed = 1 << 2,    // 4
    Transparent = 1 << 3   // 8
}

[Flags]
public enum EPermissionFlags
{
    None = 0,
    Read = 1 << 0,
    Write = 1 << 1,
    Execute = 1 << 2,
    Delete = 1 << 3,
    All = Read | Write | Execute | Delete
}

// Usage
EPermissionFlags permissions = EPermissionFlags.Read | EPermissionFlags.Write;
bool bCanRead = (permissions & EPermissionFlags.Read) != 0;
```

## Struct Conventions

### Standard Struct: S Prefix

```csharp
// Mutable struct: S prefix
public struct SUserID
{
    public int Value { get; set; }

    public SUserID(int value)
    {
        Value = value;
    }
}

public struct SVector3
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
}

public struct SPlayerData
{
    public string Name { get; set; }
    public int Level { get; set; }
    public int Experience { get; set; }
}
```

### Readonly Record Struct: No S Prefix

```csharp
// readonly record struct: S prefix 불필요 (C# 10.0)
public readonly record struct UserID(int Value);
public readonly record struct Point(int X, int Y);
public readonly record struct Money(decimal Amount, string Currency);
```

## Nullable Naming

### Nullable Parameters: OrNull Suffix

```csharp
public class AnimationService
{
    // Parameter that accepts null: OrNull suffix
    public Animation GetAnimation(string nameOrNull)
    {
        if (nameOrNull == null)
        {
            return mDefaultAnimation;
        }

        if (mAnimations.TryGetValue(nameOrNull, out Animation animation))
        {
            return animation;
        }

        return mDefaultAnimation;
    }

    // Multiple nullable parameters
    public void Configure(string pathOrNull, int? timeoutOrNull)
    {
        if (pathOrNull != null)
        {
            mPath = pathOrNull;
        }

        if (timeoutOrNull.HasValue)
        {
            mTimeout = timeoutOrNull.Value;
        }
    }
}
```

### Nullable Returns: OrNull Suffix

```csharp
public class CustomerRepository
{
    // Return that may be null: OrNull suffix
    public Customer GetCustomerOrNull(int id)
    {
        if (mCustomers.TryGetValue(id, out Customer customer))
        {
            return customer;
        }
        return null;
    }

    public string GetNameOrNull(int id)
    {
        Customer customer = GetCustomerOrNull(id);
        if (customer != null)
        {
            return customer.Name;
        }
        return null;
    }

    // Non-nullable return: no suffix, throws on not found
    public Customer GetCustomer(int id)
    {
        Customer customer = GetCustomerOrNull(id);
        Debug.Assert(customer != null, $"Customer {id} not found");
        return customer;
    }
}
```

## Special Suffixes

### Recursive Functions

```csharp
public class MathUtils
{
    // Recursive: Recursive suffix
    public int FactorialRecursive(int n)
    {
        Debug.Assert(n >= 0, "n must be non-negative");

        if (n <= 1)
        {
            return 1;
        }
        return n * FactorialRecursive(n - 1);
    }

    public int FibonacciRecursive(int n)
    {
        if (n <= 1)
        {
            return n;
        }
        return FibonacciRecursive(n - 1) + FibonacciRecursive(n - 2);
    }

    // Non-recursive version: no suffix
    public int Factorial(int n)
    {
        int result = 1;
        for (int i = 2; i <= n; i++)
        {
            result *= i;
        }
        return result;
    }
}
```

### Async Methods: No Async Suffix

```csharp
public class OrderService
{
    // [WRONG] Async suffix
    public async Task<Order> GetOrderAsync(int id) { }

    // [CORRECT] No Async suffix
    public async Task<Order> GetOrder(int id)
    {
        return await mRepository.Find(id);
    }

    public async Task SaveOrder(Order order)
    {
        await mRepository.Save(order);
    }

    public async Task<List<Order>> GetOrders(int customerId)
    {
        return await mRepository.FindByCustomer(customerId);
    }
}
```

## Abbreviations and Acronyms

### Two-Letter Acronyms: All Uppercase

```csharp
// Two letters: ALL CAPS
public int OrderID { get; private init; }
public string GetUIElement() { }
public class IOHandler { }

// At end of word
public int CustomerID { get; }
public string DBConnection { get; }
```

### Three+ Letter Acronyms: First Letter Only Caps

```csharp
// Three or more letters: First cap only
public class XmlParser { }
public string GetHtmlContent() { }
public class JsonSerializer { }
public int HttpStatusCode { get; }

// [WRONG]
public class XMLParser { }
public string GetHTMLContent { }
```

## Constants and Static Fields

```csharp
public class Configuration
{
    // Constants: ALL_CAPS with underscores
    private const int MAX_RETRY_COUNT = 3;
    private const int DEFAULT_BUFFER_SIZE = 4096;
    private const string DEFAULT_CONNECTION_STRING = "Server=localhost";

    // Public constants
    public const int MAX_NAME_LENGTH = 100;
    public const int MIN_PASSWORD_LENGTH = 8;

    // Static readonly: ALL_CAPS
    public static readonly TimeSpan DEFAULT_TIMEOUT = TimeSpan.FromSeconds(30);
    public static readonly Encoding DEFAULT_ENCODING = Encoding.UTF8;

    // Static readonly object
    public static readonly MyConfig MY_CONFIG_OBJECT = new MyConfig();
}
```

## Anti-Patterns

### Avoid These Naming Patterns

```csharp
// [WRONG] Hungarian notation for types (except m, mb, b)
private string strName;      // [WRONG]
private int iCount;          // [WRONG]
private List<Order> lstOrders;  // [WRONG]

// [CORRECT]
private string mName;
private int mCount;
private List<Order> mOrders;

// [WRONG] Underscore prefix (Microsoft style)
private readonly ILogger _logger;
private int _count;

// [CORRECT] m prefix (POCU style)
private readonly ILogger mLogger;
private int mCount;

// [WRONG] No prefix for private fields
private ILogger logger;
private int count;

// [CORRECT]
private ILogger mLogger;
private int mCount;

// [WRONG] Enum without E prefix
public enum OrderStatus { }
public enum Direction { }

// [CORRECT]
public enum EOrderStatus { }
public enum EDirection { }

// [WRONG] Struct without S prefix (non-readonly)
public struct UserID { }
public struct Vector3 { }

// [CORRECT]
public struct SUserID { }
public struct SVector3 { }
```
