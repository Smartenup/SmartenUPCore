using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Services.Shipping;
using System.Collections.Generic;
using System.Web.Mvc;

namespace SmartenUP.Core.Services.Shippping
{
    public partial interface ISUPShippingService : IShippingService
    {
        /// <summary>
        /// Get shipping options of order
        /// </summary>
        /// <param name="order">Order placed</param>
        /// <returns>Shipping option</returns>
        ShippingOption GetShippingOption(Order order);


        /// <summary>
        /// Get total dimensions
        /// </summary>
        /// <param name="packageItems">Package items</param>
        /// <param name="width">Width</param>
        /// <param name="length">Length</param>
        /// <param name="height">Height</param>
        void GetDimensionsByOrder(IList<SUPGetShippingOptionRequest.SUPPackageItem> packageItems,
            out decimal width, out decimal length, out decimal height);


        /// <summary>
        /// Get dimensions of associated products (for quantity 1)
        /// </summary>
        /// <param name="orderItem">Order item</param>
        /// <param name="width">Width</param>
        /// <param name="length">Length</param>
        /// <param name="height">Height</param>
        void GetAssociatedProductDimensions(OrderItem orderItem,
            out decimal width, out decimal length, out decimal height);



        /// <summary>
        /// Gets order weight
        /// </summary>
        /// <param name="request">Request</param>
        /// <param name="includeCheckoutAttributes">A value indicating whether we should calculate weights of selected checkotu attributes</param>
        /// <returns>Total weight</returns>
        decimal GetTotalWeightByOrder(SUPGetShippingOptionRequest request, bool includeCheckoutAttributes = true);



        /// <summary>
        /// Gets Order item weight (of one item)
        /// </summary>
        /// <param name="orderItem">Order item</param>
        /// <returns>Order item weight</returns>
        decimal GetOrderItemWeight(OrderItem orderItem);


        /// <summary>
        /// Gets available shipping options for a product 
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="shippingAddress"></param>
        /// <param name="storeId"></param>
        /// <returns></returns>
        GetShippingOptionResponse GetShippingOptions(int productId, Address shippingAddress, 
            Customer customer, int storeId = 0, FormCollection formProductDetails = null);


        /// <summary>
        /// Get total dimensions of a product
        /// </summary>
        /// <param name="product">product</param>
        /// <param name="width">Width</param>
        /// <param name="length">Length</param>
        /// <param name="height">Height</param>
        void GetDimensions(Product product, int qty,
            out decimal width, out decimal length, out decimal height);


    }
}
