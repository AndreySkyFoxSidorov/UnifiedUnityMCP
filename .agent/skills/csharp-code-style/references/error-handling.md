# C# Error Handling Reference (POCU)

## Debug.Assert 사용

### 모든 가정에 Assert 추가

```csharp
public class OrderService
{
    public void ProcessOrder(Order order)
    {
        // 모든 가정에 Debug.Assert 추가
        Debug.Assert(order != null, "Order cannot be null");
        Debug.Assert(order.Items.Count > 0, "Order must have items");
        Debug.Assert(order.CustomerID > 0, "Invalid customer ID");

        processInternal(order);
    }

    public decimal CalculateDiscount(Customer customer, decimal amount)
    {
        Debug.Assert(customer != null);
        Debug.Assert(amount >= 0, "Amount must be non-negative");
        Debug.Assert(customer.DiscountRate >= 0 && customer.DiscountRate <= 1,
            "Discount rate must be between 0 and 1");

        return amount * customer.DiscountRate;
    }
}
```

### Switch Default Case

```csharp
public string GetStatusMessage(EOrderStatus status)
{
    switch (status)
    {
        case EOrderStatus.Pending:
            return "Order is pending";
        case EOrderStatus.Processing:
            return "Processing order";
        case EOrderStatus.Completed:
            return "Order completed";
        case EOrderStatus.Cancelled:
            return "Order cancelled";
        default:
            // 도달 불가능한 default: Debug.Fail 사용
            Debug.Fail($"Unknown order status: {status}");
            return "Unknown";
    }
}

// Switch expression
public string GetMessage(ELogLevel level)
{
    return level switch
    {
        ELogLevel.Debug => "DEBUG",
        ELogLevel.Info => "INFO",
        ELogLevel.Warning => "WARNING",
        ELogLevel.Error => "ERROR",
        _ => throw new ArgumentOutOfRangeException(nameof(level))
    };
}
```

## 경계에서만 예외 처리

### 경계 함수 (Public API)

```csharp
// 경계 함수: 외부 데이터 검증 및 예외 처리
public class OrderController
{
    private readonly OrderService mOrderService;

    // 경계: 외부 입력 검증
    public ActionResult CreateOrder(OrderRequest request)
    {
        // 경계에서 모든 검증 수행
        if (request == null)
        {
            return BadRequest("Request is required");
        }

        if (string.IsNullOrEmpty(request.CustomerName))
        {
            return BadRequest("Customer name is required");
        }

        if (request.Items == null || request.Items.Count == 0)
        {
            return BadRequest("Order must have at least one item");
        }

        foreach (OrderItemRequest item in request.Items)
        {
            if (item.Quantity <= 0)
            {
                return BadRequest("Item quantity must be positive");
            }
        }

        // 검증 통과 후 내부 함수 호출 (예외 없음)
        Order order = mOrderService.CreateOrder(request);
        return Ok(order);
    }
}
```

### 내부 함수 (예외 금지)

```csharp
// 내부 함수: 예외를 던지지 않음
public class OrderService
{
    // 내부 함수: 경계에서 이미 검증됨
    public Order CreateOrder(OrderRequest request)
    {
        // Assert로 가정 확인 (Debug 빌드에서만)
        Debug.Assert(request != null);
        Debug.Assert(!string.IsNullOrEmpty(request.CustomerName));
        Debug.Assert(request.Items != null && request.Items.Count > 0);

        Order order = new Order(request.CustomerName);
        foreach (OrderItemRequest item in request.Items)
        {
            Debug.Assert(item.Quantity > 0);
            order.AddItem(item.ProductID, item.Quantity);
        }

        mRepository.Save(order);
        return order;
    }

    // 내부 helper: 예외 없음
    private decimal calculateTotal(List<OrderItem> items)
    {
        Debug.Assert(items != null);

        decimal total = 0;
        foreach (OrderItem item in items)
        {
            total += item.Price * item.Quantity;
        }
        return total;
    }
}
```

## Null 처리 규칙

### Public 함수: null 매개변수 미허용

```csharp
public class CustomerService
{
    // Public: null 허용하지 않음 (경계에서 검증)
    public void UpdateCustomer(Customer customer)
    {
        Debug.Assert(customer != null, "Customer cannot be null");
        // 경계에서 이미 검증되었으므로 null 체크 불필요

        mRepository.Update(customer);
    }

    // null 허용 시: OrNull suffix 필수
    public void UpdateCustomerOrNull(Customer customerOrNull)
    {
        if (customerOrNull == null)
        {
            return;
        }

        mRepository.Update(customerOrNull);
    }
}
```

### Public 함수: null 반환 회피

```csharp
public class OrderRepository
{
    // [WRONG] AVOID: null 반환
    public Order GetOrder(int id)
    {
        return mOrders.FirstOrDefault(o => o.ID == id);  // null 가능
    }

    // [CORRECT] OPTION 1: OrNull suffix로 명시
    public Order GetOrderOrNull(int id)
    {
        Order order;
        if (mOrders.TryGetValue(id, out order))
        {
            return order;
        }
        return null;
    }

    // [CORRECT] OPTION 2: 존재 확인 후 예외 (경계 함수에서)
    public Order GetOrder(int id)
    {
        Order order = GetOrderOrNull(id);
        Debug.Assert(order != null, $"Order {id} not found");
        return order;
    }

    // [CORRECT] OPTION 3: bool 반환 패턴
    public bool TryGetOrder(int id, out Order order)
    {
        return mOrders.TryGetValue(id, out order);
    }
}
```

