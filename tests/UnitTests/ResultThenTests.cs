using Davish.Result;

namespace UnitTests;

public class ResultThenTests
{
    private static readonly Error SomeError = new("Some.Code", "Some description");

    [Fact]
    public void GivenSuccessResult_WhenThen_ThenRunsNext()
    {
        var invoked = false;

        var result = Result.Success().Then(() =>
        {
            invoked = true;
            return Result.Success();
        });

        Assert.True(invoked);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void GivenFailedResult_WhenThen_ThenShortCircuits()
    {
        var invoked = false;

        var result = Result.Failure(SomeError).Then(() =>
        {
            invoked = true;
            return Result.Success();
        });

        Assert.False(invoked);
        Assert.Equal(SomeError, result.Error);
    }

    [Fact]
    public void GivenSuccessResult_WhenThenValue_ThenRunsNext()
    {
        var result = Result.Success().Then(() => Result.Success(42));

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void GivenFailedResult_WhenThenValue_ThenShortCircuits()
    {
        var result = Result.Failure(SomeError).Then(() => Result.Success(42));

        Assert.False(result.IsSuccess);
        Assert.Equal(SomeError, result.Error);
    }

    [Fact]
    public async Task GivenSuccessResult_WhenThenAsync_ThenRunsNext()
    {
        var invoked = false;

        var result = await Result.Success().ThenAsync(() =>
        {
            invoked = true;
            return Task.FromResult(Result.Success());
        });

        Assert.True(invoked);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GivenFailedResult_WhenThenAsync_ThenShortCircuits()
    {
        var result = await Result.Failure(SomeError).ThenAsync(() => Task.FromResult(Result.Success()));

        Assert.Equal(SomeError, result.Error);
    }

    [Fact]
    public async Task GivenSuccessResult_WhenThenAsyncValue_ThenRunsNext()
    {
        var result = await Result.Success().ThenAsync(() => Task.FromResult(Result.Success(42)));

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task GivenFailedResult_WhenThenAsyncValue_ThenShortCircuits()
    {
        var result = await Result.Failure(SomeError).ThenAsync(() => Task.FromResult(Result.Success(42)));

        Assert.False(result.IsSuccess);
        Assert.Equal(SomeError, result.Error);
    }


    [Fact]
    public void GivenSuccessValue_WhenThenMap_ThenMapsTheValue()
    {
        var result = Result.Success(21).Then(x => x * 2);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void GivenFailedValue_WhenThenMap_ThenShortCircuits()
    {
        var result = Result.Failure<int>(SomeError).Then(x => x * 2);

        Assert.False(result.IsSuccess);
        Assert.Equal(SomeError, result.Error);
    }

    [Fact]
    public void GivenSuccessValue_WhenThenBind_ThenReturnsNextResult()
    {
        var result = Result.Success(21).Then(x => Result.Success(x.ToString()));

        Assert.True(result.IsSuccess);
        Assert.Equal("21", result.Value);
    }

    [Fact]
    public void GivenFailedValue_WhenThenBind_ThenShortCircuits()
    {
        var result = Result.Failure<int>(SomeError).Then(x => Result.Success(x.ToString()));

        Assert.False(result.IsSuccess);
        Assert.Equal(SomeError, result.Error);
    }

    [Fact]
    public async Task GivenSuccessValue_WhenThenAsyncMap_ThenMapsTheValue()
    {
        var result = await Result.Success(21).ThenAsync(x => Task.FromResult(x * 2));

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task GivenFailedValue_WhenThenAsyncMap_ThenShortCircuits()
    {
        var result = await Result.Failure<int>(SomeError).ThenAsync(x => Task.FromResult(x * 2));

        Assert.False(result.IsSuccess);
        Assert.Equal(SomeError, result.Error);
    }

    [Fact]
    public async Task GivenSuccessValue_WhenThenAsyncBind_ThenReturnsNextResult()
    {
        var result = await Result.Success(21).ThenAsync(x => Task.FromResult(Result.Success(x.ToString())));

        Assert.True(result.IsSuccess);
        Assert.Equal("21", result.Value);
    }

    [Fact]
    public async Task GivenFailedValue_WhenThenAsyncBind_ThenShortCircuits()
    {
        var result = await Result.Failure<int>(SomeError).ThenAsync(x => Task.FromResult(Result.Success(x.ToString())));

        Assert.False(result.IsSuccess);
        Assert.Equal(SomeError, result.Error);
    }


    [Fact]
    public async Task GivenSuccessTaskResult_WhenThen_ThenRunsNext()
    {
        var invoked = false;
        Task<Result> task = Task.FromResult(Result.Success());

        var result = await task.Then(() =>
        {
            invoked = true;
            return Result.Success();
        });

        Assert.True(invoked);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GivenFailedTaskResult_WhenThen_ThenShortCircuits()
    {
        var invoked = false;
        Task<Result> task = Task.FromResult(Result.Failure(SomeError));

        var result = await task.Then(() =>
        {
            invoked = true;
            return Result.Success();
        });

        Assert.False(invoked);
        Assert.Equal(SomeError, result.Error);
    }

    [Fact]
    public async Task GivenSuccessTaskResult_WhenThenValue_ThenRunsNext()
    {
        Task<Result> task = Task.FromResult(Result.Success());

        var result = await task.Then(() => Result.Success(42));

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task GivenFailedTaskResult_WhenThenValue_ThenShortCircuits()
    {
        Task<Result> task = Task.FromResult(Result.Failure(SomeError));

        var result = await task.Then(() => Result.Success(42));

        Assert.False(result.IsSuccess);
        Assert.Equal(SomeError, result.Error);
    }

    [Fact]
    public async Task GivenSuccessTaskResult_WhenThenAsync_ThenRunsNext()
    {
        Task<Result> task = Task.FromResult(Result.Success());

        var result = await task.ThenAsync(() => Task.FromResult(Result.Success()));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task GivenFailedTaskResult_WhenThenAsync_ThenShortCircuits()
    {
        Task<Result> task = Task.FromResult(Result.Failure(SomeError));

        var result = await task.ThenAsync(() => Task.FromResult(Result.Success()));

        Assert.Equal(SomeError, result.Error);
    }

    [Fact]
    public async Task GivenSuccessTaskResult_WhenThenAsyncValue_ThenRunsNext()
    {
        Task<Result> task = Task.FromResult(Result.Success());

        var result = await task.ThenAsync(() => Task.FromResult(Result.Success(42)));

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task GivenFailedTaskResult_WhenThenAsyncValue_ThenShortCircuits()
    {
        Task<Result> task = Task.FromResult(Result.Failure(SomeError));

        var result = await task.ThenAsync(() => Task.FromResult(Result.Success(42)));

        Assert.False(result.IsSuccess);
        Assert.Equal(SomeError, result.Error);
    }


    [Fact]
    public async Task GivenSuccessTaskValue_WhenThenMap_ThenMapsTheValue()
    {
        Task<Result<int>> task = Task.FromResult(Result.Success(21));

        var result = await task.Then(x => x + 1);

        Assert.True(result.IsSuccess);
        Assert.Equal(22, result.Value);
    }

    [Fact]
    public async Task GivenFailedTaskValue_WhenThenMap_ThenShortCircuits()
    {
        Task<Result<int>> task = Task.FromResult(Result.Failure<int>(SomeError));

        var result = await task.Then(x => x + 1);

        Assert.False(result.IsSuccess);
        Assert.Equal(SomeError, result.Error);
    }

    [Fact]
    public async Task GivenSuccessTaskValue_WhenThenBind_ThenReturnsNextResult()
    {
        Task<Result<int>> task = Task.FromResult(Result.Success(21));

        var result = await task.Then(x => Result.Success(x.ToString()));

        Assert.True(result.IsSuccess);
        Assert.Equal("21", result.Value);
    }

    [Fact]
    public async Task GivenFailedTaskValue_WhenThenBind_ThenShortCircuits()
    {
        Task<Result<int>> task = Task.FromResult(Result.Failure<int>(SomeError));

        var result = await task.Then(x => Result.Success(x.ToString()));

        Assert.False(result.IsSuccess);
        Assert.Equal(SomeError, result.Error);
    }

    [Fact]
    public async Task GivenSuccessTaskValue_WhenThenAsyncMap_ThenMapsTheValue()
    {
        Task<Result<int>> task = Task.FromResult(Result.Success(21));

        var result = await task.ThenAsync(x => Task.FromResult(x + 1));

        Assert.True(result.IsSuccess);
        Assert.Equal(22, result.Value);
    }

    [Fact]
    public async Task GivenFailedTaskValue_WhenThenAsyncMap_ThenShortCircuits()
    {
        Task<Result<int>> task = Task.FromResult(Result.Failure<int>(SomeError));

        var result = await task.ThenAsync(x => Task.FromResult(x + 1));

        Assert.False(result.IsSuccess);
        Assert.Equal(SomeError, result.Error);
    }

    [Fact]
    public async Task GivenSuccessTaskValue_WhenThenAsyncBind_ThenReturnsNextResult()
    {
        Task<Result<int>> task = Task.FromResult(Result.Success(21));

        var result = await task.ThenAsync(x => Task.FromResult(Result.Success(x.ToString())));

        Assert.True(result.IsSuccess);
        Assert.Equal("21", result.Value);
    }

    [Fact]
    public async Task GivenFailedTaskValue_WhenThenAsyncBind_ThenShortCircuits()
    {
        Task<Result<int>> task = Task.FromResult(Result.Failure<int>(SomeError));

        var result = await task.ThenAsync(x => Task.FromResult(Result.Success(x.ToString())));

        Assert.False(result.IsSuccess);
        Assert.Equal(SomeError, result.Error);
    }


    [Fact]
    public async Task GivenValidOrder_WhenPlaced_ThenReturnsConfirmation()
    {
        var request = new OrderRequest(CustomerId: 1, Product: "Book", Quantity: 2);

        var result = await ValidateRequest(request)
            .ThenAsync(CheckCustomerAsync)
            .ThenAsync(CheckStockAsync)
            .ThenAsync(ChargeAsync)
            .ThenAsync(CreateOrderAsync)
            .Then(Confirm);

        Assert.True(result.IsSuccess);
        Assert.Equal(1001, result.Value.OrderId);
    }

    [Fact]
    public async Task GivenInvalidQuantity_WhenPlaced_ThenFailsAtValidationAndSkipsRest()
    {
        var stockChecked = false;
        var request = new OrderRequest(CustomerId: 1, Product: "Book", Quantity: 0);

        var result = await ValidateRequest(request)
            .ThenAsync(CheckCustomerAsync)
            .ThenAsync(req =>
            {
                stockChecked = true;
                return CheckStockAsync(req);
            })
            .ThenAsync(ChargeAsync)
            .ThenAsync(CreateOrderAsync)
            .Then(Confirm);

        Assert.False(stockChecked);
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Order.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task GivenDeclinedPayment_WhenPlaced_ThenFailsAtChargeAndSkipsRest()
    {
        var orderCreated = false;
        var request = new OrderRequest(CustomerId: 1, Product: "Book", Quantity: 2);
        var declined = new Error("Payment.Declined", "The card was declined", ErrorType.BadRequest);

        var result = await ValidateRequest(request)
            .ThenAsync(CheckCustomerAsync)
            .ThenAsync(CheckStockAsync)
            .ThenAsync(_ => Task.FromResult(Result.Failure<Payment>(declined)))
            .ThenAsync(payment =>
            {
                orderCreated = true;
                return CreateOrderAsync(payment);
            })
            .Then(Confirm);

        Assert.False(orderCreated);
        Assert.False(result.IsSuccess);
        Assert.Equal(declined, result.Error);
    }

    [Fact]
    public async Task GivenOrderCreationFailure_WhenPlaced_ThenFailsAndSkipsConfirmation()
    {
        var confirmed = false;
        var request = new OrderRequest(CustomerId: 1, Product: "Book", Quantity: 2);
        var dbError = new Error("Order.CreateFailed", "Could not persist the order", ErrorType.ServiceUnavailable);

        var result = await ValidateRequest(request)
            .ThenAsync(CheckCustomerAsync)
            .ThenAsync(CheckStockAsync)
            .ThenAsync(ChargeAsync)
            .ThenAsync(_ => Task.FromResult(Result.Failure<Order>(dbError)))
            .Then(order =>
            {
                confirmed = true;
                return Confirm(order);
            });

        Assert.False(confirmed);
        Assert.False(result.IsSuccess);
        Assert.Equal(dbError, result.Error);
    }

    private sealed record OrderRequest(int CustomerId, string Product, int Quantity);

    private sealed record Payment(string Reference, OrderRequest Request);

    private sealed record Order(int Id, string Product, int Quantity);

    private sealed record OrderConfirmation(int OrderId, string Message);

    private static Result<OrderRequest> ValidateRequest(OrderRequest request)
        => request.Quantity > 0
            ? Result.Success(request)
            : Result.Failure<OrderRequest>(new Error("Order.Invalid", "Quantity must be positive", ErrorType.Validation));

    private static Task<Result<OrderRequest>> CheckCustomerAsync(OrderRequest request)
        => Task.FromResult(request.CustomerId > 0
            ? Result.Success(request)
            : Result.Failure<OrderRequest>(new Error("Customer.NotFound", "Customer was not found", ErrorType.NotFound)));

    private static Task<Result<OrderRequest>> CheckStockAsync(OrderRequest request)
        => Task.FromResult(request.Product != "OutOfStock"
            ? Result.Success(request)
            : Result.Failure<OrderRequest>(new Error("Stock.Insufficient", "Item is out of stock", ErrorType.BadRequest)));

    private static Task<Result<Payment>> ChargeAsync(OrderRequest request)
        => Task.FromResult(Result.Success(new Payment($"PAY-{request.CustomerId}", request)));

    private static Task<Result<Order>> CreateOrderAsync(Payment payment)
        => Task.FromResult(Result.Success(new Order(1001, payment.Request.Product, payment.Request.Quantity)));

    private static Result<OrderConfirmation> Confirm(Order order)
        => Result.Success(new OrderConfirmation(order.Id, $"Order {order.Id} for {order.Quantity}x {order.Product} confirmed"));
}
