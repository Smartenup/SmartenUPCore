using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Data;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Core.Plugins;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Shipping;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;

namespace SmartenUP.Core.Services.Shippping
{
    public partial class SUPShippingService : ShippingService, ISUPShippingService
    {
        private readonly ShippingSettings _shippingSettings;
        private readonly IAddressService _addressService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IProductService _productService;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        private readonly IPluginFinder _pluginFinder;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IStoreContext _storeContext;

        public SUPShippingService(IRepository<ShippingMethod> shippingMethodRepository,
            IRepository<DeliveryDate> deliveryDateRepository,
            IRepository<Warehouse> warehouseRepository,
            ILogger logger,
            IProductService productService,
            IProductAttributeParser productAttributeParser,
            ICheckoutAttributeParser checkoutAttributeParser,
            IGenericAttributeService genericAttributeService,
            ILocalizationService localizationService,
            IAddressService addressService,
            ShippingSettings shippingSettings,
            IPluginFinder pluginFinder,
            IStoreContext storeContext,
            IEventPublisher eventPublisher,
            ShoppingCartSettings shoppingCartSettings,
            ICacheManager cacheManager) : 
            base(
                shippingMethodRepository, 
                deliveryDateRepository, 
                warehouseRepository, 
                logger,  
                productService,
                productAttributeParser,
                checkoutAttributeParser,
                genericAttributeService,
                localizationService,
                addressService,
                shippingSettings,
                pluginFinder,
                storeContext,
                eventPublisher,
                shoppingCartSettings,
                cacheManager
                )
        {
            _shippingSettings = shippingSettings;
            _addressService = addressService;
            _productAttributeParser = productAttributeParser;
            _productService = productService;
            _shoppingCartSettings = shoppingCartSettings;
            _localizationService = localizationService;
            _logger = logger;
            _checkoutAttributeParser = checkoutAttributeParser;
            _pluginFinder = pluginFinder;
            _genericAttributeService = genericAttributeService;
            _storeContext = storeContext;
        }





        /// <summary>
        /// Get shipping options of order
        /// </summary>
        /// <param name="order">Order placed</param>
        /// <returns>Shipping option</returns>
        public virtual ShippingOption GetShippingOption(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            if (string.IsNullOrWhiteSpace(order.ShippingMethod))
                return null;

            var result = new ShippingOption();

            bool shippingFromMultipleLocations;
            var shippingOptionRequests = CreateShippingOptionRequests(order, out shippingFromMultipleLocations);
            
            ISUPShippingRateComputationMethod shippingRateComputationMethod = LoadSUPShippingRateComputationMethodBySystemName(order.ShippingRateComputationMethodSystemName);

            if (shippingRateComputationMethod == null)
                throw new NopException("Shipping rate computation method could not be loaded");


            //request shipping options (separately for each package-request)
            IList<ShippingOption> srcmShippingOptions = null;

            foreach (var shippingOptionRequest in shippingOptionRequests)
            {
                var getShippingOptionResponse = shippingRateComputationMethod.GetShippingOptions(shippingOptionRequest);

                if (getShippingOptionResponse.Success)
                {
                    //success
                    if (srcmShippingOptions == null)
                    {
                        //first shipping option request
                        srcmShippingOptions = getShippingOptionResponse.ShippingOptions;
                    }
                    else
                    {
                        //get shipping options which already exist for prior requested packages for this scrm (i.e. common options)
                        srcmShippingOptions = srcmShippingOptions
                            .Where(existingso => getShippingOptionResponse.ShippingOptions.Any(newso => newso.Name == existingso.Name))
                            .ToList();

                        //and sum the rates
                        foreach (var existingso in srcmShippingOptions)
                        {
                            existingso.Rate += getShippingOptionResponse
                                .ShippingOptions
                                .First(newso => newso.Name == existingso.Name)
                                .Rate;
                        }
                    }
                }
                else
                {
                    //errors
                    foreach (string error in getShippingOptionResponse.Errors)
                    {
                        _logger.Warning(string.Format("Shipping ({0}). {1}", shippingRateComputationMethod.PluginDescriptor.FriendlyName, error));
                    }
                    //clear the shipping options in this case
                    srcmShippingOptions = new List<ShippingOption>();
                    break;
                }
            }

            //no shipping options loaded
            if (srcmShippingOptions.Count == 0)
                throw new Exception(_localizationService.GetResource("Checkout.ShippingOptionCouldNotBeLoaded"));


            //add this option to the result
            if (srcmShippingOptions.Count > 1)
                throw new Exception("Just one shipping method may have been returned");

            foreach (var so in srcmShippingOptions)
            {
                //set system name if not set yet
                if (String.IsNullOrEmpty(so.ShippingRateComputationMethodSystemName))
                    so.ShippingRateComputationMethodSystemName = order.ShippingRateComputationMethodSystemName;

                if (_shoppingCartSettings.RoundPricesDuringCalculation)
                    so.Rate = RoundingHelper.RoundPrice(so.Rate);

                result = so;
            }

            return result;

        }

