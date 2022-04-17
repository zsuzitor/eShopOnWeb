using System;
using System.Collections.Generic;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;

namespace Microsoft.eShopWeb.ApplicationCore.Integration;
public class OrderIntegration
{
    public int Id { get; set; }
    public string id => Id.ToString();

    public string BuyerId { get; set; }
    public DateTimeOffset OrderDate { get; set; }
    public Address ShipToAddress { get; set; }
    public List<OrderItem> OrderItems { get; set; }


    public OrderIntegration()
    {
        OrderItems = new List<OrderItem>();
        OrderDate = DateTimeOffset.Now;
    }

    public OrderIntegration(Order order) : this()
    {
        BuyerId = order.BuyerId;
        OrderDate = order.OrderDate;
        ShipToAddress = order.ShipToAddress;
        OrderItems.AddRange(order.OrderItems);
    }
}
