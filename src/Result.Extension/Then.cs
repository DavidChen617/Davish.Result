namespace Davish.Result;

/// <summary>
/// Provides fluent <c>Then</c> composition over <see cref="Result"/> and
/// <see cref="Result{TValue}"/>, including asynchronous variants that operate on tasks.
/// </summary>
public static class ThenExtension
{
    extension(Result result)
    {
        /// <summary>Executes <paramref name="next"/> when successful; otherwise propagates the current failure.</summary>
        /// <param name="next">The next operation to run on success.</param>
        /// <returns>The result of <paramref name="next"/>, or the current failure.</returns>
        public Result Then(Func<Result> next)
            => !result.IsSuccess ? result.Error : next();

        /// <summary>Executes <paramref name="next"/> when successful; otherwise propagates the current failure.</summary>
        /// <typeparam name="TNext">The value type produced by <paramref name="next"/>.</typeparam>
        /// <param name="next">The next operation to run on success.</param>
        /// <returns>The result of <paramref name="next"/>, or the current failure.</returns>
        public Result<TNext> Then<TNext>(Func<Result<TNext>> next) where TNext : notnull
            => !result.IsSuccess ? result.Error : next();

        /// <summary>Asynchronously executes <paramref name="next"/> when successful; otherwise propagates the current failure.</summary>
        /// <param name="next">The next asynchronous operation to run on success.</param>
        /// <returns>The result of <paramref name="next"/>, or the current failure.</returns>
        public async Task<Result> ThenAsync(Func<Task<Result>> next)
            => !result.IsSuccess ? result.Error : await next().ConfigureAwait(false);

        /// <summary>Asynchronously executes <paramref name="next"/> when successful; otherwise propagates the current failure.</summary>
        /// <typeparam name="TNext">The value type produced by <paramref name="next"/>.</typeparam>
        /// <param name="next">The next asynchronous operation to run on success.</param>
        /// <returns>The result of <paramref name="next"/>, or the current failure.</returns>
        public async Task<Result<TNext>> ThenAsync<TNext>(Func<Task<Result<TNext>>> next) where TNext : notnull
            => !result.IsSuccess ? result.Error : await next().ConfigureAwait(false);
    }

    extension<T>(Result<T> result) where T : notnull
    {
        /// <summary>Projects the successful value using <paramref name="next"/>; otherwise propagates the current failure.</summary>
        /// <typeparam name="TNext">The projected value type.</typeparam>
        /// <param name="next">The projection applied to the successful value.</param>
        /// <returns>A result carrying the projected value, or the current failure.</returns>
        public Result<TNext> Then<TNext>(Func<T, TNext> next) where TNext : notnull
            => !result.IsSuccess ? result.Error : next(result.Value);

        /// <summary>Binds the successful value to another result using <paramref name="next"/>; otherwise propagates the current failure.</summary>
        /// <typeparam name="TNext">The value type produced by <paramref name="next"/>.</typeparam>
        /// <param name="next">The operation applied to the successful value.</param>
        /// <returns>The result of <paramref name="next"/>, or the current failure.</returns>
        public Result<TNext> Then<TNext>(Func<T, Result<TNext>> next) where TNext : notnull
            => !result.IsSuccess ? result.Error : next(result.Value);

        /// <summary>Asynchronously projects the successful value using <paramref name="next"/>; otherwise propagates the current failure.</summary>
        /// <typeparam name="TNext">The projected value type.</typeparam>
        /// <param name="next">The asynchronous projection applied to the successful value.</param>
        /// <returns>A result carrying the projected value, or the current failure.</returns>
        public async Task<Result<TNext>> ThenAsync<TNext>(Func<T, Task<TNext>> next) where TNext : notnull
            => !result.IsSuccess ? result.Error : await next(result.Value).ConfigureAwait(false);

        /// <summary>Asynchronously binds the successful value to another result using <paramref name="next"/>; otherwise propagates the current failure.</summary>
        /// <typeparam name="TNext">The value type produced by <paramref name="next"/>.</typeparam>
        /// <param name="next">The asynchronous operation applied to the successful value.</param>
        /// <returns>The result of <paramref name="next"/>, or the current failure.</returns>
        public async Task<Result<TNext>> ThenAsync<TNext>(Func<T, Task<Result<TNext>>> next) where TNext : notnull
            => !result.IsSuccess ? result.Error : await next(result.Value).ConfigureAwait(false);
    }

