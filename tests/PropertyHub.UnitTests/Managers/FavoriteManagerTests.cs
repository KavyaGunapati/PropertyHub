using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PropertyHub.DataAccess.Entities;
using PropertyHub.DataAccess.Enums;
using PropertyHub.Managers;
using PropertyHub.UnitTests.Helpers;

namespace PropertyHub.UnitTests.Managers
{
    public class FavoriteManagerTests
    {
        [Fact]
        public async Task AddAsync_Should_Add_Favorite_When_Property_Exists()
        {
            var context= DbContextFactory.Create();
            context.Properties.Add(new Property
            {
                Id = 1,
            Title = "Villa",
            Description = "Nice villa",
            Address = "1 Main St",
            City = "Hyderabad",
            State = "TS",
            ZipCode = "500001",
            Status = PropertyStatus.Published,
            AgentId = 10
            });
            await context.SaveChangesAsync();
            var manger=new FavoriteManager(context, new Mock<IMapper>().Object, NullLogger<FavoriteManager>.Instance);
            var result= await manger.AddAsync(100,1);
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("Added to favorites");
            context.Favorites.Count().Should().Be(1);
        }
        [Fact]
        public async Task AddAsync_Should_Return_Failure_When_Property_Not_Found()
        {
            var context=DbContextFactory.Create();
            var manager=new FavoriteManager(context,new Mock<IMapper>().Object,NullLogger<FavoriteManager>.Instance);
            var result=await manager.AddAsync(100,999);
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Property Not found");                                                                                                                                                                                                                                                                                                  
        }
        [Fact]
        public async Task  AddAsync_Should_Return_Already_In_Favorites()
        {
            // Given
            var context= DbContextFactory.Create();
            context.Properties.Add(new Property
            {
                Id = 1,
                Title = "Villa",
                Description = "Nice villa",
                Address = "1 Main St",
                City = "Hyderabad",
                State = "TS",
                ZipCode = "500001",
                Status = PropertyStatus.Published,
                AgentId = 10
            });
            context.Favorites.Add(new Favorite
            {
                UserId = 100,
                PropertyId = 1,
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
            var manager=new FavoriteManager(context,new Mock<IMapper>().Object,NullLogger<FavoriteManager>.Instance);
            // Act
            var result = await manager.AddAsync(100, 1);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("Already in favorites");
        }
        [Fact]
        public async Task RemoveAsync_Should_Remove_Favorite()
        {
            var context= DbContextFactory.Create();
            context.Properties.Add(new Property
            {
                Id = 1,
            Title = "Villa",
            Description = "Nice villa",
            Address = "1 Main St",
            City = "Hyderabad",
            State = "TS",
            ZipCode = "500001",
            Status = PropertyStatus.Published,
            AgentId = 10
            });
            await context.SaveChangesAsync();
            var manger=new FavoriteManager(context, new Mock<IMapper>().Object, NullLogger<FavoriteManager>.Instance);
            var result= await manger.RemoveAsync(100,1);
            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Removed from favorites");
            context.Favorites.Count().Should().Be(0);
        }
        [Fact]
public async Task RemoveAsync_Should_Return_Failure_When_Favorite_Not_Found()
{
    // Arrange
    var context = DbContextFactory.Create();

    var manager = new FavoriteManager(context,new Mock<IMapper>().Object,NullLogger<FavoriteManager>.Instance);

    // Act
    var result = await manager.RemoveAsync(100, 1);

    // Assert
    result.IsSuccess.Should().BeFalse();
    result.Error.Should().Be("Removed from favorites");
}
    }
}