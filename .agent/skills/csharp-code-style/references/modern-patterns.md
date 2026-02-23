# Modern C# Patterns Reference (C# 9.0)

## Platform Compatibility

### Unity Runtime Limitations

> **IMPORTANT**: Unity uses Mono/IL2CPP runtime which does NOT include `System.Runtime.CompilerServices.IsExternalInit`.
> This means `init` accessors cause compile error CS0518 in Unity projects.

| Feature | .NET 5+ | Unity |
|---------|---------|-------|
| `init` accessor | [OK] Supported | [N/A] Not available |
| `private init` | [OK] Supported | [N/A] Not available |
| `required` (C# 11) | [OK] Supported | [N/A] Not available |
| Records | [OK] Full support | [LIMIT] Without `init` |
| Pattern matching | [OK] Full support | [OK] Supported |

**Unity Alternatives**:
```csharp
// [WRONG] COMPILE ERROR in Unity
public string Name { get; private init; }

// [CORRECT] Unity Option 1: private set
public string Name { get; private set; }

// [CORRECT] Unity Option 2: readonly field + property (true immutability)
private readonly string mName;
public string Name => mName;
```

---

## C# 9.0 Features

### Init-Only Properties

> **Note**: `init` accessors are NOT available in Unity. Use `private set` or readonly fields instead.

```csharp
// private init - constructor-only assignment (recommended for .NET 5+)
public class Customer
{
    public int ID { get; private init; }
    public string Name { get; private init; }
    public string Email { get; private init; }

    public Customer(int id, string name, string email)
    {
        ID = id;
        Name = name;
        Email = email;
    }
}

// public init - 객체 초기자에서 설정 가능
public class OrderDto
{
    public int ID { get; init; }
    public string CustomerName { get; init; }
    public decimal TotalAmount { get; init; }
}

// Usage
OrderDto order = new OrderDto
{
    ID = 1,
    CustomerName = "John",
    TotalAmount = 99.99m
};
// order.ID = 2;  // [WRONG] Compile error - cannot modify after init
```

### Records

```csharp
// Positional record
public record OrderDto(int ID, string CustomerName, decimal TotalAmount);

// Record with additional members
public record CustomerDto(string FirstName, string LastName, string Email)
{
    public string FullName => FirstName + " " + LastName;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

// with-expression for copies
OrderDto original = new OrderDto(1, "John", 100m);
OrderDto modified = original with { TotalAmount = 150m };

// Value equality
OrderDto order1 = new OrderDto(1, "John", 100m);
OrderDto order2 = new OrderDto(1, "John", 100m);
bool bEqual = order1 == order2;  // true
```

### Pattern Matching Enhancements

```csharp
// Relational patterns
public string GetGrade(int score)
{
    return score switch
    {
        >= 90 => "A",
        >= 80 => "B",
        >= 70 => "C",
        >= 60 => "D",
        _ => "F"
    };
}

// Logical patterns (and, or, not)
public bool IsValidAge(int age)
{
    return age is >= 0 and <= 120;
}

public string Categorize(int value)
{
    return value switch
    {
        < 0 => "Negative",
        0 => "Zero",
        > 0 and < 10 => "Single digit",
        >= 10 and < 100 => "Double digit",
        _ => "Large"
    };
}

// Type pattern with property pattern
public decimal CalculateDiscount(object customer)
{
    return customer switch
    {
        Customer { IsPremium: true, OrderCount: > 100 } => 0.20m,
        Customer { IsPremium: true } => 0.10m,
        Customer { OrderCount: > 50 } => 0.05m,
        _ => 0m
    };
}
```

### Target-Typed new (금지)

```csharp
// [WRONG] Target-typed new 사용 금지
List<Order> orders = new();
Dictionary<string, int> cache = new();
Customer customer = new("John");

// [CORRECT] 명시적 타입 사용
List<Order> orders = new List<Order>();
Dictionary<string, int> cache = new Dictionary<string, int>();
Customer customer = new Customer("John");
```

## Prohibited Patterns

### var Keyword (금지)

```csharp
// [WRONG] var 사용 금지
var order = GetOrder(1);
var customers = new List<Customer>();
var result = Calculate();

// [CORRECT] 명시적 타입 선언
Order order = GetOrder(1);
List<Customer> customers = new List<Customer>();
int result = Calculate();

// [CAUTION] EXCEPTION: Anonymous types only
var anonymousObj = new { Name = "John", Age = 30 };

// [CAUTION] EXCEPTION: IEnumerable with complex LINQ
var query = from c in customers
            where c.Age > 18
            select new { c.Name, c.Email };
```

### Null Coalescing Operator (금지)

```csharp
// [WRONG] ?? 연산자 사용 금지
string name = inputName ?? "Default";
int count = nullableCount ?? 0;
Order order = GetOrderOrNull(id) ?? new Order();

// [CORRECT] 명시적 null 검사
string name;
if (inputName != null)
{
    name = inputName;
}
else
{
    name = "Default";
}

int count;
if (nullableCount.HasValue)
{
    count = nullableCount.Value;
}
else
{
    count = 0;
}

Order order = GetOrderOrNull(id);
if (order == null)
{
    order = new Order();
}
```

### Using Declaration (금지)

```csharp
// [WRONG] using 선언 (C# 8.0) 사용 금지
using FileStream stream = new FileStream(path, FileMode.Open);
DoSomething(stream);

// [CORRECT] using 문 사용
using (FileStream stream = new FileStream(path, FileMode.Open))
{
    DoSomething(stream);
}

// 여러 리소스
using (FileStream input = new FileStream(inputPath, FileMode.Open))
using (FileStream output = new FileStream(outputPath, FileMode.Create))
{
    CopyStream(input, output);
}
```

### Inline Out Declaration (금지)

```csharp
// [WRONG] 인라인 out 선언 금지
if (int.TryParse(input, out int result))
{
    Process(result);
}

if (mCache.TryGetValue(key, out Customer customer))
{
    return customer;
}

// [CORRECT] 별도 라인에 선언
int result;
if (int.TryParse(input, out result))
{
    Process(result);
}

Customer customer;
if (mCache.TryGetValue(key, out customer))
{
    return customer;
}
```

### Async Suffix (금지)

```csharp
// [WRONG] Async 접미사 사용 금지
public async Task<Order> GetOrderAsync(int id);
public async Task SaveOrderAsync(Order order);
public async Task<List<Customer>> FindCustomersAsync(string query);

// [CORRECT] Async 접미사 없음
public async Task<Order> GetOrder(int id)
{
    return await mRepository.Find(id);
}

public async Task SaveOrder(Order order)
{
    await mRepository.Save(order);
}

public async Task<List<Customer>> FindCustomers(string query)
{
    return await mRepository.Search(query);
}
```

## C# 10.0 Features (Optional)

### File-Scoped Namespaces

```csharp
// C# 10.0: 파일 범위 namespace 사용 권장
namespace MyCompany.Orders.Services;

public class OrderService
{
    // No extra indentation
    private readonly IOrderRepository mRepository;

    public OrderService(IOrderRepository repository)
    {
        mRepository = repository;
    }
}
```

### Readonly Record Struct

```csharp
// C# 10.0: readonly record struct로 강타입 사용
public readonly record struct UserID(int Value);
public readonly record struct OrderID(int Value);
public readonly record struct Money(decimal Amount, string Currency);

// Usage - type safety
public Order GetOrder(OrderID id)  // OrderID, not just int
{
    return mRepository.Find(id.Value);
}

// Cannot accidentally pass wrong ID type
UserID userId = new UserID(1);
OrderID orderId = new OrderID(1);
// GetOrder(userId);  // ❌ Compile error
GetOrder(orderId);    // ✅ OK
```

## Switch Expression

```csharp
// Switch expression (C# 8.0+) - 사용 가능
public string GetStatusMessage(EOrderStatus status)
{
    return status switch
    {
        EOrderStatus.None => "No status",
        EOrderStatus.Pending => "Order is pending",
        EOrderStatus.Processing => "Order is being processed",
        EOrderStatus.Completed => "Order completed",
        EOrderStatus.Cancelled => "Order was cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };
}

// With property patterns
public decimal GetShippingCost(Order order)
{
    return order switch
    {
        { IsPriority: true, Weight: > 10 } => 25.00m,
        { IsPriority: true } => 15.00m,
        { Weight: > 20 } => 20.00m,
        { Weight: > 10 } => 12.00m,
        _ => 5.00m
    };
}

// Multiple conditions
public string Categorize(Customer customer)
{
    return (customer.OrderCount, customer.TotalSpent) switch
    {
        ( > 100, > 10000m) => "VIP",
        ( > 50, > 5000m) => "Gold",
        ( > 10, > 1000m) => "Silver",
        _ => "Bronze"
    };
}
```

## Object Initializer (주의)

```csharp
// [CAUTION] 객체 초기자: 일반적으로 회피
// [WRONG] AVOID in most cases
Order order = new Order
{
    CustomerID = 1,
    Total = 100m,
    Status = EOrderStatus.Pending
};

// [CORRECT] PREFER constructor
Order order = new Order(1, 100m, EOrderStatus.Pending);

// [CORRECT] EXCEPTION: required/init 사용 시 허용 (C# 11+)
public class OrderDto
{
    public required int CustomerID { get; init; }
    public required decimal Total { get; init; }
}

OrderDto dto = new OrderDto
{
    CustomerID = 1,
    Total = 100m
};
```

## Inline Lambda (한 줄만 허용)

```csharp
// [CORRECT] Single line lambda
List<Order> pending = orders.Where(o => o.Status == EOrderStatus.Pending).ToList();
int maxAge = customers.Max(c => c.Age);
Customer found = customers.FirstOrDefault(c => c.ID == id);

// [WRONG] Multi-line inline lambda
List<Order> filtered = orders.Where(o =>
{
    if (o.Status == EOrderStatus.Pending)
    {
        return o.Total > 100;
    }
    return false;
}).ToList();

// [CORRECT] Extract to method for complex logic
List<Order> filtered = orders.Where(shouldIncludeOrder).ToList();

private bool shouldIncludeOrder(Order order)
{
    if (order.Status == EOrderStatus.Pending)
    {
        return order.Total > 100;
    }
    return false;
}
```

## Recommended Patterns Summary

| Feature | Status | Unity | Notes |
|---------|--------|-------|-------|
| `private init` | [OK] Recommended | [N/A] | .NET 5+ only |
| Records | [OK] Allowed | [LIMIT] Limited | Without `init` in Unity |
| Pattern matching | [OK] Allowed | [OK] | Includes switch expression |
| File-scoped namespace | [OK] Recommended | [OK] | C# 10.0 |
| readonly record struct | [OK] Recommended | [OK] | C# 10.0, strong typing |
| `var` | [NO] Prohibited | [NO] | Except anonymous/IEnumerable |
| `??` | [NO] Prohibited | [NO] | Use explicit null check |
| `new()` | [NO] Prohibited | [NO] | Use explicit type |
| using declaration | [NO] Prohibited | [NO] | Use using statement |
| inline out | [NO] Prohibited | [NO] | Declare on separate line |
| Async suffix | [NO] Prohibited | [NO] | No suffix |
| Object initializer | [CAUTION] | [CAUTION] | Only with required/init |
| Multi-line lambda | [NO] Prohibited | [NO] | Extract to method |