    extension(Task<Result> task)
    {
        /// <summary>Awaits the task, then executes <paramref name="next"/> when successful; otherwise propagates the failure.</summary>
        /// <param name="next">The next operation to run on success.</param>
        /// <returns>The result of <paramref name="next"/>, or the awaited failure.</returns>
        public async Task<Result> Then(Func<Result> next)
            => (await task.ConfigureAwait(false)).Then(next);

        /// <summary>Awaits the task, then executes <paramref name="next"/> when successful; otherwise propagates the failure.</summary>
        /// <typeparam name="TNext">The value type produced by <paramref name="next"/>.</typeparam>
        /// <param name="next">The next operation to run on success.</param>
        /// <returns>The result of <paramref name="next"/>, or the awaited failure.</returns>
        public async Task<Result<TNext>> Then<TNext>(Func<Result<TNext>> next) where TNext : notnull
            => (await task.ConfigureAwait(false)).Then(next);

        /// <summary>Awaits the task, then asynchronously executes <paramref name="next"/> when successful; otherwise propagates the failure.</summary>
        /// <param name="next">The next asynchronous operation to run on success.</param>
        /// <returns>The result of <paramref name="next"/>, or the awaited failure.</returns>
        public async Task<Result> ThenAsync(Func<Task<Result>> next)
            => await (await task.ConfigureAwait(false)).ThenAsync(next).ConfigureAwait(false);

        /// <summary>Awaits the task, then asynchronously executes <paramref name="next"/> when successful; otherwise propagates the failure.</summary>
        /// <typeparam name="TNext">The value type produced by <paramref name="next"/>.</typeparam>
        /// <param name="next">The next asynchronous operation to run on success.</param>
        /// <returns>The result of <paramref name="next"/>, or the awaited failure.</returns>
        public async Task<Result<TNext>> ThenAsync<TNext>(Func<Task<Result<TNext>>> next) where TNext : notnull
            => await (await task.ConfigureAwait(false)).ThenAsync(next).ConfigureAwait(false);
    }

    extension<T>(Task<Result<T>> task) where T : notnull
    {
        /// <summary>Awaits the task, then projects the successful value using <paramref name="next"/>; otherwise propagates the failure.</summary>
        /// <typeparam name="TNext">The projected value type.</typeparam>
        /// <param name="next">The projection applied to the successful value.</param>
        /// <returns>A result carrying the projected value, or the awaited failure.</returns>
        public async Task<Result<TNext>> Then<TNext>(Func<T, TNext> next) where TNext : notnull
            => (await task.ConfigureAwait(false)).Then(next);

        /// <summary>Awaits the task, then binds the successful value to another result using <paramref name="next"/>; otherwise propagates the failure.</summary>
        /// <typeparam name="TNext">The value type produced by <paramref name="next"/>.</typeparam>
        /// <param name="next">The operation applied to the successful value.</param>
        /// <returns>The result of <paramref name="next"/>, or the awaited failure.</returns>
        public async Task<Result<TNext>> Then<TNext>(Func<T, Result<TNext>> next) where TNext : notnull
            => (await task.ConfigureAwait(false)).Then(next);

        /// <summary>Awaits the task, then asynchronously projects the successful value using <paramref name="next"/>; otherwise propagates the failure.</summary>
        /// <typeparam name="TNext">The projected value type.</typeparam>
        /// <param name="next">The asynchronous projection applied to the successful value.</param>
        /// <returns>A result carrying the projected value, or the awaited failure.</returns>
        public async Task<Result<TNext>> ThenAsync<TNext>(Func<T, Task<TNext>> next) where TNext : notnull
            => await (await task.ConfigureAwait(false)).ThenAsync(next).ConfigureAwait(false);

        /// <summary>Awaits the task, then asynchronously binds the successful value to another result using <paramref name="next"/>; otherwise propagates the failure.</summary>
        /// <typeparam name="TNext">The value type produced by <paramref name="next"/>.</typeparam>
        /// <param name="next">The asynchronous operation applied to the successful value.</param>
        /// <returns>The result of <paramref name="next"/>, or the awaited failure.</returns>
        public async Task<Result<TNext>> ThenAsync<TNext>(Func<T, Task<Result<TNext>>> next) where TNext : notnull
            => await (await task.ConfigureAwait(false)).ThenAsync(next).ConfigureAwait(false);
    }
}
