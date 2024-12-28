using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Shipping;

namespace SmartenUP.Core.Services.Shippping
{
    /// <summary>
    /// Represents a request for getting shipping rate options for a unique product
    /// </summary>
    public partial class SUPGetShippingOptionProductRequest
    {

        #region Properties

        /// <summary>
        /// Gets or sets a customer
        /// </summary>
        public virtual Customer Customer { get; set; }

        /// <summary>
        /// Gets or sets a shipping address (where we ship to)
        /// </summary>
        public Address ShippingAddress { get; set; }

        /// <summary>
        /// Shipped from warehouse
        /// </summary>
        public Warehouse WarehouseFrom { get; set; }
        /// <summary>
        /// Shipped from country
        /// </summary>
        public Country CountryFrom { get; set; }
        /// <summary>
        /// Shipped from state/province
        /// </summary>
        public StateProvince StateProvinceFrom { get; set; }
        /// <summary>
        /// Shipped from zip/postal code
        /// </summary>
        public string ZipPostalCodeFrom { get; set; }
        /// <summary>
        /// Shipped from city
        /// </summary>
        public string CityFrom { get; set; }
        /// <summary>
        /// Shipped from address
        /// </summary>
        public string AddressFrom { get; set; }

        /// <summary>
        /// Limit to store (identifier)
        /// </summary>
        public int StoreId { get; set; }

        /// <summary>
        /// Product that want to estimate shipping
        /// </summary>
        public Product Product { get; set; }

        /// <summary>
        /// Quantity of product that want to estimate shipping
        /// </summary>
        public int Quantity { get; set; }

        #endregion
    }
}
