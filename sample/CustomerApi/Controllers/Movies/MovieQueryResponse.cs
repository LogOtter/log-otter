using CustomerApi.Uris;

namespace CustomerApi.Controllers.Movies;

public record MovieQueryResponse(MovieUri MovieUri, string Name);