        /// <summary>
        /// Create shipment packages (requests) from shopping cart
        /// </summary>
        /// <param name="cart">Order placed</param>
        /// <param name="storeId">Load records allowed only in a specified store; pass 0 to load all records</param>
        /// <param name="shippingFromMultipleLocations">Value indicating whether shipping is done from multiple locations (warehouses)</param>
        /// <returns>Shipment packages (requests)</returns>
        public virtual IList<SUPGetShippingOptionRequest> CreateShippingOptionRequests(Order order, 
            out bool shippingFromMultipleLocations)
        {
            //if we always ship from the default shipping origin, then there's only one request
            //if we ship from warehouses ("ShippingSettings.UseWarehouseLocation" enabled),
            //then there could be several requests


            //key - warehouse identifier (0 - default shipping origin)
            //value - request
            var requests = new Dictionary<int, SUPGetShippingOptionRequest>();

            //a list of requests with products which should be shipped separately
            var separateRequests = new List<SUPGetShippingOptionRequest>();

            foreach (var item in order.OrderItems)
            {
                var product = item.Product;

                //warehouses
                Warehouse warehouse = null;
                if (_shippingSettings.UseWarehouseLocation)
                {
                    if (product.ManageInventoryMethod == ManageInventoryMethod.ManageStock &&
                        product.UseMultipleWarehouses)
                    {
                        var allWarehouses = new List<Warehouse>();
                        //multiple warehouses supported
                        foreach (var pwi in product.ProductWarehouseInventory)
                        {
                            //TODO validate stock quantity when backorder is not allowed?
                            var tmpWarehouse = GetWarehouseById(pwi.WarehouseId);
                            if (tmpWarehouse != null)
                                allWarehouses.Add(tmpWarehouse);
                        }
                        warehouse = GetNearestWarehouse(order.ShippingAddress, allWarehouses);
                    }
                    else
                    {
                        //multiple warehouses are not supported
                        warehouse = GetWarehouseById(product.WarehouseId);
                    }
                }
                int warehouseId = warehouse != null ? warehouse.Id : 0;

                if (requests.ContainsKey(warehouseId) && !product.ShipSeparately)
                {
                    //add item to existing request
                    requests[warehouseId].Items.Add(new SUPGetShippingOptionRequest.SUPPackageItem(item));
                }
                else
                {
                    //create a new request
                    var request = new SUPGetShippingOptionRequest();
                    //store
                    request.StoreId = order.StoreId;
                    //add item
                    request.SUPItems.Add(new SUPGetShippingOptionRequest.SUPPackageItem(item));

                    //customer
                    request.Customer = order.Customer;
                    //ship to
                    request.ShippingAddress = order.ShippingAddress;
                    //ship from
                    Address originAddress = null;
                    if (warehouse != null)
                    {
                        //warehouse address
                        originAddress = _addressService.GetAddressById(warehouse.AddressId);
                        request.WarehouseFrom = warehouse;
                    }
                    if (originAddress == null)
                    {
                        //no warehouse address. in this case use the default shipping origin
                        originAddress = _addressService.GetAddressById(_shippingSettings.ShippingOriginAddressId);
                    }
                    if (originAddress != null)
                    {
                        request.CountryFrom = originAddress.Country;
                        request.StateProvinceFrom = originAddress.StateProvince;
                        request.ZipPostalCodeFrom = originAddress.ZipPostalCode;
                        request.CityFrom = originAddress.City;
                        request.AddressFrom = originAddress.Address1;
                    }

                    request.ShippingMethod = order.ShippingMethod;
                    request.ShippingRateComputationMethodSystemName = order.ShippingRateComputationMethodSystemName;
                    request.Order = order;
                    request.IsOrderBasead = true;

                    if (product.ShipSeparately)
                    {
                        //ship separately
                        separateRequests.Add(request);
                    }
                    else
                    {
                        //usual request
                        requests.Add(warehouseId, request);
                    }

                    
                }
            }

            //multiple locations?
            //currently we just compare warehouses
            //but we should also consider cases when several warehouses are located in the same address
            shippingFromMultipleLocations = requests.Select(x => x.Key).Distinct().Count() > 1;


            var result = requests.Values.ToList();
            result.AddRange(separateRequests);

            

            return result;
        }


