using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XForge.Sync.Http.Extensions;

namespace XForge.Sync.Http.Tests;

/// <summary>
/// Tests for <see cref="HttpSyncTransport"/>.
/// </summary>
public sealed class HttpSyncTransportTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private static HttpSyncTransportOptions CreateOptions(string baseUrl = "https://sync.example.com") =>
        new() { BaseUrl = baseUrl, MaxRetries = 0 };

    private static Mock<ILogger<HttpSyncTransport>> CreateLogger() =>
        new() { DefaultValue = DefaultValue.Mock };

    private static HttpClient CreateHttpClient(HttpMessageHandler handler) =>
        new(handler);

    // -------------------------------------------------------------------
    // SendAsync — Success
    // -------------------------------------------------------------------

    [Fact]
    public async Task SendAsync_Success_ReturnsSyncResponse()
    {
        // Arrange
        SyncResponse expectedResponse = new()
        {
            IsSuccess = true,
            ServerVersion = 42,
            Changes =
            [
                new TrackedChange
                {
                    Key = "item-1",
                    EntityType = "Product",
                    ChangeType = ChangeType.Update,
                    Version = 5,
                    TrackedAt = DateTime.UtcNow
                }
            ]
        };

        MockHttpHandler handler = new(expectedResponse);
        HttpSyncTransport sut = new(
            CreateHttpClient(handler),
            Options.Create(CreateOptions()),
            CreateLogger().Object);

        SyncRequest request = new()
        {
            ClientId = "test-client",
            LastServerVersion = 40,
            Changes = []
        };

        // Act
        SyncResponse result = await sut.SendAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ServerVersion.Should().Be(42);
        result.Changes.Should().HaveCount(1);
        result.Changes[0].Key.Should().Be("item-1");
        handler.LastRequestUri.Should().NotBeNull();
        handler.LastRequestUri!.AbsolutePath.Should().Be("/api/sync");
    }

    [Fact]
    public async Task SendAsync_WithChanges_SerializesRequestCorrectly()
    {
        // Arrange
        SyncResponse expectedResponse = new() { IsSuccess = true, ServerVersion = 1 };
        MockHttpHandler handler = new(expectedResponse);
        HttpSyncTransport sut = new(
            CreateHttpClient(handler),
            Options.Create(CreateOptions()),
            CreateLogger().Object);

        SyncRequest request = new()
        {
            ClientId = "client-1",
            LastServerVersion = 0,
            Changes =
            [
                new TrackedChange
                {
                    Key = "item-1",
                    EntityType = "Order",
                    ChangeType = ChangeType.Create,
                    Data = """{"id":1,"total":100}""",
                    Version = 1,
                    TrackedAt = DateTime.UtcNow
                },
                new TrackedChange
                {
                    Key = "item-2",
                    EntityType = "Order",
                    ChangeType = ChangeType.Delete,
                    Version = 2,
                    TrackedAt = DateTime.UtcNow
                }
            ]
        };

        // Act
        SyncResponse result = await sut.SendAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        handler.LastRequestBody.Should().NotBeNullOrEmpty();

        // Verify the request body can be deserialized back
        SyncRequest? deserialized = JsonSerializer.Deserialize<SyncRequest>(
            handler.LastRequestBody!, JsonOptions);
        deserialized.Should().NotBeNull();
        deserialized!.ClientId.Should().Be("client-1");
        deserialized.Changes.Should().HaveCount(2);
        deserialized.Changes[0].ChangeType.Should().Be(ChangeType.Create);
        deserialized.Changes[1].ChangeType.Should().Be(ChangeType.Delete);
    }

    // -------------------------------------------------------------------
    // SendAsync — Server Error (5xx) with retry
    // -------------------------------------------------------------------

    [Fact]
    public async Task SendAsync_ServerError_RetriesAndReturnsFailure()
    {
        // Arrange
        MockHttpHandler handler = new(statusCode: HttpStatusCode.InternalServerError);
        HttpSyncTransportOptions options = CreateOptions();
        options.MaxRetries = 2;
        options.RetryBaseDelay = TimeSpan.FromMilliseconds(1); // Fast for tests

        HttpSyncTransport sut = new(
            CreateHttpClient(handler),
            Options.Create(options),
            CreateLogger().Object);

        SyncRequest request = new() { ClientId = "test", LastServerVersion = 0, Changes = [] };

        // Act
        SyncResponse result = await sut.SendAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("InternalServerError");
        handler.RequestCount.Should().Be(3); // 1 initial + 2 retries
    }

    // -------------------------------------------------------------------
    // SendAsync — Client Error (4xx) without retry
    // -------------------------------------------------------------------

    [Fact]
    public async Task SendAsync_ClientError_DoesNotRetry()
    {
        // Arrange
        MockHttpHandler handler = new(statusCode: HttpStatusCode.BadRequest);
        HttpSyncTransportOptions options = CreateOptions();
        options.MaxRetries = 3;

        HttpSyncTransport sut = new(
            CreateHttpClient(handler),
            Options.Create(options),
            CreateLogger().Object);

        SyncRequest request = new() { ClientId = "test", LastServerVersion = 0, Changes = [] };

        // Act
        SyncResponse result = await sut.SendAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("BadRequest");
        handler.RequestCount.Should().Be(1); // No retries for 4xx
    }

    // -------------------------------------------------------------------
    // SendAsync — Timeout
    // -------------------------------------------------------------------

    [Fact]
    public async Task SendAsync_Timeout_RetriesAndReturnsFailure()
    {
        // Arrange
        MockHttpHandler handler = new(throwTimeout: true);
        HttpSyncTransportOptions options = CreateOptions();
        options.MaxRetries = 1;
        options.RetryBaseDelay = TimeSpan.FromMilliseconds(1);

        HttpSyncTransport sut = new(
            CreateHttpClient(handler),
            Options.Create(options),
            CreateLogger().Object);

        SyncRequest request = new() { ClientId = "test", LastServerVersion = 0, Changes = [] };

        // Act
        SyncResponse result = await sut.SendAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("timed out");
        handler.RequestCount.Should().Be(2); // 1 initial + 1 retry
    }

    // -------------------------------------------------------------------
    // SendAsync — Null response body
    // -------------------------------------------------------------------

    [Fact]
    public async Task SendAsync_NullResponseBody_ReturnsFailure()
    {
        // Arrange
        MockHttpHandler handler = new(responseBody: null);
        HttpSyncTransport sut = new(
            CreateHttpClient(handler),
            Options.Create(CreateOptions()),
            CreateLogger().Object);

        SyncRequest request = new() { ClientId = "test", LastServerVersion = 0, Changes = [] };

        // Act
        SyncResponse result = await sut.SendAsync(request);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid JSON");
    }

    // -------------------------------------------------------------------
    // SendAsync — Custom endpoint
    // -------------------------------------------------------------------

    [Fact]
    public async Task SendAsync_CustomEndpoint_UsesCorrectUrl()
    {
        // Arrange
        SyncResponse expectedResponse = new() { IsSuccess = true };
        MockHttpHandler handler = new(expectedResponse);
        HttpSyncTransportOptions options = CreateOptions();
        options.SyncEndpoint = "/custom/sync/v2";

        HttpSyncTransport sut = new(
            CreateHttpClient(handler),
            Options.Create(options),
            CreateLogger().Object);

        SyncRequest request = new() { ClientId = "test", LastServerVersion = 0, Changes = [] };

        // Act
        await sut.SendAsync(request);

        // Assert
        handler.LastRequestUri.Should().NotBeNull();
        handler.LastRequestUri!.AbsolutePath.Should().Be("/custom/sync/v2");
    }

    // -------------------------------------------------------------------
    // SendAsync — Argument validation
    // -------------------------------------------------------------------

    [Fact]
    public async Task SendAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        HttpSyncTransport sut = new(
            CreateHttpClient(new MockHttpHandler(new SyncResponse { IsSuccess = true })),
            Options.Create(CreateOptions()),
            CreateLogger().Object);

        // Act
        Func<Task> act = () => sut.SendAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    // -------------------------------------------------------------------
    // IsAvailableAsync — Available
    // -------------------------------------------------------------------

    [Fact]
    public async Task IsAvailableAsync_ServerAvailable_ReturnsTrue()
    {
        // Arrange
        MockHttpHandler handler = new(statusCode: HttpStatusCode.OK);
        HttpSyncTransport sut = new(
            CreateHttpClient(handler),
            Options.Create(CreateOptions()),
            CreateLogger().Object);

        // Act
        bool result = await sut.IsAvailableAsync();

        // Assert
        result.Should().BeTrue();
        handler.LastRequestUri.Should().NotBeNull();
        handler.LastRequestUri!.AbsolutePath.Should().Be("/api/sync/health");
    }

    // -------------------------------------------------------------------
    // IsAvailableAsync — Unavailable
    // -------------------------------------------------------------------

    [Fact]
    public async Task IsAvailableAsync_ServerUnavailable_ReturnsFalse()
    {
        // Arrange
        MockHttpHandler handler = new(statusCode: HttpStatusCode.ServiceUnavailable);
        HttpSyncTransport sut = new(
            CreateHttpClient(handler),
            Options.Create(CreateOptions()),
            CreateLogger().Object);

        // Act
        bool result = await sut.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAvailableAsync_NetworkError_ReturnsFalse()
    {
        // Arrange
        MockHttpHandler handler = new(throwTimeout: true);
        HttpSyncTransport sut = new(
            CreateHttpClient(handler),
            Options.Create(CreateOptions()),
            CreateLogger().Object);

        // Act
        bool result = await sut.IsAvailableAsync();

        // Assert
        result.Should().BeFalse();
    }

    // -------------------------------------------------------------------
    // Constructor — Default headers
    // -------------------------------------------------------------------

    [Fact]
    public void Constructor_WithDefaultHeaders_SetsHeadersOnHttpClient()
    {
        // Arrange
        HttpSyncTransportOptions options = CreateOptions();
        options.DefaultHeaders["X-Api-Key"] = "test-key-123";
        options.DefaultHeaders["X-Custom"] = "custom-value";

        HttpClient httpClient = CreateHttpClient(new MockHttpHandler(new SyncResponse { IsSuccess = true }));
        HttpSyncTransport sut = new(httpClient, Options.Create(options), CreateLogger().Object);

        // Assert
        httpClient.DefaultRequestHeaders.Should().Contain(h => h.Key == "X-Api-Key");
        httpClient.DefaultRequestHeaders.GetValues("X-Api-Key").Should().Contain("test-key-123");
        httpClient.DefaultRequestHeaders.Should().Contain(h => h.Key == "X-Custom");
    }

    [Fact]
    public void Constructor_WithTimeout_SetsTimeoutOnHttpClient()
    {
        // Arrange
        HttpSyncTransportOptions options = CreateOptions();
        options.Timeout = TimeSpan.FromSeconds(15);

        HttpClient httpClient = CreateHttpClient(new MockHttpHandler(new SyncResponse { IsSuccess = true }));
        _ = new HttpSyncTransport(httpClient, Options.Create(options), CreateLogger().Object);

        // Assert
        httpClient.Timeout.Should().Be(TimeSpan.FromSeconds(15));
    }

    // -------------------------------------------------------------------
    // DI Extension
    // -------------------------------------------------------------------

    [Fact]
    public void AddXForgeSyncHttp_RegistersServices()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddOptions();
        services.AddLogging();
        services.Configure<HttpSyncTransportOptions>(o => o.BaseUrl = "https://test.com");

        // Act
        services.AddXForgeSyncHttp();

        // Assert
        ServiceProvider provider = services.BuildServiceProvider();
        ISyncTransport? transport = provider.GetService<ISyncTransport>();
        transport.Should().NotBeNull();
    }

    [Fact]
    public void AddXForgeSyncHttp_WithConfigureAction_BindsOptions()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddOptions();
        services.AddLogging();

        // Act
        services.AddXForgeSyncHttp(opts =>
        {
            opts.BaseUrl = "https://custom.example.com";
            opts.Timeout = TimeSpan.FromSeconds(60);
            opts.MaxRetries = 5;
        });

        // Assert
        ServiceProvider provider = services.BuildServiceProvider();
        IOptions<HttpSyncTransportOptions> options = provider.GetRequiredService<IOptions<HttpSyncTransportOptions>>();
        options.Value.BaseUrl.Should().Be("https://custom.example.com");
        options.Value.Timeout.Should().Be(TimeSpan.FromSeconds(60));
        options.Value.MaxRetries.Should().Be(5);
    }

    // -------------------------------------------------------------------
    // Mock HTTP Handler
    // -------------------------------------------------------------------

    /// <summary>
    /// A mock <see cref="HttpMessageHandler"/> for testing HTTP transport without a real server.
    /// </summary>
    private sealed class MockHttpHandler(
        SyncResponse? responseBody = null,
        HttpStatusCode statusCode = HttpStatusCode.OK,
        bool throwTimeout = false) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }
        public Uri? LastRequestUri { get; private set; }
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            LastRequestUri = request.RequestUri;

            if (request.Content is not null)
            {
                LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            if (throwTimeout)
            {
                throw new TaskCanceledException("Request timed out");
            }

            HttpResponseMessage response = new(statusCode);

            if (responseBody is not null)
            {
                string json = JsonSerializer.Serialize(responseBody, JsonOptions);
                response.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            }
            else if (statusCode == HttpStatusCode.OK)
            {
                // Return empty content for null response body tests
                response.Content = new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json");
            }
            else
            {
                response.Content = new StringContent("Error", System.Text.Encoding.UTF8, "text/plain");
            }

            return response;
        }
    }
}
