using LogOtter.HttpPatch;

namespace CustomerApi;

public static class OptionallyPatchedExtensions
{
    public static bool IsIncludedInPatchAndIsDifferentFrom<T>(this OptionallyPatched<T> optionallyPatched, T value)
    {
        if (!optionallyPatched.IsIncludedInPatch)
        {
            return false;
        }

        return !EqualityComparer<T>.Default.Equals(optionallyPatched.Value, value);
    }
}