        /// <summary>
        /// Get total dimensions
        /// </summary>
        /// <param name="packageItems">Package items</param>
        /// <param name="width">Width</param>
        /// <param name="length">Length</param>
        /// <param name="height">Height</param>
        public virtual void GetDimensionsByOrder(IList<SUPGetShippingOptionRequest.SUPPackageItem> packageItems,
            out decimal width, out decimal length, out decimal height)
        {
            if (packageItems == null)
                throw new ArgumentNullException("packageItems");

            if (_shippingSettings.UseCubeRootMethod)
            {
                //cube root of volume
                decimal totalVolume = 0;
                decimal maxProductWidth = 0;
                decimal maxProductLength = 0;
                decimal maxProductHeight = 0;
                foreach (var packageItem in packageItems)
                {
                    var orderItem = packageItem.OrderItem;
                    var product = orderItem.Product;
                    var qty = packageItem.GetQuantityByOrder();

                    //associated products
                    decimal associatedProductsWidth;
                    decimal associatedProductsLength;
                    decimal associatedProductsHeight;
                    GetAssociatedProductDimensions(orderItem, out associatedProductsWidth,
                        out associatedProductsLength, out associatedProductsHeight);

                    var productWidth = product.Width + associatedProductsWidth;
                    var productLength = product.Length + associatedProductsLength;
                    var productHeight = product.Height + associatedProductsHeight;

                    //we do not use cube root method when we have only one item with "qty" set to 1
                    if (packageItems.Count == 1 && qty == 1)
                    {
                        width = productWidth;
                        length = productLength;
                        height = productHeight;
                        return;
                    }

                    totalVolume += qty * productHeight * productWidth * productLength;

                    if (productWidth > maxProductWidth)
                        maxProductWidth = productWidth;
                    if (productLength > maxProductLength)
                        maxProductLength = productLength;
                    if (productHeight > maxProductHeight)
                        maxProductHeight = productHeight;
                }
                decimal dimension = Convert.ToDecimal(Math.Pow(Convert.ToDouble(totalVolume), (double)(1.0 / 3.0)));
                length = width = height = dimension;

                //sometimes we have products with sizes like 1x1x20
                //that's why let's ensure that a maximum dimension is always preserved
                //otherwise, shipping rate computation methods can return low rates
                if (width < maxProductWidth)
                    width = maxProductWidth;
                if (length < maxProductLength)
                    length = maxProductLength;
                if (height < maxProductHeight)
                    height = maxProductHeight;
            }
            else
            {
                //summarize all values (very inaccurate with multiple items)
                width = length = height = decimal.Zero;
                foreach (var packageItem in packageItems)
                {
                    var shoppingCartItem = packageItem.ShoppingCartItem;
                    var product = shoppingCartItem.Product;
                    var qty = packageItem.GetQuantity();
                    width += product.Width * qty;
                    length += product.Length * qty;
                    height += product.Height * qty;

                    //associated products
                    decimal associatedProductsWidth;
                    decimal associatedProductsLength;
                    decimal associatedProductsHeight;
                    GetAssociatedProductDimensions(shoppingCartItem, out associatedProductsWidth,
                        out associatedProductsLength, out associatedProductsHeight);

                    width += associatedProductsWidth;
                    length += associatedProductsLength;
                    height += associatedProductsHeight;
                }
            }
        }