## 유효성 검증 패턴

### 경계에서 검증 (Controller/Handler)

```csharp
public class ProductController
{
    public ActionResult UpdatePrice(int productId, UpdatePriceRequest request)
    {
        // 경계에서 모든 검증
        if (productId <= 0)
        {
            return BadRequest("Invalid product ID");
        }

        if (request == null)
        {
            return BadRequest("Request body is required");
        }

        if (request.NewPrice < 0)
        {
            return BadRequest("Price cannot be negative");
        }

        if (request.NewPrice > 1000000)
        {
            return BadRequest("Price exceeds maximum allowed");
        }

        // 검증 통과 - 내부 서비스 호출
        bool bSuccess = mProductService.UpdatePrice(productId, request.NewPrice);

        if (!bSuccess)
        {
            return NotFound($"Product {productId} not found");
        }

        return Ok();
    }
}
```

### 내부 서비스 (Assert Only)

```csharp
public class ProductService
{
    // 내부: 검증 대신 Assert
    public bool UpdatePrice(int productId, decimal newPrice)
    {
        Debug.Assert(productId > 0);
        Debug.Assert(newPrice >= 0);
        Debug.Assert(newPrice <= 1000000);

        Product product = mRepository.GetProductOrNull(productId);
        if (product == null)
        {
            return false;
        }

        product.Price = newPrice;
        mRepository.Save(product);
        return true;
    }
}
```

## async void 금지

```csharp
public class EventProcessor
{
    // [WRONG] async void 사용 금지
    public async void ProcessEvent(Event evt)
    {
        await handleEvent(evt);
    }

    // [CORRECT] async Task 사용
    public async Task ProcessEvent(Event evt)
    {
        Debug.Assert(evt != null);
        await handleEvent(evt);
    }

    // [CAUTION] EXCEPTION: 이벤트 핸들러만 async void 허용
    private async void OnButtonClick(object sender, EventArgs e)
    {
        try
        {
            await ProcessClick();
        }
        catch (Exception ex)
        {
            mLogger.Error(ex, "Button click failed");
        }
    }
}
```

## 에러 로깅 패턴

```csharp
public class OrderProcessor
{
    private readonly ILogger mLogger;

    public async Task<bool> ProcessOrder(Order order)
    {
        Debug.Assert(order != null);

        try
        {
            await validatePayment(order);
            await reserveInventory(order);
            await sendConfirmation(order);

            mLogger.Info($"Order {order.ID} processed successfully");
            return true;
        }
        catch (PaymentException ex)
        {
            mLogger.Error(ex, $"Payment failed for order {order.ID}");
            return false;
        }
        catch (InventoryException ex)
        {
            mLogger.Error(ex, $"Inventory reservation failed for order {order.ID}");
            await rollbackPayment(order);
            return false;
        }
        catch (Exception ex)
        {
            mLogger.Error(ex, $"Unexpected error processing order {order.ID}");
            await rollbackAll(order);
            return false;
        }
    }
}
```

## Try 패턴

```csharp
public class Parser
{
    // Try 패턴: out 매개변수 별도 선언
    public bool TryParse(string input, out Order order)
    {
        order = null;

        if (string.IsNullOrEmpty(input))
        {
            return false;
        }

        try
        {
            order = JsonSerializer.Deserialize<Order>(input);
            return order != null;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    // 사용
    public void ProcessInput(string input)
    {
        Order order;
        if (TryParse(input, out order))
        {
            Process(order);
        }
        else
        {
            mLogger.Warning("Failed to parse order");
        }
    }
}
```

## 프로젝트 설정

### Release 빌드 경고 → 오류

```xml
<!-- .csproj -->
<PropertyGroup Condition="'$(Configuration)'=='Release'">
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
```

### Nullable Context 설정

```xml
<!-- .csproj -->
<PropertyGroup>
    <Nullable>enable</Nullable>
</PropertyGroup>
```

### Implicit Global Using 금지

```xml
<!-- .csproj -->
<PropertyGroup>
    <ImplicitUsings>disable</ImplicitUsings>
</PropertyGroup>
```

## Summary

| Rule | Description |
|------|-------------|
| Debug.Assert | 모든 가정에 사용 |
| Debug.Fail | 도달 불가능한 코드에 사용 |
| 경계 검증 | Public API에서만 예외 처리 |
| 내부 함수 | 예외 던지지 않음, Assert만 사용 |
| null 매개변수 | Public에서 미허용 (OrNull suffix로 예외) |
| null 반환 | 회피 (OrNull suffix 또는 Try 패턴) |
| async void | 금지 (이벤트 핸들러 제외) |
| TreatWarningsAsErrors | Release 빌드에서 활성화 |
