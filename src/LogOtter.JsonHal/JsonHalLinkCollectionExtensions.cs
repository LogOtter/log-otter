namespace LogOtter.JsonHal;

public static partial class JsonHalLinkCollectionExtensions
{
    public static void AddPagedLinks(
        this JsonHalLinkCollection collection,
        int currentPage,
        int totalPages,
        Func<int, string> pageUrlAction
    )
    {
        collection.AddSelfLink(pageUrlAction(currentPage));

        collection.AddFirstLink(pageUrlAction(1));
        
        if (currentPage > 1)
        {
            collection.AddPrevLink(pageUrlAction(currentPage - 1));
        }
        
        if (currentPage < totalPages)
        {
            collection.AddNextLink(pageUrlAction(currentPage + 1));
        }

        collection.AddLastLink(pageUrlAction(totalPages));
    }
}