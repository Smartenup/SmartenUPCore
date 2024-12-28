using Nop.Core.Domain.Orders;
using Nop.Services.Shipping;
using System.Collections.Generic;

namespace SmartenUP.Core.Services.Shippping
{
    public partial class SUPGetShippingOptionRequest : GetShippingOptionRequest
    {

        public SUPGetShippingOptionRequest() { 

        }
        public SUPGetShippingOptionRequest(GetShippingOptionRequest request)
        {
            Customer = request.Customer;
            Items = request.Items;
            ShippingAddress = request.ShippingAddress;
            WarehouseFrom = request.WarehouseFrom;
            CountryFrom = request.CountryFrom;
            StateProvinceFrom = request.StateProvinceFrom;
            ZipPostalCodeFrom = request.ZipPostalCodeFrom;
            CityFrom = request.CityFrom;
            AddressFrom = request.AddressFrom;
            StoreId = request.StoreId;

            SUPItems = new List<SUPPackageItem>();

            foreach (var item in Items) 
            {
                SUPItems.Add(
                    new SUPPackageItem(
                        item.ShoppingCartItem, 
                        item.GetQuantity()
                        )
                    );
            }
        }

        private bool _isOrderBasead = false;

        /// <summary>
        /// Means that the Shipping rate is basead in the placed order
        /// </summary>
        public bool IsOrderBasead
        {
            get { return _isOrderBasead; }
            set { _isOrderBasead = value; }
        }

        /// <summary>
        /// Gets or sets a shipping method name
        /// </summary>
        public string ShippingMethod { get; set; }

        /// <summary>
        /// Gets or sets the system name of shipping rate computation method
        /// </summary>
        public string ShippingRateComputationMethodSystemName { get; set; }
        
        /// <summary>
        /// Gest or set the order placed
        /// </summary>
        public Order Order { get; set; }

        /// <summary>
        /// Gets or sets a shopping cart items
        /// </summary>
        public IList<SUPPackageItem> SUPItems { get; set; }


        public partial class SUPPackageItem: GetShippingOptionRequest.PackageItem
        {

            public SUPPackageItem(ShoppingCartItem sci, int? qty = null):
                base(sci, qty)
            { 
            
            }


            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="sci">Order item</param>
            /// <param name="qty">Override "Quantity" property of shopping cart item</param>
            public SUPPackageItem(OrderItem orderItem, int? qty = null):
                base(null, qty)
            {
                this.OrderItem = orderItem;
                this.OverriddenQuantity = qty;
            }


            /// <summary>
            /// Shopping cart item
            /// </summary>
            public OrderItem OrderItem { get; set; }


            public int GetQuantityByOrder()
            {
                if (OverriddenQuantity.HasValue)
                    return OverriddenQuantity.Value;

                return OrderItem.Quantity;
            }
        }
    }
}
