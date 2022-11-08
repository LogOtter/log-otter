﻿using Microsoft.Azure.Cosmos;

namespace LogOtter.EventStore.CosmosDb;

public class CosmosContainer<TDocument>
{
    public Container Container { get; }

    public CosmosContainer(Container container)
    {
        Container = container;
    }
}