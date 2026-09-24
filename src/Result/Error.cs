namespace Davish.Result;

/// <summary>
/// Represents an error with a code, a description, a category, and optional per-field messages.
/// </summary>
public record Error
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> EmptyFields =
        new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>Gets the machine-readable error code.</summary>
    public string Code { get; }

    /// <summary>Gets the human-readable error description.</summary>
    public string Description { get; }

    /// <summary>Gets the category of this error. Defaults to <see cref="ErrorType.Validation"/>.</summary>
    public ErrorType Type { get; } = ErrorType.Validation;

    /// <summary>
    /// Gets the errors that caused this error, in the order they were added. Empty when this error has no
    /// recorded cause. Use <see cref="CausedBy(Error)"/>/<see cref="CausedBy(IReadOnlyList{Error})"/> to derive
    /// a new <see cref="Error"/> with additional causes; this instance is never modified in place.
    /// </summary>
    public IReadOnlyList<Error> Causes { get; private init; } = [];

    /// <summary>
    /// Gets the direct cause of this error, or <see langword="null"/> if there is none. When multiple causes
    /// exist, this is the first one — mirrors <see cref="System.Exception.InnerException"/>.
    /// </summary>
    public Error? InnerError => Causes.Count > 0 ? Causes[0] : null;

    /// <summary>
    /// Gets the per-field error messages, keyed by field name. Typically used for validation errors.
    /// Immutable: use <see cref="AddFieldError(string, string)"/>/<see cref="AddFieldError(string, ICollection{string})"/>
    /// to derive a new <see cref="Error"/> with additional messages; this instance is never modified in place.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Fields { get; private init; } = EmptyFields;

    /// <summary>
    /// Initializes a new instance of the <see cref="Error"/> record.
    /// </summary>
    /// <param name="code">The machine-readable error code.</param>
    /// <param name="description">The human-readable error description.</param>
    public Error(string code, string description)
    {
        Code = code;
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Error"/> record with a specific category.
    /// </summary>
    /// <param name="code">The machine-readable error code.</param>
    /// <param name="description">The human-readable error description.</param>
    /// <param name="type">The error category.</param>
    public Error(string code, string description, ErrorType type) : this(code, description) => Type = type;

    /// <summary>
    /// Initializes a new instance of the <see cref="Error"/> record with per-field messages.
    /// </summary>
    /// <param name="code">The machine-readable error code.</param>
    /// <param name="description">The human-readable error description.</param>
    /// <param name="fields">The per-field error messages, keyed by field name. Copied defensively.</param>
    /// <exception cref="ArgumentNullException"><paramref name="fields"/> is <see langword="null"/>.</exception>
    public Error(string code, string description, Dictionary<string, List<string>> fields) : this(code, description)
    {
        if (fields is null)
            throw new ArgumentNullException(nameof(fields));

        var copy = new Dictionary<string, IReadOnlyList<string>>();
        foreach (var entry in fields)
            copy[entry.Key] = [.. entry.Value];

        Fields = copy;
    }

    /// <summary>Gets the canonical "no error" instance, used by successful results.</summary>
    public static Error None { get; } = new(string.Empty, string.Empty, ErrorType.None);

    /// <summary>Gets the canonical error for a <see langword="null"/> value.</summary>
    public static Error NullValue { get; } = new("Null.Value", "Null value was provided", ErrorType.NullValue);

    /// <summary>
    /// Implicitly converts an <see cref="Error"/> into a failed <see cref="Result"/>.
    /// </summary>
    /// <param name="error">The error to wrap.</param>
    public static implicit operator Result(Error error) => Result.Failure(error);

    /// <summary>
    /// Returns a new <see cref="Error"/> with a single error message added for the specified field.
    /// Creates the field entry if it does not exist yet. This instance is left unchanged.
    /// </summary>
    /// <param name="field">The field name.</param>
    /// <param name="message">The error message to add.</param>
    /// <returns>A new <see cref="Error"/> carrying the added message.</returns>
    public Error AddFieldError(string field, string message) => AddFieldError(field, (ICollection<string>)[message]);

    /// <summary>
    /// Returns a new <see cref="Error"/> with multiple error messages added for the specified field.
    /// Creates the field entry if it does not exist yet, even when <paramref name="messages"/> is empty.
    /// This instance is left unchanged.
    /// </summary>
    /// <param name="field">The field name.</param>
    /// <param name="messages">The error messages to add.</param>
    /// <returns>A new <see cref="Error"/> carrying the added messages.</returns>
    public Error AddFieldError(string field, ICollection<string> messages)
    {
        var fields = new Dictionary<string, IReadOnlyList<string>>();
        foreach (var entry in Fields)
            fields[entry.Key] = entry.Value;

        fields[field] = fields.TryGetValue(field, out var existing) ? [.. existing, .. messages] : [.. messages];

        return this with { Fields = fields };
    }

    /// <summary>
    /// Returns a new <see cref="Error"/> with a single cause added. This instance is left unchanged.
    /// </summary>
    /// <param name="cause">The error that caused this error.</param>
    /// <returns>A new <see cref="Error"/> carrying the added cause.</returns>
    public Error CausedBy(Error cause) => CausedBy([cause]);

    /// <summary>
    /// Returns a new <see cref="Error"/> with multiple causes added. This instance is left unchanged.
    /// </summary>
    /// <param name="causes">The errors that caused this error.</param>
    /// <returns>A new <see cref="Error"/> carrying the added causes.</returns>
    public Error CausedBy(IReadOnlyList<Error> causes) => this with { Causes = [.. Causes, .. causes] };

    /// <summary>
    /// Walks down the cause chain to find the root cause. Mirrors
    /// <see cref="System.AggregateException.GetBaseException"/>: stops at a node with zero causes, or one with
    /// more than one cause (an aggregation point).
    /// </summary>
    /// <returns>The root <see cref="Error"/>.</returns>
    public Error GetRootCause() => Causes.Count switch
    {
        1 => Causes[0].GetRootCause(),
        _ => this
    };

    /// <inheritdoc/>
    public virtual bool Equals(Error? other) =>
        other is not null
        && EqualityContract == other.EqualityContract
        && Code == other.Code
        && Description == other.Description
        && Type == other.Type
        && FieldsEqual(Fields, other.Fields)
        && Causes.SequenceEqual(other.Causes);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + EqualityContract.GetHashCode();
            hash = hash * 31 + Code.GetHashCode();
            hash = hash * 31 + Description.GetHashCode();
            hash = hash * 31 + Type.GetHashCode();

            foreach (var entry in Fields.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            {
                hash = hash * 31 + entry.Key.GetHashCode();

                foreach (var message in entry.Value)
                    hash = hash * 31 + message.GetHashCode();
            }

            foreach (var cause in Causes)
                hash = hash * 31 + cause.GetHashCode();


            return hash;
        }
    }

    private static bool FieldsEqual(
        IReadOnlyDictionary<string, IReadOnlyList<string>> a,
        IReadOnlyDictionary<string, IReadOnlyList<string>> b)
    {
        if (a.Count != b.Count)
            return false;

        foreach (var entry in a)
        {
            if (!b.TryGetValue(entry.Key, out var otherMessages) || !entry.Value.SequenceEqual(otherMessages))
                return false;
        }

        return true;
    }
}
