using Asisya.Application.Abstractions;
using Asisya.Application.Categories;
using Asisya.Application.Common;
using Asisya.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace Asisya.Application.Tests.Categories;

public sealed class CategoryServiceTests : IDisposable
{
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly CategoryService _sut;

    public CategoryServiceTests()
    {
        _sut = new CategoryService(_categories.Object, _cache);
    }

    public void Dispose() => _cache.Dispose();

    [Fact]
    public async Task ListAsync_CalledTwice_HitsRepositoryOnce()
    {
        _categories.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Category { Id = Guid.NewGuid(), Name = "CLOUD", PhotoUrl = "https://x/c.png" }]);

        var first = await _sut.ListAsync();
        var second = await _sut.ListAsync();

        Assert.Single(first);
        Assert.Same(first, second);
        _categories.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InvalidatesListCache()
    {
        _categories.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        await _sut.ListAsync();

        await _sut.CreateAsync(new CreateCategoryDto { Name = "EDGE", PhotoUrl = "https://x/e.png" });
        await _sut.ListAsync();

        _categories.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task CreateAsync_WhenValid_PersistsAndReturnsDto()
    {
        _categories.Setup(x => x.ExistsByNameAsync("SERVIDORES", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.CreateAsync(new CreateCategoryDto
        {
            Name = " SERVIDORES ",
            Description = "Infra",
            PhotoUrl = "https://cdn.example.com/servidores.png"
        });

        Assert.Equal("SERVIDORES", result.Name);
        Assert.Equal("https://cdn.example.com/servidores.png", result.PhotoUrl);
        _categories.Verify(x => x.AddAsync(It.Is<Category>(c => c.Name == "SERVIDORES"), It.IsAny<CancellationToken>()), Times.Once);
        _categories.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenNameMissing_ThrowsBusinessException()
    {
        await Assert.ThrowsAsync<BusinessException>(() =>
            _sut.CreateAsync(new CreateCategoryDto { Name = " ", PhotoUrl = "https://cdn.example.com/x.png" }));
    }

    [Fact]
    public async Task CreateAsync_WhenPhotoMissing_ThrowsBusinessException()
    {
        await Assert.ThrowsAsync<BusinessException>(() =>
            _sut.CreateAsync(new CreateCategoryDto { Name = "CLOUD", PhotoUrl = "" }));
    }

    [Fact]
    public async Task CreateAsync_WhenDuplicateName_ThrowsBusinessException()
    {
        _categories.Setup(x => x.ExistsByNameAsync("CLOUD", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<BusinessException>(() =>
            _sut.CreateAsync(new CreateCategoryDto
            {
                Name = "CLOUD",
                PhotoUrl = "https://cdn.example.com/cloud.png"
            }));

        Assert.Contains("already exists", ex.Message);
        _categories.Verify(x => x.AddAsync(It.IsAny<Category>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
