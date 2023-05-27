using LogOtter.CosmosDb;
using Microsoft.Azure.Cosmos;

namespace CustomerApi.NonEventSourcedData.CustomerInterests;

public class SearchableInterestsProcessor : IChangeFeedProcessorChangeHandler<CustomerInterest>
{
    private readonly Container _searchableInterestContainer;

    public SearchableInterestsProcessor(CosmosContainer<SearchableInterest> searchableInterestContainer)
    {
        _searchableInterestContainer = searchableInterestContainer.Container;
    }

    public async Task ProcessChanges(IReadOnlyCollection<CustomerInterest> changes, CancellationToken cancellationToken)
    {
        foreach (var change in changes)
        {
            await _searchableInterestContainer.CreateItemAsync(
                new SearchableInterest(change.Id, change.Uri, change.Name),
                cancellationToken: cancellationToken
            );
        }
    }
}
