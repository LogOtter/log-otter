namespace LogOtter.JsonHal;

public static class PageHelpers
{
    public static int CalculatePageCount(int pageSize, int totalItems)
    {
        return Math.Max((int)Math.Ceiling((decimal)totalItems / pageSize), 1);
    }
}
