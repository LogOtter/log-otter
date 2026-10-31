# ShortGuid

A library to create string identifiers that are as unique as GUIDs.

## Usage

### Generate a new short GUID

Call the `NewShortGuid()` method.

```c#
var id = Guid.NewShortGuid();
// e.g. cft9D0fwBXzBkKRyLLWWKhsB
```

or

```c#
var id = ShortGuid.NewShortGuid();
```

### Convert a GUID to a ShortGuid

Call the `ToShortString()` extension method on a GUID.

```c#
var guid = Guid.NewGuid();
var id = guid.ToShortString();
```

or

```c#
var guid = Guid.NewGuid();
var id = ShortGuid.ToShortString(guid);
```
