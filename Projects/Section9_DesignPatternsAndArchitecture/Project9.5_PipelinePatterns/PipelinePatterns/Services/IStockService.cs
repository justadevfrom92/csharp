namespace PipelinePatterns.Services;

public interface IStockService
{
    int GetStock(string sku);

    void Reserve(string sku, int quantity);
}
