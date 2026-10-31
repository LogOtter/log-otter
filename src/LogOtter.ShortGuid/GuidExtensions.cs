// ReSharper disable CheckNamespace

using LogOtter.ShortGuid;

namespace System;

public static class GuidExtensions
{
    extension(Guid inputGuid)
    {
        public string ToShortString()
        {
            return ShortGuid.ToShortString(inputGuid);
        }

        public static string NewShortGuid()
        {
            return ShortGuid.NewShortGuid();
        }
    }

    extension(string guidString)
    {
        public bool IsValidShortGuid()
        {
            return ShortGuid.IsValidShortGuid(guidString);
        }
    }
}
