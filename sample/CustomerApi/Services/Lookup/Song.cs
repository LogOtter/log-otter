namespace CustomerApi.Services.Lookup;

public record Song(string Id, string Name, string Genre) : LookupItem(Id, Name, "songs");
