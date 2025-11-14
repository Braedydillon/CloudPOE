using Azure;
using Azure.Data.Tables;
using IncredibleComponentsPoe.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IncredibleComponentsPoe.Services
{
    public class AzureTableService
    {
        private readonly TableServiceClient _serviceClient;
        private readonly TableClient _orderEntity;

        public AzureTableService(string connectionString)
        {
            _serviceClient = new TableServiceClient(connectionString);
            _orderEntity = _serviceClient.GetTableClient("Orders");
        }

        private TableClient GetTable(string tableName)
        {
            var tableClient = _serviceClient.GetTableClient(tableName);
            tableClient.CreateIfNotExists();
            return tableClient;
        }

        public async Task AddCustomerAsync(CustomerEntity customer)
        {
            var table = GetTable("Customers");
            await table.AddEntityAsync(customer);
        }

        public async Task<List<CustomerEntity>> GetAllCustomersAsync()
        {
            var table = GetTable("Customers");
            var query = table.QueryAsync<CustomerEntity>();
            var results = new List<CustomerEntity>();

            await foreach (var entity in query)
            {
                results.Add(entity);
            }

            return results;
        }

        public async Task AddProductAsync(ProductEntity product)
        {
            var table = GetTable("Products");
            await table.AddEntityAsync(product);
        }

        public async Task<List<ProductEntity>> GetAllProductsAsync()
        {
            var table = GetTable("Products");
            var query = table.QueryAsync<ProductEntity>();
            var results = new List<ProductEntity>();

            await foreach (var entity in query)
            {
                results.Add(entity);
            }

            return results;
        }

        public async Task AddOrderAsync(OrderEntity order)
        {
            var table = GetTable("Orders");
            await table.AddEntityAsync(order);
        }

        public async Task<List<OrderEntity>> GetAllOrdersAsync()
        {
            var table = GetTable("Orders");
            var query = table.QueryAsync<OrderEntity>();
            var results = new List<OrderEntity>();

            await foreach (var entity in query)
            {
                results.Add(entity);
            }

            return results;
        }
    }
}