        /// <summary>
        /// Get dimensions of associated products (for quantity 1)
        /// </summary>
        /// <param name="shoppingCartItem">Shopping cart item</param>
        /// <param name="width">Width</param>
        /// <param name="length">Length</param>
        /// <param name="height">Height</param>
        public virtual void GetAssociatedProductDimensions(OrderItem orderItem,
            out decimal width, out decimal length, out decimal height)
        {
            if (orderItem == null)
                throw new ArgumentNullException("orderItem");

            width = length = height = decimal.Zero;

            //attributes
            if (String.IsNullOrEmpty(orderItem.AttributesXml))
                return;

            //bundled products (associated attributes)
            var attributeValues = _productAttributeParser.ParseProductAttributeValues(orderItem.AttributesXml)
                .Where(x => x.AttributeValueType == AttributeValueType.AssociatedToProduct)
                .ToList();
            foreach (var attributeValue in attributeValues)
            {
                var associatedProduct = _productService.GetProductById(attributeValue.AssociatedProductId);
                if (associatedProduct != null && associatedProduct.IsShipEnabled)
                {
                    width += associatedProduct.Width * attributeValue.Quantity;
                    length += associatedProduct.Length * attributeValue.Quantity;
                    height += associatedProduct.Height * attributeValue.Quantity;
                }
            }
        }




        /// <summary>
        /// Gets order weight
        /// </summary>
        /// <param name="request">Request</param>
        /// <param name="includeCheckoutAttributes">A value indicating whether we should calculate weights of selected checkotu attributes</param>
        /// <returns>Total weight</returns>
        public virtual decimal GetTotalWeightByOrder(SUPGetShippingOptionRequest request, bool includeCheckoutAttributes = true)
        {
            if (request == null)
                throw new ArgumentNullException("request");

            Customer customer = request.Customer;

            decimal totalWeight = decimal.Zero;
            //shopping cart items
            foreach (var packageItem in request.SUPItems)
                totalWeight += GetOrderItemWeight(packageItem.OrderItem) * packageItem.GetQuantityByOrder();

            //checkout attributes
            if (customer != null && includeCheckoutAttributes)
            {
                var checkoutAttributesXml = customer.GetAttribute<string>(SystemCustomerAttributeNames.CheckoutAttributes, _genericAttributeService, _storeContext.CurrentStore.Id);
                if (!String.IsNullOrEmpty(checkoutAttributesXml))
                {
                    var attributeValues = _checkoutAttributeParser.ParseCheckoutAttributeValues(checkoutAttributesXml);
                    foreach (var attributeValue in attributeValues)
                        totalWeight += attributeValue.WeightAdjustment;
                }
            }
            return totalWeight;
        }


