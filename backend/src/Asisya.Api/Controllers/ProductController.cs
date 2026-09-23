using Asisya.Application.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;

namespace Asisya.Api.Controllers;

[ApiController]
[Route("Product")]
[Authorize]
public sealed class ProductController : ControllerBase
{
    private readonly ProductService _products;

    public ProductController(ProductService products)
    {
        _products = products;
    }

    /// <summary>Creates one product, or bulk-generates random products when <c>count</c> &gt; 1.</summary>
    /// <remarks>
    /// Bulk mode (max 100,000) spreads products across SERVIDORES and CLOUD and inserts them in
    /// batches of 5,000. Returns 200 with timing stats for bulk, 201 with the product otherwise.
    /// </remarks>
    [HttpPost]
    [RequestTimeout(600_000)]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BulkGenerateResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateProductDto request, CancellationToken cancellationToken)
    {
        if (request.Count is > 1)
        {
            var bulk = await _products.BulkGenerateAsync(request.Count.Value, cancellationToken);
            return Ok(bulk);
        }

        var created = await _products.CreateAsync(request, cancellationToken);
        return Created($"/Products/{created.Id}", created);
    }

    /// <summary>Replaces a product's editable fields.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductDto request, CancellationToken cancellationToken)
    {
        var updated = await _products.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>Deletes a product.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _products.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
