using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using UmiHealth.Infrastructure.Repositories;
using UmiHealth.Infrastructure.Data;
using UmiHealth.Core.Entities;

namespace UmiHealth.Tests.Unit.Infrastructure.Repositories;

public class RepositoryTests
{
    private readonly Mock<AppDbContext> _contextMock;
    private readonly Repository<Tenant> _repository;

    public RepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _contextMock = new Mock<AppDbContext>(options);
        _repository = new Repository<Tenant>(_contextMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ShouldReturnEntity()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Test Tenant" };
        var mockDbSet = CreateMockDbSet(new List<Tenant> { tenant });
        _contextMock.Setup(x => x.Set<Tenant>()).Returns(mockDbSet.Object);

        // Act
        var result = await _repository.GetByIdAsync(tenantId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tenantId, result.Id);
        Assert.Equal("Test Tenant", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var mockDbSet = CreateMockDbSet(new List<Tenant>());
        _contextMock.Setup(x => x.Set<Tenant>()).Returns(mockDbSet.Object);

        // Act
        var result = await _repository.GetByIdAsync(tenantId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllEntities()
    {
        // Arrange
        var tenants = new List<Tenant>
        {
            new Tenant { Id = Guid.NewGuid(), Name = "Tenant 1" },
            new Tenant { Id = Guid.NewGuid(), Name = "Tenant 2" },
            new Tenant { Id = Guid.NewGuid(), Name = "Tenant 3" }
        };
        var mockDbSet = CreateMockDbSet(tenants);
        _contextMock.Setup(x => x.Set<Tenant>()).Returns(mockDbSet.Object);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Contains(result, t => t.Name == "Tenant 1");
        Assert.Contains(result, t => t.Name == "Tenant 2");
        Assert.Contains(result, t => t.Name == "Tenant 3");
    }

    [Fact]
    public async Task FindAsync_WithPredicate_ShouldReturnFilteredEntities()
    {
        // Arrange
        var tenants = new List<Tenant>
        {
            new Tenant { Id = Guid.NewGuid(), Name = "Active Tenant", IsActive = true },
            new Tenant { Id = Guid.NewGuid(), Name = "Inactive Tenant", IsActive = false },
            new Tenant { Id = Guid.NewGuid(), Name = "Another Active", IsActive = true }
        };
        var mockDbSet = CreateMockDbSet(tenants);
        _contextMock.Setup(x => x.Set<Tenant>()).Returns(mockDbSet.Object);

        // Act
        var result = await _repository.FindAsync(t => t.IsActive);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, t => Assert.True(t.IsActive));
    }

    [Fact]
    public async Task AddAsync_ShouldAddEntity()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "New Tenant",
            IsActive = true
        };
        var mockDbSet = CreateMockDbSet(new List<Tenant>());
        _contextMock.Setup(x => x.Set<Tenant>()).Returns(mockDbSet.Object);

        // Act
        var result = await _repository.AddAsync(tenant);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Tenant", result.Name);
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateEntity()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Updated Tenant",
            IsActive = true
        };
        var mockDbSet = CreateMockDbSet(new List<Tenant>());
        _contextMock.Setup(x => x.Set<Tenant>()).Returns(mockDbSet.Object);

        // Act
        await _repository.UpdateAsync(tenant);

        // Assert
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteEntity()
    {
        // Arrange
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Tenant to Delete",
            IsActive = true
        };
        var mockDbSet = CreateMockDbSet(new List<Tenant>());
        _contextMock.Setup(x => x.Set<Tenant>()).Returns(mockDbSet.Object);

        // Act
        await _repository.DeleteAsync(tenant);

        // Assert
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var tenants = new List<Tenant>
        {
            new Tenant { Id = Guid.NewGuid(), Name = "Tenant 1", IsActive = true },
            new Tenant { Id = Guid.NewGuid(), Name = "Tenant 2", IsActive = false },
            new Tenant { Id = Guid.NewGuid(), Name = "Tenant 3", IsActive = true }
        };
        var mockDbSet = CreateMockDbSet(tenants);
        _contextMock.Setup(x => x.Set<Tenant>()).Returns(mockDbSet.Object);

        // Act
        var result = await _repository.CountAsync();

        // Assert
        Assert.Equal(3, result);
    }

    [Fact]
    public async Task CountAsync_WithPredicate_ShouldReturnFilteredCount()
    {
        // Arrange
        var tenants = new List<Tenant>
        {
            new Tenant { Id = Guid.NewGuid(), Name = "Active Tenant", IsActive = true },
            new Tenant { Id = Guid.NewGuid(), Name = "Inactive Tenant", IsActive = false },
            new Tenant { Id = Guid.NewGuid(), Name = "Another Active", IsActive = true }
        };
        var mockDbSet = CreateMockDbSet(tenants);
        _contextMock.Setup(x => x.Set<Tenant>()).Returns(mockDbSet.Object);

        // Act
        var result = await _repository.CountAsync(t => t.IsActive);

        // Assert
        Assert.Equal(2, result);
    }

    private Mock<DbSet<T>> CreateMockDbSet<T>(List<T> data) where T : class
    {
        var queryable = data.AsQueryable();
        var mockSet = new Mock<DbSet<T>>();

        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());

        mockSet.Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync((object[] ids, System.Threading.CancellationToken token) =>
            {
                var id = (Guid)ids[0];
                return data.FirstOrDefault(d => d.GetType().GetProperty("Id")?.GetValue(d)?.Equals(id) == true);
            });

        mockSet.Setup(m => m.AddAsync(It.IsAny<T>(), It.IsAny<System.Threading.CancellationToken>()))
            .ReturnsAsync((T entity, System.Threading.CancellationToken token) =>
            {
                data.Add(entity);
                return entity;
            });

        mockSet.Setup(m => m.UpdateAsync(It.IsAny<T>(), It.IsAny<System.Threading.CancellationToken>()))
            .Returns((T entity, System.Threading.CancellationToken token) =>
            {
                var index = data.FindIndex(d => d.GetType().GetProperty("Id")?.GetValue(d)?.Equals(entity.GetType().GetProperty("Id")?.GetValue(entity)) == true);
                if (index >= 0)
                {
                    data[index] = entity;
                }
                return Task.CompletedTask;
            });

        mockSet.Setup(m => m.RemoveAsync(It.IsAny<T>(), It.IsAny<System.Threading.CancellationToken>()))
            .Returns((T entity, System.Threading.CancellationToken token) =>
            {
                data.Remove(entity);
                return Task.CompletedTask;
            });

        return mockSet;
    }
}
