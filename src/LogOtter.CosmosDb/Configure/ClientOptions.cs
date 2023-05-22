using System.Collections.ObjectModel;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Azure.Cosmos;

namespace LogOtter.CosmosDb;

/// <summary>
/// Wrapper for <see cref="CosmosClientOptions"/> with our defaults and that only exposes settings that are supported by LogOtter.
/// </summary>
/// <remarks>Of note, is that switching the serializer is not currently possible.</remarks>
public class ClientOptions
{
    internal CosmosClientOptions UnderlyingOptions { get; private set; }

    /// <inheritdoc cref="CosmosClientOptions.ApplicationName"/>
    public string ApplicationName
    {
        get => UnderlyingOptions.ApplicationName;
        set => UnderlyingOptions.ApplicationName = value;
    }

    /// <inheritdoc cref="CosmosClientOptions.ApplicationRegion"/>
    public string ApplicationRegion
    {
        get => UnderlyingOptions.ApplicationRegion;
        set => UnderlyingOptions.ApplicationRegion = value;
    }

    /// <inheritdoc cref="CosmosClientOptions.ApplicationPreferredRegions"/>
    public IReadOnlyList<string> ApplicationPreferredRegions
    {
        get => UnderlyingOptions.ApplicationPreferredRegions;
        set => UnderlyingOptions.ApplicationPreferredRegions = value;
    }

    /// <inheritdoc cref="CosmosClientOptions.GatewayModeMaxConnectionLimit"/>
    public int GatewayModeMaxConnectionLimit
    {
        get => UnderlyingOptions.GatewayModeMaxConnectionLimit;
        set => UnderlyingOptions.GatewayModeMaxConnectionLimit = value;
    }

    /// <inheritdoc cref="CosmosClientOptions.RequestTimeout"/>
    public TimeSpan RequestTimeout
    {
        get => UnderlyingOptions.RequestTimeout;
        set => UnderlyingOptions.RequestTimeout = value;
    }

    /// <inheritdoc cref="CosmosClientOptions.TokenCredentialBackgroundRefreshInterval"/>
    public TimeSpan? TokenCredentialBackgroundRefreshInterval
    {
        get => UnderlyingOptions.TokenCredentialBackgroundRefreshInterval;
        set => UnderlyingOptions.TokenCredentialBackgroundRefreshInterval = value;
    }

    /// <inheritdoc cref="CosmosClientOptions.CustomHandlers"/>
    public Collection<RequestHandler> CustomHandlers => UnderlyingOptions.CustomHandlers;

    /// <inheritdoc cref="CosmosClientOptions.ConnectionMode"/>
    public ConnectionMode ConnectionMode
    {
        get => UnderlyingOptions.ConnectionMode;
        set => UnderlyingOptions.ConnectionMode = value;
    }

    /// <inheritdoc cref="CosmosClientOptions.ConsistencyLevel"/>
    public ConsistencyLevel? ConsistencyLevel
    {
        get => UnderlyingOptions.ConsistencyLevel;
        set => UnderlyingOptions.ConsistencyLevel = value;
    }

    /// <inheritdoc cref="CosmosClientOptions.MaxRetryAttemptsOnRateLimitedRequests"/>
    public int? MaxRetryAttemptsOnRateLimitedRequests
    {
        get => UnderlyingOptions.MaxRetryAttemptsOnRateLimitedRequests;
        set => UnderlyingOptions.MaxRetryAttemptsOnRateLimitedRequests = value;
    }

    /// <inheritdoc cref="CosmosClientOptions.MaxRetryWaitTimeOnRateLimitedRequests"/>
    public TimeSpan? MaxRetryWaitTimeOnRateLimitedRequests
    {
        get => UnderlyingOptions.MaxRetryWaitTimeOnRateLimitedRequests;
        set => UnderlyingOptions.MaxRetryWaitTimeOnRateLimitedRequests = value;
    }

