namespace Asisya.Application.Products;

public sealed class BulkGenerateResultDto
{
    public int RequestedCount { get; init; }
    public int InsertedCount { get; init; }
    public int BatchSize { get; init; }
    public int BatchCount { get; init; }
    public long ElapsedMilliseconds { get; init; }
    public double ElapsedSeconds => ElapsedMilliseconds / 1000.0;
}
