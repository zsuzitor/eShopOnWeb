using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure.Storage.Blobs;
using Microsoft.Azure.Cosmos;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Integration;

namespace OrderItemsReserver
{
    public static class SendOrder
    {
        //cosmos
        private static readonly string _endpointUri = "https://zsuz-cosmos.documents.azure.com:443/";
        private static readonly string _primaryKey = "awEakCIfVNCCbkDvAb41WleRUiJ8Dgxm4bsWwTw637lbLciFH5P0vltEGDJXvPxdXyEHwkXnEiMv4fKG3PxZHw==";
        //cosmos

        [FunctionName("SendOrder")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation($"C# HTTP trigger function processed a request at {DateTime.Now}");
            //await SaveToBlob(req, log);
            await SaveToCosmosDb(req, log);

            return new OkObjectResult("success end");//OkResult();//OkObjectResult
        }

        private static async Task SaveToCosmosDb(HttpRequest req,
            ILogger log)
        {
            using (CosmosClient client = new CosmosClient(_endpointUri, _primaryKey))
            {
                var container = client.GetContainer("eshop", "orders");
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                //dynamic data = JsonConvert.DeserializeObject(requestBody);
                var data = JsonConvert.DeserializeObject<OrderIntegration>(requestBody);
                //var it = new {
                //    BuyerId=123,
                //    Name="123"
                //};
                //await container.CreateItemStreamAsync(req.Body, new PartitionKey(data.BuyerId));
                await container.CreateItemAsync(data, new PartitionKey(data.BuyerId));
            }
        }


        private static async Task SaveToBlob(HttpRequest req,
        ILogger log)
        {
            string connectionStringToBlob = "DefaultEndpointsProtocol=https;AccountName=zsuzlearnblob;AccountKey=v9PT+SJAomWnrxev/3hIggVxggj0yhd6rq8D2RaD+XarVxBMKtEfVWBahfwfmpgQ2qI7X66Z9EivHQWiwKSNwg==;EndpointSuffix=core.windows.net";
            string blobConainerName = "zsus-orders-eshop";

            //string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            //dynamic data = JsonConvert.DeserializeObject(requestBody);


            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionStringToBlob);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(blobConainerName);
            //containerClient.CreateIfNotExistsAsync();
            await containerClient.UploadBlobAsync($"{Guid.NewGuid()}.json", req.Body);
        }
    }
}
