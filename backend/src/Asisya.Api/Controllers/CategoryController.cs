using Asisya.Application.Categories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Asisya.Api.Controllers;

[ApiController]
[Authorize]
[Route("Category")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public sealed class CategoryController : ControllerBase
{
    private readonly CategoryService _categories;

    public CategoryController(CategoryService categories)
    {
        _categories = categories;
    }

    /// <summary>Lists all categories (served from an in-memory cache).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var items = await _categories.ListAsync(cancellationToken);
        return Ok(items);
    }

    /// <summary>Creates a category. Name must be unique; photo URL is required.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateCategoryDto request, CancellationToken cancellationToken)
    {
        var created = await _categories.CreateAsync(request, cancellationToken);
        return Created($"/Category/{created.Id}", created);
    }
}
