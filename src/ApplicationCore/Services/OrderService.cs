using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using BlazorShared;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.eShopWeb.ApplicationCore.Integration;

namespace Microsoft.eShopWeb.ApplicationCore.Services;

public class OrderService : IOrderService
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IUriComposer _uriComposer;
    private readonly IRepository<Basket> _basketRepository;
    private readonly IRepository<CatalogItem> _itemRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly BaseUrlConfiguration _baseUrlConfiguration;

    public OrderService(IRepository<Basket> basketRepository,
        IRepository<CatalogItem> itemRepository,
        IRepository<Order> orderRepository,
        IUriComposer uriComposer, IHttpClientFactory httpClientFactory,
        IOptions<BaseUrlConfiguration> baseUrlConfiguration)
    {
        _orderRepository = orderRepository;
        _uriComposer = uriComposer;
        _basketRepository = basketRepository;
        _itemRepository = itemRepository;
        _httpClientFactory = httpClientFactory;
        _baseUrlConfiguration = baseUrlConfiguration.Value;
    }

    public async Task CreateOrderAsync(int basketId, Address shippingAddress)
    {
        var basketSpec = new BasketWithItemsSpecification(basketId);
        var basket = await _basketRepository.GetBySpecAsync(basketSpec);

        Guard.Against.NullBasket(basketId, basket);
        Guard.Against.EmptyBasketOnCheckout(basket.Items);

        var catalogItemsSpecification = new CatalogItemsSpecification(basket.Items.Select(item => item.CatalogItemId).ToArray());
        var catalogItems = await _itemRepository.ListAsync(catalogItemsSpecification);

        var items = basket.Items.Select(basketItem =>
        {
            var catalogItem = catalogItems.First(c => c.Id == basketItem.CatalogItemId);
            var itemOrdered = new CatalogItemOrdered(catalogItem.Id, catalogItem.Name, _uriComposer.ComposePicUri(catalogItem.PictureUri));
            var orderItem = new OrderItem(itemOrdered, basketItem.UnitPrice, basketItem.Quantity);
            return orderItem;
        }).ToList();

        var order = new Order(basket.BuyerId, shippingAddress, items);

        await _orderRepository.AddAsync(order);

        //https://docs.microsoft.com/ru-ru/aspnet/core/fundamentals/http-requests?view=aspnetcore-6.0
        HttpClient httpClient = _httpClientFactory.CreateClient();
        httpClient.BaseAddress = new System.Uri(_baseUrlConfiguration.ReserveOrderUrl);
        var itemJson = new StringContent(
           JsonSerializer.Serialize(new OrderIntegration(order)),
           Encoding.UTF8,
           Application.Json);//http://localhost:7071/api/Function1
        var response = await httpClient.PostAsync("/api/SendOrder?code=mNqAedawR382YPqYg0B6ET91UH4/1OJIUtLzqKQDDgzame7d/8tTGA==", itemJson);//GetAsync("/api/values");
            _ = await response.Content.ReadAsStringAsync();
        //List<string> data = JsonSerializer.Deserialize<List<string>>(str);
        
    }
}
