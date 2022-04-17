using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using MinimalApi.Endpoint;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Microsoft.eShopWeb.PublicApi.CatalogItemEndpoints;

/// <summary>
/// List Catalog Items (paged)
/// </summary>
public class CatalogItemListPagedEndpoint : IEndpoint<IResult, ListPagedCatalogItemRequest>
{
    private IRepository<CatalogItem> _itemRepository;
    private readonly IUriComposer _uriComposer;
    private readonly IMapper _mapper;

    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;

    public CatalogItemListPagedEndpoint(IUriComposer uriComposer, IMapper mapper
        , ILogger<CatalogItemListPagedEndpoint> logger, IConfiguration configuration)
    {
        _uriComposer = uriComposer;
        _mapper = mapper;
        _logger = logger;
        _configuration = configuration;
    }

    public void AddRoute(IEndpointRouteBuilder app)
    {
        app.MapGet("api/catalog-items",
            async (int? pageSize, int? pageIndex, int? catalogBrandId, int? catalogTypeId, IRepository<CatalogItem> itemRepository) =>
            {
                _itemRepository = itemRepository;
                return await HandleAsync(new ListPagedCatalogItemRequest(pageSize, pageIndex, catalogBrandId, catalogTypeId));
            })            
            .Produces<ListPagedCatalogItemResponse>()
            .WithTags("CatalogItemEndpoints");
    }

    public async Task<IResult> HandleAsync(ListPagedCatalogItemRequest request)
    {
        //var valueFromVault = _configuration.GetConnectionString("TestKeyVault");
        var valueFromVault = _configuration["testSecret"];
        return Results.Ok($"value from Vault{valueFromVault}");
        var response = new ListPagedCatalogItemResponse(request.CorrelationId());

        var filterSpec = new CatalogFilterSpecification(request.CatalogBrandId, request.CatalogTypeId);
        int totalItems = await _itemRepository.CountAsync(filterSpec);

        var pagedSpec = new CatalogFilterPaginatedSpecification(
            skip: request.PageIndex.Value * request.PageSize.Value,
            take: request.PageSize.Value,
            brandId: request.CatalogBrandId,
            typeId: request.CatalogTypeId);

        var items = await _itemRepository.ListAsync(pagedSpec);

        response.CatalogItems.AddRange(items.Select(_mapper.Map<CatalogItemDto>));
        foreach (CatalogItemDto item in response.CatalogItems)
        {
            item.PictureUri = _uriComposer.ComposePicUri(item.PictureUri);
        }

        if (request.PageSize > 0)
        {
            response.PageCount = int.Parse(Math.Ceiling((decimal)totalItems / request.PageSize.Value).ToString());
        }
        else
        {
            response.PageCount = totalItems > 0 ? 1 : 0;
        }

        //_logger.LogInformation($"catalog-items count - {totalItems}");
        //_logger.LogError($"test-error - {totalItems}");
        //_logger.LogWarning($"test-warning - {totalItems}");

        //var exc = new Exception("test");
        //_logger.LogError(exc, "test with exc");
        //try
        //{
        //    throw exc;
        //}
        //catch(Exception e)
        //{
        //    _logger.LogError(e, "test with exc from catch");
        //    throw;
        //}
        return Results.Ok(response);
    }
}
