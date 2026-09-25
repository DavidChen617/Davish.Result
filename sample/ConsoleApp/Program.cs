using Davish.Result;

Console.WriteLine("=== Davish.Result sample ===");

// 1. Result<T>: expected failures (like "not found") are values, not exceptions.
PrintOrder(FindOrder(orderId: 1));
PrintOrder(FindOrder(orderId: 999));

// 2. Error.CausedBy / GetRootCause: preserve the low-level cause while surfacing a higher-level error,
// without needing an exception to carry it.
var dbError = new Error("Db.Timeout", "The database connection timed out.");
var repositoryError = new Error("Order.LoadFailed", "Could not load the order.", ErrorType.Unexpected)
    .CausedBy(dbError);

Console.WriteLine();
Console.WriteLine($"Error:      {repositoryError.Code} - {repositoryError.Description}");
Console.WriteLine($"InnerError: {repositoryError.InnerError?.Code}");
Console.WriteLine($"Root cause: {repositoryError.GetRootCause().Code}");

static Result<Order> FindOrder(int orderId) =>
    orderId == 1
        ? Result.Success(new Order(orderId, 100m))
        : Result.Failure<Order>(new Error("Order.NotFound", $"Order {orderId} was not found.", ErrorType.NotFound));

static void PrintOrder(Result<Order> result) => Console.WriteLine(result.IsSuccess
    ? $"Order #{result.Value.Id}: ${result.Value.Amount}"
    : $"Failed: {result.Error.Code} - {result.Error.Description}");

record Order(int Id, decimal Amount);
