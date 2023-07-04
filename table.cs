
using Azure.Data.Tables;
using Azure.Core;

public interface ITableStorage<T> where T : ITableEntity
{
    Task<T> GetEntityAsync(string partitionKey, string rowKey);
    Task<IEnumerable<T>> GetAllEntitiesAsync();
    Task<IEnumerable<T>> QueryEntitiesAsync(string filter);
    Task InsertOrUpdateEntityAsync(T entity);
    Task DeleteEntityAsync(string partitionKey, string rowKey);
}

public class AzureTableStorage<T> : ITableStorage<T> where T : ITableEntity, new()
{
    private readonly TableClient _tableClient;

    public AzureTableStorage(string connectionString, string tableName)
    {
        var clientOptions = new TableClientOptions
        {
            Transport = new HttpClientTransport(new HttpClient())
        };
        _tableClient = new TableClient(connectionString, tableName, clientOptions);
    }

    public async Task<T> GetEntityAsync(string partitionKey, string rowKey)
    {
        Response<T> response = await _tableClient.GetEntityAsync<T>(partitionKey, rowKey);
        return response.Value;
    }

    public async Task<IEnumerable<T>> GetAllEntitiesAsync()
    {
        List<T> entities = new List<T>();
        await foreach (Page<T> page in _tableClient.QueryAsync<T>(new()))
        {
            entities.AddRange(page.Values);
        }
        return entities;
    }

    public async Task<IEnumerable<T>> QueryEntitiesAsync(string filter)
    {
        List<T> entities = new List<T>();
        await foreach (Page<T> page in _tableClient.QueryAsync<T>(filter))
        {
            entities.AddRange(page.Values);
        }
        return entities;
    }

    public async Task InsertOrUpdateEntityAsync(T entity)
    {
        await _tableClient.UpsertEntityAsync(entity);
    }

    public async Task DeleteEntityAsync(string partitionKey, string rowKey)
    {
        await _tableClient.DeleteEntityAsync(partitionKey, rowKey);
    }
}
