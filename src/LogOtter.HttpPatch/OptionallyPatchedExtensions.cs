namespace LogOtter.HttpPatch;

public static class OptionallyPatchedExtensions
{
    public static bool IsIncludedInPatchAndDifferentFrom<T>(
        this OptionallyPatched<T> optionallyPatched,
        T value,
        IEqualityComparer<T>? equalityComparer = null
    )
    {
        if (!optionallyPatched.IsIncludedInPatch)
        {
            return false;
        }

        var comparer = equalityComparer ?? EqualityComparer<T>.Default;

        return !comparer.Equals(optionallyPatched.Value, value);
    }

    public static bool IsIncludedInPatchAndDifferentFrom<TRequest, TValue>(
        this OptionallyPatched<TRequest> optionallyPatched,
        TValue value,
        Func<TRequest, TValue> valueSelector,
        IEqualityComparer<TValue>? equalityComparer = null
    )
    {
        if (!optionallyPatched.IsIncludedInPatch)
        {
            return false;
        }

        var comparerValue = valueSelector(optionallyPatched.Value!);

        var comparer = equalityComparer ?? EqualityComparer<TValue>.Default;

        return !comparer.Equals(comparerValue, value);
    }

    public static T GetValueIfIncludedOrDefault<T>(this OptionallyPatched<T> optionallyPatched, T defaultValue)
    {
        return optionallyPatched.IsIncludedInPatch ? optionallyPatched.Value! : defaultValue;
    }

    public static TValue GetValueIfIncludedOrDefault<TRequest, TValue>(
        this OptionallyPatched<TRequest> optionallyPatched,
        TValue defaultValue,
        Func<TRequest, TValue> valueSelector
    )
    {
        return optionallyPatched.IsIncludedInPatch ? valueSelector(optionallyPatched.Value!) : defaultValue;
    }
}
