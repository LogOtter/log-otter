﻿namespace LogOtter.CosmosDb;

public record AutoProvisionSettings(bool Enabled, int? Throughput = null);
