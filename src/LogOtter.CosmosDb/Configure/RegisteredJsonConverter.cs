using Newtonsoft.Json;

namespace LogOtter.CosmosDb;

internal interface IRegisteredJsonConverter
{
    Type ConverterType { get; }
}

internal record RegisteredJsonConverter<T> : IRegisteredJsonConverter
    where T : JsonConverter
{
    public Type ConverterType => typeof(T);
};
