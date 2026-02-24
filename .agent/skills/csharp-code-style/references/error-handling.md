# C# Error Handling Reference (POCU)

## Debug.Assert 

###   Assert 

```csharp
public class OrderService
{
    public void ProcessOrder(Order order)
    {
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

##   

###   (Public API)

```csharp
public class OrderController
{
    private readonly OrderService mOrderService;

    public ActionResult CreateOrder(OrderRequest request)
    {
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

        Order order = mOrderService.CreateOrder(request);
        return Ok(order);
    }
}
```

###   ( Prohibited)

```csharp
public class OrderService
{
    public Order CreateOrder(OrderRequest request)
    {
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

## Null  

### Public : null  

```csharp
public class CustomerService
{
    public void UpdateCustomer(Customer customer)
    {
        Debug.Assert(customer != null, "Customer cannot be null");

        mRepository.Update(customer);
    }

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

### Public : null  

```csharp
public class OrderRepository
{
    public Order GetOrder(int id)
    {
        return mOrders.FirstOrDefault(o => o.ID == id);  
    }

    public Order GetOrderOrNull(int id)
    {
        Order order;
        if (mOrders.TryGetValue(id, out order))
        {
            return order;
        }
        return null;
    }

    public Order GetOrder(int id)
    {
        Order order = GetOrderOrNull(id);
        Debug.Assert(order != null, $"Order {id} not found");
        return order;
    }

    public bool TryGetOrder(int id, out Order order)
    {
        return mOrders.TryGetValue(id, out order);
    }
}
```

##   

###   (Controller/Handler)

```csharp
public class ProductController
{
    public ActionResult UpdatePrice(int productId, UpdatePriceRequest request)
    {
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

        bool bSuccess = mProductService.UpdatePrice(productId, request.NewPrice);

        if (!bSuccess)
        {
            return NotFound($"Product {productId} not found");
        }

        return Ok();
    }
}
```

###   (Assert Only)

```csharp
public class ProductService
{
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

## async void Prohibited

```csharp
public class EventProcessor
{
    public async void ProcessEvent(Event evt)
    {
        await handleEvent(evt);
    }

    public async Task ProcessEvent(Event evt)
    {
        Debug.Assert(evt != null);
        await handleEvent(evt);
    }

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

##   

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

## Try 

```csharp
public class Parser
{
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

##  

### Release   → 

```xml
<!-- .csproj -->
<PropertyGroup Condition="'$(Configuration)'=='Release'">
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
```

### Nullable Context 

```xml
<!-- .csproj -->
<PropertyGroup>
    <Nullable>enable</Nullable>
</PropertyGroup>
```

### Implicit Global Using Prohibited

```xml
<!-- .csproj -->
<PropertyGroup>
    <ImplicitUsings>disable</ImplicitUsings>
</PropertyGroup>
```

## Summary

| Rule | Description |
|------|-------------|
| Debug.Assert |    |
| Debug.Fail |     |
|   | Public API   |
|   |   , Assert  |
| null  | Public  (OrNull suffix ) |
| null  |  (OrNull suffix  Try ) |
| async void | Prohibited (  ) |
| TreatWarningsAsErrors | Release   |
