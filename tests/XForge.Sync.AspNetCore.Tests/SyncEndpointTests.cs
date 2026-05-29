using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using XForge.Sync.AspNetCore.Endpoints;
using XForge.Sync.AspNetCore.Extensions;

namespace XForge.Sync.AspNetCore.Tests;

public class SyncEndpointTests
{
    private static NullLoggerFactory CreateLoggerFactory() => NullLoggerFactory.Instance;

    [Fact]
    public async Task HandleSync_WithSuccessfulHandler_ReturnsOkWithResponse()
    {
        // Arrange
        Mock<IServerSyncHandler> handlerMock = new();
        SyncResponse expectedResponse = new()
        {
            IsSuccess = true,
            ServerVersion = 5,
            Changes =
            [
                new TrackedChange
                {
                    Key = "item-1",
                    ChangeType = ChangeType.Update,
                    EntityType = "Product",
                    Data = """{"name":"Updated"}""",
                    Version = 5,
                    TrackedAt = DateTime.UtcNow
                }
            ]
        };

        handlerMock
            .Setup(h => h.HandleSyncAsync(It.IsAny<SyncRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        SyncRequest request = new()
        {
            ClientId = "test-client",
            LastServerVersion = 0,
            Changes = []
        };

        // Act
        IResult result = await SyncEndpoint.HandleSync(
            request, handlerMock.Object, CreateLoggerFactory(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();

        // Verify the handler was called with the correct request
        handlerMock.Verify(
            h => h.HandleSyncAsync(
                It.Is<SyncRequest>(r => r.ClientId == "test-client"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleSync_WithChanges_SendsChangesToHandler()
    {
        // Arrange
        Mock<IServerSyncHandler> handlerMock = new();
        SyncResponse response = new() { IsSuccess = true, ServerVersion = 1 };
        handlerMock
            .Setup(h => h.HandleSyncAsync(It.IsAny<SyncRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        SyncRequest request = new()
        {
            ClientId = "client-1",
            LastServerVersion = 0,
            Changes =
            [
                new TrackedChange
                {
                    Key = "item-1",
                    ChangeType = ChangeType.Create,
                    EntityType = "Order",
                    Data = """{"id":1}""",
                    Version = 1,
                    TrackedAt = DateTime.UtcNow
                },
                new TrackedChange
                {
                    Key = "item-2",
                    ChangeType = ChangeType.Delete,
                    EntityType = "Order",
                    Version = 2,
                    TrackedAt = DateTime.UtcNow
                }
            ]
        };

        // Act
        IResult result = await SyncEndpoint.HandleSync(
            request, handlerMock.Object, CreateLoggerFactory(), CancellationToken.None);

        // Assert
        handlerMock.Verify(
            h => h.HandleSyncAsync(
                It.Is<SyncRequest>(r => r.Changes.Count == 2),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleSync_WhenHandlerThrows_ReturnsFailureResponse()
    {
        // Arrange
        Mock<IServerSyncHandler> handlerMock = new();
        handlerMock
            .Setup(h => h.HandleSyncAsync(It.IsAny<SyncRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        SyncRequest request = new()
        {
            ClientId = "test-client",
            LastServerVersion = 0,
            Changes = []
        };

        // Act
        IResult result = await SyncEndpoint.HandleSync(
            request, handlerMock.Object, CreateLoggerFactory(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleSync_WhenHandlerReturnsFailure_ReturnsOkWithFailure()
    {
        // Arrange
        Mock<IServerSyncHandler> handlerMock = new();
        SyncResponse failureResponse = new()
        {
            IsSuccess = false,
            ErrorMessage = "Server storage unavailable"
        };
        handlerMock
            .Setup(h => h.HandleSyncAsync(It.IsAny<SyncRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failureResponse);

        SyncRequest request = new()
        {
            ClientId = "test-client",
            LastServerVersion = 0,
            Changes = []
        };

        // Act
        IResult result = await SyncEndpoint.HandleSync(
            request, handlerMock.Object, CreateLoggerFactory(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        handlerMock.Verify(
            h => h.HandleSyncAsync(It.IsAny<SyncRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void HandleHealth_ReturnsHealthyStatus()
    {
        // Act
        IResult result = SyncEndpoint.HandleHealth();

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleSync_WithCancellation_PassesCancellationTokenToHandler()
    {
        // Arrange
        Mock<IServerSyncHandler> handlerMock = new();
        SyncResponse response = new() { IsSuccess = true };
        CancellationTokenSource cts = new();
        CancellationToken token = cts.Token;

        handlerMock
            .Setup(h => h.HandleSyncAsync(It.IsAny<SyncRequest>(), token))
            .ReturnsAsync(response);

        SyncRequest request = new()
        {
            ClientId = "test-client",
            LastServerVersion = 0,
            Changes = []
        };

        // Act
        await SyncEndpoint.HandleSync(request, handlerMock.Object, CreateLoggerFactory(), token);

        // Assert
        handlerMock.Verify(
            h => h.HandleSyncAsync(It.IsAny<SyncRequest>(), token),
            Times.Once);
    }

    [Fact]
    public async Task HandleSync_WithMultipleChangeTypes_SendsAllToHandler()
    {
        // Arrange
        Mock<IServerSyncHandler> handlerMock = new();
        SyncResponse response = new() { IsSuccess = true, ServerVersion = 10 };
        handlerMock
            .Setup(h => h.HandleSyncAsync(It.IsAny<SyncRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        SyncRequest request = new()
        {
            ClientId = "multi-client",
            LastServerVersion = 5,
            Changes =
            [
                new TrackedChange
                {
                    Key = "create-1",
                    ChangeType = ChangeType.Create,
                    EntityType = "Product",
                    Data = """{"name":"New"}""",
                    Version = 6,
                    TrackedAt = DateTime.UtcNow
                },
                new TrackedChange
                {
                    Key = "update-1",
                    ChangeType = ChangeType.Update,
                    EntityType = "Product",
                    Data = """{"name":"Updated"}""",
                    Version = 7,
                    TrackedAt = DateTime.UtcNow
                },
                new TrackedChange
                {
                    Key = "delete-1",
                    ChangeType = ChangeType.Delete,
                    EntityType = "Product",
                    Version = 8,
                    TrackedAt = DateTime.UtcNow
                }
            ]
        };

        // Act
        await SyncEndpoint.HandleSync(
            request, handlerMock.Object, CreateLoggerFactory(), CancellationToken.None);

        // Assert
        handlerMock.Verify(
            h => h.HandleSyncAsync(
                It.Is<SyncRequest>(r =>
                    r.ClientId == "multi-client" &&
                    r.LastServerVersion == 5 &&
                    r.Changes.Count == 3),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleSync_WithConflictsInResponse_ReturnsResponseWithConflicts()
    {
        // Arrange
        Mock<IServerSyncHandler> handlerMock = new();
        SyncResponse responseWithConflicts = new()
        {
            IsSuccess = true,
            ServerVersion = 10,
            Conflicts =
            [
                new SyncConflict<object>
                {
                    Key = "item-1",
                    Local = new { Name = "Local" },
                    Remote = new { Name = "Remote" },
                    LocalVersion = 5,
                    RemoteVersion = 8,
                    LocalModified = DateTime.UtcNow.AddMinutes(-10),
                    RemoteModified = DateTime.UtcNow
                }
            ]
        };
        handlerMock
            .Setup(h => h.HandleSyncAsync(It.IsAny<SyncRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseWithConflicts);

        SyncRequest request = new()
        {
            ClientId = "test-client",
            LastServerVersion = 0,
            Changes = []
        };

        // Act
        IResult result = await SyncEndpoint.HandleSync(
            request, handlerMock.Object, CreateLoggerFactory(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void MapXForgeSync_WithNullApp_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => SyncEndpoint.MapXForgeSync(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void MapXForgeSync_RegistersEndpoints()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<IServerSyncHandler>(Mock.Of<IServerSyncHandler>());
        WebApplication app = builder.Build();

        // Act
        IEndpointRouteBuilder result = app.MapXForgeSync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(app);
    }

    [Fact]
    public void MapXForgeSync_WithCustomOptions_AppliesOptions()
    {
        // Arrange
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<IServerSyncHandler>(Mock.Of<IServerSyncHandler>());
        WebApplication app = builder.Build();

        // Act
        IEndpointRouteBuilder result = app.MapXForgeSync(opts =>
        {
            opts.BasePath = "/custom/sync";
            opts.EnableHealthEndpoint = false;
            opts.EnableDetailedErrors = true;
        });

        // Assert
        result.Should().NotBeNull();
    }
}