        /// <summary>
        /// Gets Order item weight (of one item)
        /// </summary>
        /// <param name="orderItem">Order item</param>
        /// <returns>Order item weight</returns>
        public virtual decimal GetOrderItemWeight(OrderItem orderItem)
        {
            if (orderItem == null)
                throw new ArgumentNullException("orderItem");

            if (orderItem.Product == null)
                return decimal.Zero;

            //attribute weight
            decimal attributesTotalWeight = decimal.Zero;
            if (!String.IsNullOrEmpty(orderItem.AttributesXml))
            {
                var attributeValues = _productAttributeParser.ParseProductAttributeValues(orderItem.AttributesXml);
                foreach (var attributeValue in attributeValues)
                {
                    switch (attributeValue.AttributeValueType)
                    {
                        case AttributeValueType.Simple:
                            {
                                //simple attribute
                                attributesTotalWeight += attributeValue.WeightAdjustment;
                            }
                            break;
                        case AttributeValueType.AssociatedToProduct:
                            {
                                //bundled product
                                var associatedProduct = _productService.GetProductById(attributeValue.AssociatedProductId);
                                if (associatedProduct != null && associatedProduct.IsShipEnabled)
                                {
                                    attributesTotalWeight += associatedProduct.Weight * attributeValue.Quantity;
                                }
                            }
                            break;
                    }
                }
            }

            var weight = orderItem.Product.Weight + attributesTotalWeight;
            return weight;
        }



        /// <summary>
        /// Gets available shipping options for a product 
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="shippingAddress"></param>
        /// <param name="storeId"></param>
        /// <returns></returns>
        public GetShippingOptionResponse GetShippingOptions(int productId, Address shippingAddress, 
            Customer customer, int storeId = 0, FormCollection formProductDetails = null)
        {

            var result = new GetShippingOptionResponse();

            //create a package
            SUPGetShippingOptionProductRequest shippingOptionProductRequest = CreateShippingOptionProductRequest(productId, 
                shippingAddress, storeId, customer, formProductDetails);

            result.ShippingFromMultipleLocations = false;

            IList<ISUPShippingRateComputationMethod> shippingRateComputationMethods = LoadActiveSUPShippingRateComputationMethods(storeId);

            if (!shippingRateComputationMethods.Any())
                //throw new NopException("Shipping rate computation method could not be loaded");
                return result;

            //request shipping options from each shipping rate computation methods
            foreach (var srcm in shippingRateComputationMethods)
            {
                //request shipping options (separately for each package-request)
                IList<ShippingOption> srcmShippingOptions = null;

                var getShippingOptionResponse = srcm.GetShippingOptions(shippingOptionProductRequest);

                if (getShippingOptionResponse.Success)
                {
                    //first shipping option request
                    srcmShippingOptions = getShippingOptionResponse.ShippingOptions;
                }
                else
                {
                    //errors
                    foreach (string error in getShippingOptionResponse.Errors)
                    {
                        result.AddError(error);
                        _logger.Warning(string.Format("Shipping ({0}). {1}", srcm.PluginDescriptor.FriendlyName, error));
                    }
                    //clear the shipping options in this case
                    srcmShippingOptions = new List<ShippingOption>();
                }


                //add this scrm's options to the result
                if (srcmShippingOptions != null)
                {
                    foreach (var so in srcmShippingOptions)
                    {
                        //set system name if not set yet
                        if (String.IsNullOrEmpty(so.ShippingRateComputationMethodSystemName))
                            so.ShippingRateComputationMethodSystemName = srcm.PluginDescriptor.SystemName;
                        if (_shoppingCartSettings.RoundPricesDuringCalculation)
                            so.Rate = RoundingHelper.RoundPrice(so.Rate);
                        result.ShippingOptions.Add(so);
                    }
                }
            }

            if (_shippingSettings.ReturnValidOptionsIfThereAreAny)
            {
                //return valid options if there are any (no matter of the errors returned by other shipping rate compuation methods).
                if (result.ShippingOptions.Any() && result.Errors.Any())
                    result.Errors.Clear();
            }

            //no shipping options loaded
            if (!result.ShippingOptions.Any() && !result.Errors.Any())
                result.Errors.Add(_localizationService.GetResource("Checkout.ShippingOptionCouldNotBeLoaded"));

            return result;

        }


