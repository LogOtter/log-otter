using LogOtter.CosmosDb;
using Microsoft.Azure.Cosmos;

namespace CustomerApi.Services.CustomerInterests;

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
            if (change is Movie movie)
            {
                await _searchableInterestContainer.CreateItemAsync(
                    new SearchableInterest(movie.Id, movie.MovieUri.Uri, movie.Name),
                    cancellationToken: cancellationToken
                );
            }
            else if (change is Song song)
            {
                await _searchableInterestContainer.CreateItemAsync(
                    new SearchableInterest(song.Id, song.SongUri.Uri, song.Name),
                    cancellationToken: cancellationToken
                );
            }
        }
    }
}
