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
}