        /// <summary>
        /// Create shipment packages (requests) from shopping cart
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <param name="shippingAddress">Shipping address</param>
        /// <param name="storeId">Load records allowed only in a specified store; pass 0 to load all records</param>
        /// <param name="shippingFromMultipleLocations">Value indicating whether shipping is done from multiple locations (warehouses)</param>
        /// <returns>Shipment packages (requests)</returns>
        public virtual SUPGetShippingOptionProductRequest CreateShippingOptionProductRequest(int productId,
            Address shippingAddress, int storeId, Customer customer, FormCollection formProductDetails = null)
        {
            //if we always ship from the default shipping origin, then there's only one request
            //if we ship from warehouses ("ShippingSettings.UseWarehouseLocation" enabled),
            //then there could be several requests


            //key - warehouse identifier (0 - default shipping origin)
            //value - request

            var product = _productService.GetProductById(productId);

            #region Quantity

            int quantity = GetQuantity(productId, formProductDetails);

            #endregion


            //warehouses
            Warehouse warehouse = null;
            if (_shippingSettings.UseWarehouseLocation)
            {
                if (product.ManageInventoryMethod == ManageInventoryMethod.ManageStock &&
                    product.UseMultipleWarehouses)
                {
                    var allWarehouses = new List<Warehouse>();
                    //multiple warehouses supported
                    foreach (var pwi in product.ProductWarehouseInventory)
                    {
                        //TODO validate stock quantity when backorder is not allowed?
                        var tmpWarehouse = GetWarehouseById(pwi.WarehouseId);
                        if (tmpWarehouse != null)
                            allWarehouses.Add(tmpWarehouse);
                    }
                    warehouse = GetNearestWarehouse(shippingAddress, allWarehouses);
                }
                else
                {
                    //multiple warehouses are not supported
                    warehouse = GetWarehouseById(product.WarehouseId);
                }
            }
            int warehouseId = warehouse != null ? warehouse.Id : 0;


            //create a new request
            var request = new SUPGetShippingOptionProductRequest();
            //store
            request.StoreId = storeId;
            //add item
            request.Product = product;
            request.Quantity = quantity;
            //customer
            request.Customer = customer;
            //ship to
            request.ShippingAddress = shippingAddress;
            //ship from
            Address originAddress = null;
            if (warehouse != null)
            {
                //warehouse address
                originAddress = _addressService.GetAddressById(warehouse.AddressId);
                request.WarehouseFrom = warehouse;
            }
            if (originAddress == null)
            {
                //no warehouse address. in this case use the default shipping origin
                originAddress = _addressService.GetAddressById(_shippingSettings.ShippingOriginAddressId);
            }
            if (originAddress != null)
            {
                request.CountryFrom = originAddress.Country;
                request.StateProvinceFrom = originAddress.StateProvince;
                request.ZipPostalCodeFrom = originAddress.ZipPostalCode;
                request.CityFrom = originAddress.City;
                request.AddressFrom = originAddress.Address1;
            }

            return request;
        }

        private int GetQuantity(int productId, FormCollection formProductDetails)
        {
            int quantity = 1;

            var nvc = new NameValueCollection();

            foreach (string vp in Regex.Split(formProductDetails["formProductDetails"], "&"))
            {
                string[] singlePair = Regex.Split(vp, "=");
                if (singlePair.Length == 2)
                {
                    nvc.Add(singlePair[0], singlePair[1]);
                }
            }

            foreach (string key in nvc.AllKeys)
            {
                if (key.Equals(string.Format("addtocart_{0}.EnteredQuantity", productId), StringComparison.InvariantCultureIgnoreCase))
                {
                    int.TryParse(nvc[key], out quantity);
                    break;
                }
            }

            return quantity;
        }

