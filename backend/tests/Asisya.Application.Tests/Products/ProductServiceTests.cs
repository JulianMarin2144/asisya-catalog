using Asisya.Application.Abstractions;
using Asisya.Application.Common;
using Asisya.Application.Products;
using Asisya.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Asisya.Application.Tests.Products;

public sealed class ProductServiceTests
{
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly ProductService _sut;

    public ProductServiceTests()
    {
        _sut = new ProductService(_products.Object, _categories.Object, NullLogger<ProductService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_WhenValid_PersistsProduct()
    {
        var categoryId = Guid.NewGuid();
        _categories.Setup(x => x.ExistsAsync(categoryId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _products.Setup(x => x.GetByIdWithCategoryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => new Product
            {
                Id = id,
                Name = "Blade",
                Price = 10,
                Stock = 2,
                CategoryId = categoryId,
                Category = new Category { Id = categoryId, Name = "SERVIDORES", PhotoUrl = "https://x/s.png" }
            });

        var result = await _sut.CreateAsync(new CreateProductDto
        {
            Name = "Blade",
            Price = 10,
            Stock = 2,
            CategoryId = categoryId
        });

        Assert.Equal("Blade", result.Name);
        _products.Verify(x => x.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
        _products.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("", 10, 1)]
    [InlineData("Name", 0, 1)]
    [InlineData("Name", -1, 1)]
    [InlineData("Name", 10, -1)]
    public async Task CreateAsync_WhenInvalidFields_ThrowsBusinessException(string name, decimal price, int stock)
    {
        var categoryId = Guid.NewGuid();
        _categories.Setup(x => x.ExistsAsync(categoryId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _sut.CreateAsync(new CreateProductDto
            {
                Name = name,
                Price = price,
                Stock = stock,
                CategoryId = categoryId
            }));
    }

    [Fact]
    public async Task CreateAsync_WhenCategoryMissing_ThrowsBusinessException()
    {
        var categoryId = Guid.NewGuid();
        _categories.Setup(x => x.ExistsAsync(categoryId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await Assert.ThrowsAsync<BusinessException>(() =>
            _sut.CreateAsync(new CreateProductDto
            {
                Name = "Blade",
                Price = 10,
                Stock = 1,
                CategoryId = categoryId
            }));
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_UpdatesFields()
    {
        var id = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var entity = new Product
        {
            Id = id,
            Name = "Old",
            Price = 1,
            Stock = 1,
            CategoryId = categoryId
        };

        _categories.Setup(x => x.ExistsAsync(categoryId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _products.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _products.Setup(x => x.GetByIdWithCategoryAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Product
            {
                Id = id,
                Name = "New",
                Price = 20,
                Stock = 5,
                CategoryId = categoryId,
                Category = new Category { Id = categoryId, Name = "CLOUD", PhotoUrl = "https://x/c.png" }
            });

        var result = await _sut.UpdateAsync(id, new UpdateProductDto
        {
            Name = "New",
            Price = 20,
            Stock = 5,
            CategoryId = categoryId
        });

        Assert.Equal("New", result.Name);
        Assert.Equal(20, result.Price);
        Assert.Equal("New", entity.Name);
        Assert.Equal(5, entity.Stock);
        Assert.NotNull(entity.UpdatedAtUtc);
        _products.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_ThrowsNotFound()
    {
        var id = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        _categories.Setup(x => x.ExistsAsync(categoryId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _products.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.UpdateAsync(id, new UpdateProductDto
            {
                Name = "New",
                Price = 20,
                Stock = 5,
                CategoryId = categoryId
            }));
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_RemovesProduct()
    {
        var id = Guid.NewGuid();
        var entity = new Product { Id = id, Name = "X", Price = 1, Stock = 0 };
        _products.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        await _sut.DeleteAsync(id);

        _products.Verify(x => x.Remove(entity), Times.Once);
        _products.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenMissing_ThrowsNotFound()
    {
        var id = Guid.NewGuid();
        _products.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Product?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(id));
    }

    [Fact]
    public async Task ListAsync_NormalizesPagingAndMapsResults()
    {
        var categoryId = Guid.NewGuid();
        var products = new List<Product>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "A",
                Price = 1,
                Stock = 1,
                CategoryId = categoryId,
                Category = new Category { Id = categoryId, Name = "SERVIDORES", PhotoUrl = "https://x/s.png" }
            }
        };

        _products.Setup(x => x.SearchAsync(1, 10, categoryId, "A", It.IsAny<CancellationToken>()))
            .ReturnsAsync((products, 1));

        var result = await _sut.ListAsync(0, 0, categoryId, "A");

        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("A", result.Items[0].Name);
    }

    [Fact]
    public async Task BulkGenerateAsync_WhenCategoriesExist_InsertsInBatches()
    {
        var servidores = new Category { Id = Guid.NewGuid(), Name = "SERVIDORES", PhotoUrl = "https://x/s.png" };
        var cloud = new Category { Id = Guid.NewGuid(), Name = "CLOUD", PhotoUrl = "https://x/c.png" };

        _categories.Setup(x => x.GetByNamesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category> { servidores, cloud });

        var batches = new List<int>();
        _products.Setup(x => x.AddRangeBatchAsync(It.IsAny<IReadOnlyList<Product>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<Product>, CancellationToken>((items, _) => batches.Add(items.Count))
            .Returns(Task.CompletedTask);

        var result = await _sut.BulkGenerateAsync(12_000);

        Assert.Equal(12_000, result.RequestedCount);
        Assert.Equal(12_000, result.InsertedCount);
        Assert.Equal(3, result.BatchCount);
        Assert.Equal(new[] { 5000, 5000, 2000 }, batches);
        _categories.Verify(x => x.GetByNamesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkGenerateAsync_WhenCountTooLow_Throws()
    {
        await Assert.ThrowsAsync<BusinessException>(() => _sut.BulkGenerateAsync(1));
    }

    [Fact]
    public async Task BulkGenerateAsync_WhenCategoriesMissing_ThrowsWithoutInserting()
    {
        _categories.Setup(x => x.GetByNamesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Category>
            {
                new() { Id = Guid.NewGuid(), Name = "SERVIDORES", PhotoUrl = "https://x/s.png" }
            });

        var ex = await Assert.ThrowsAsync<BusinessException>(() => _sut.BulkGenerateAsync(10));
        Assert.Contains("Missing", ex.Message);
        _products.Verify(x => x.AddRangeBatchAsync(It.IsAny<IReadOnlyList<Product>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
