namespace CustomerApi.Services.Lookup;

public record Movie(string Id, string Name, int AverageRating) : LookupItem(Id, Name, "movies");