        /// <summary>
        /// Get total dimensions
        /// </summary>
        /// <param name="packageItems">Package items</param>
        /// <param name="width">Width</param>
        /// <param name="length">Length</param>
        /// <param name="height">Height</param>
        public virtual void GetDimensions(Product product, int qty,
            out decimal width, out decimal length, out decimal height)
        {
            if (product == null)
                throw new ArgumentNullException("product");

            if (_shippingSettings.UseCubeRootMethod)
            {

                decimal totalVolume = 0;
                decimal maxProductWidth = 0;
                decimal maxProductLength = 0;
                decimal maxProductHeight = 0;


                var productWidth = product.Width;
                var productLength = product.Length;
                var productHeight = product.Height;

                //we do not use cube root method when we have only one item with "qty" set to 1
                if (qty == 1)
                {
                    width = productWidth;
                    length = productLength;
                    height = productHeight;
                    return;
                }


                totalVolume = qty * productHeight * productWidth * productLength;

                if (productWidth > maxProductWidth)
                    maxProductWidth = productWidth;
                if (productLength > maxProductLength)
                    maxProductLength = productLength;
                if (productHeight > maxProductHeight)
                    maxProductHeight = productHeight;


                decimal dimension = Convert.ToDecimal(Math.Pow(Convert.ToDouble(totalVolume), (double)(1.0 / 3.0)));
                length = width = height = dimension;

                //sometimes we have products with sizes like 1x1x20
                //that's why let's ensure that a maximum dimension is always preserved
                //otherwise, shipping rate computation methods can return low rates
                if (width < maxProductWidth)
                    width = maxProductWidth;
                if (length < maxProductLength)
                    length = maxProductLength;
                if (height < maxProductHeight)
                    height = maxProductHeight;

            }
            else
            {
                //summarize all values (very inaccurate with multiple items)
                width = length = height = decimal.Zero;

                width += product.Width * qty;
                length += product.Length * qty;
                height += product.Height * qty;

            }

        }



        /// <summary>
        /// Load active shipping rate computation methods
        /// </summary>
        /// <param name="storeId">Load records allowed only in a specified store; pass 0 to load all records</param>
        /// <returns>Shipping rate computation methods</returns>
        public virtual IList<ISUPShippingRateComputationMethod> LoadActiveSUPShippingRateComputationMethods(int storeId = 0)
        {
            return LoadAllSUPShippingRateComputationMethods(storeId)
                   .Where(provider => _shippingSettings.ActiveShippingRateComputationMethodSystemNames.Contains(provider.PluginDescriptor.SystemName, StringComparer.InvariantCultureIgnoreCase))
                   .ToList();
        }


        /// <summary>
        /// Load all shipping rate computation methods
        /// </summary>
        /// <param name="storeId">Load records allowed only in a specified store; pass 0 to load all records</param>
        /// <returns>Shipping rate computation methods</returns>
        public virtual IList<ISUPShippingRateComputationMethod> LoadAllSUPShippingRateComputationMethods(int storeId = 0)
        {
            return _pluginFinder.GetPlugins<ISUPShippingRateComputationMethod>(storeId: storeId).ToList();
        }


        /// <summary>
        /// Load shipping rate computation method by system name
        /// </summary>
        /// <param name="systemName">System name</param>
        /// <returns>Found Shipping rate computation method</returns>
        public virtual ISUPShippingRateComputationMethod LoadSUPShippingRateComputationMethodBySystemName(string systemName)
        {
            var descriptor = _pluginFinder.GetPluginDescriptorBySystemName<ISUPShippingRateComputationMethod>(systemName);
            if (descriptor != null)
                return descriptor.Instance<ISUPShippingRateComputationMethod>();

            return null;
        }
    }
}