    /// <inheritdoc cref="CosmosClientOptions.EnableContentResponseOnWrite"/>
    public bool? EnableContentResponseOnWrite
    {
        get => UnderlyingOptions.EnableContentResponseOnWrite;
        set => UnderlyingOptions.EnableContentResponseOnWrite = value;
    }

    /// <inheritdoc cref="CosmosClientOptions.IdleTcpConnectionTimeout"/>
    public TimeSpan? IdleTcpConnectionTimeout
    {
        get => UnderlyingOptions.IdleTcpConnectionTimeout;
        set => UnderlyingOptions.IdleTcpConnectionTimeout = value;
    }

    /// <inheritdoc cref="CosmosClientOptions.OpenTcpConnectionTimeout"/>
    public TimeSpan? OpenTcpConnectionTimeout
    {
        get => UnderlyingOptions.OpenTcpConnectionTimeout;
        set => UnderlyingOptions.OpenTcpConnectionTimeout = value;
    }

    /// <inheritdoc cref="CosmosClientOptions.MaxRequestsPerTcpConnection"/>
    public int? MaxRequestsPerTcpConnection
    {
        get => UnderlyingOptions.MaxRequestsPerTcpConnection;
        set => UnderlyingOptions.MaxRequestsPerTcpConnection = value;
    }

    /// <inheritdoc cref="CosmosClientOptions.MaxTcpConnectionsPerEndpoint"/>
    public int? MaxTcpConnectionsPerEndpoint
    {
        get => UnderlyingOptions.MaxTcpConnectionsPerEndpoint;
        set => UnderlyingOptions.MaxTcpConnectionsPerEndpoint = value;
    }

    /// <inheritdoc cref="CosmosClientOptions.PortReuseMode"/>
    public PortReuseMode? PortReuseMode
    {
        get => UnderlyingOptions.PortReuseMode;
        set => UnderlyingOptions.PortReuseMode = value;
    }

    /// <inheritdoc cref="CosmosClientOptions.WebProxy"/>
    public IWebProxy WebProxy
    {
        get => UnderlyingOptions.WebProxy;
        set => UnderlyingOptions.WebProxy = value;
    }

    /// <inheritdoc cref="CosmosClientOptions.SerializerOptions"/>
    public CosmosSerializationOptions SerializerOptions { get; set; }

    /// <inheritdoc cref="CosmosClientOptions.LimitToEndpoint"/>
    public bool LimitToEndpoint
    {
        get => UnderlyingOptions.LimitToEndpoint;
        set => UnderlyingOptions.LimitToEndpoint = value;
    }

    /// <inheritdoc cref="CosmosClientOptions.AllowBulkExecution"/>
    public bool AllowBulkExecution
    {
        get => UnderlyingOptions.AllowBulkExecution;
        set => UnderlyingOptions.AllowBulkExecution = value;
    }

    /// <inheritdoc cref="CosmosClientOptions.EnableTcpConnectionEndpointRediscovery"/>
    public bool EnableTcpConnectionEndpointRediscovery
    {
        get => UnderlyingOptions.EnableTcpConnectionEndpointRediscovery;
        set => UnderlyingOptions.EnableTcpConnectionEndpointRediscovery = value;
    }

    /// <inheritdoc cref="CosmosClientOptions.HttpClientFactory"/>
    public Func<HttpClient> HttpClientFactory
    {
        get => UnderlyingOptions.HttpClientFactory;
        set => UnderlyingOptions.HttpClientFactory = value;
    }

    /// <inheritdoc cref="CosmosClientOptions.ServerCertificateCustomValidationCallback"/>
    public Func<X509Certificate2, X509Chain, SslPolicyErrors, bool> ServerCertificateCustomValidationCallback
    {
        get => UnderlyingOptions.ServerCertificateCustomValidationCallback;
        set => UnderlyingOptions.ServerCertificateCustomValidationCallback = value;
    }

    public ClientOptions()
    {
        UnderlyingOptions = new CosmosClientOptions();
        SerializerOptions = new CosmosSerializationOptions
        {
            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
            Indented = true,
            IgnoreNullValues = false
        };
    }
}
