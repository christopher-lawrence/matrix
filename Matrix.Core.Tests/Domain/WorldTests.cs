using Matrix.Core.Domain;
using Xunit;

namespace Matrix.Core.Tests.Domain;

public sealed class WorldTests
{
    [Fact]
    public void DefaultArea_ExposesOutgoingConnections()
    {
        var world = new World();

        var area = world.GetDefaultArea();

        var connection = Assert.Single(area.Connections);
        Assert.Equal(Direction.North, connection.Direction);
        Assert.True(world.TryGetArea(connection.Destination, out var destination));
        Assert.NotNull(destination);
        Assert.Equal("Arcade", destination.Name);
    }

    [Fact]
    public void TryMove_ResolvesDestinationThroughMatchingConnection()
    {
        var world = new World();
        var lobby = world.GetDefaultArea();

        var moved = world.TryMove(lobby.Id, Direction.North, out var destinationAreaId);

        Assert.True(moved);
        Assert.True(world.TryGetArea(destinationAreaId, out var destination));
        Assert.NotNull(destination);
        Assert.Equal("Arcade", destination.Name);
    }

    [Fact]
    public void TryMove_DoesNotInferReverseConnection()
    {
        var world = new World();
        var lobby = world.GetDefaultArea();
        Assert.True(world.TryMove(lobby.Id, Direction.North, out var arcadeId));
        Assert.True(world.TryGetArea(arcadeId, out var arcade));
        Assert.NotNull(arcade);

        var moved = world.TryMove(arcade.Id, Direction.North, out var destinationAreaId);

        Assert.False(moved);
        Assert.Equal(default, destinationAreaId);
    }
}
