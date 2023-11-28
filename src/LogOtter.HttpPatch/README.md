# HttpPatch

Helpers for a simple HttpPatch using a simple model

If a property is present in the json it will be patched (even to null)

## Examples

* Patch Name to "Bob" and description (a nullable property of the resource) to null

```json
{
  "name": "Bob",
  "description": null
}
```

* Patch Name to "Bob" and description to "The best Bobertson"

```json
{
  "name": "Bob", 
  "description": "The best Bobertson"
}
```

* Patch Name to "Bob" and do not change the description

```json
{
  "name": "Bob"
}
```

## Adding To A Project

Simply use the `OptionallyPatched<T>` class in your patch requests!

### What about validation?

The DataAnnotation attributes will not work "through" the `OptionallyPatched<T>` class, however there is a workaround for this

A helper base record called `BasePatchRequest` will pass some attributes down to the underlying value when validating, if it is included in the patch

```c#
public record TestResourcePatchRequest(
    [RequiredIfPatched]
    [MinLengthIfPatched(2)]
    OptionallyPatched<string> Name
) : BasePatchRequest
```

Note that these wrappers `RequiredIfPatched` are not the actual data annotation attributes but do map 1:1, if you need one that is missing, feel free
to add them :)

### What about custom serialization?

`JsonConverter` attributes on the OptionallyPatched property will not work either, if you need custom serialization of an underlying value, either
load the converters globally or place them on the underlying type

Example:

```c#
[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter))]
[Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
public enum ResourceState
{
    Unpublished,
    Published
}
```

They will work as normal on nested properties, just not the top level `OptionallyPatched<T>` item
