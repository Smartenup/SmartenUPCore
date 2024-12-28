using Nop.Services.Shipping;

namespace SmartenUP.Core.Services.Shippping
{
    public partial interface ISUPShippingRateComputationMethod : IShippingRateComputationMethod
    {


        /// <summary>
        ///  Gets available shipping options
        /// </summary>
        /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
        /// <returns>Represents a response of getting shipping rate options</returns>
        GetShippingOptionResponse GetShippingOptions(SUPGetShippingOptionRequest getShippingOptionRequest);

        /// <summary>
        ///  Gets available shipping options
        /// </summary>
        /// <param name="getShippingOptionRequest">A request of product for getting shipping options</param>
        /// <returns>Represents a response of getting shipping rate options</returns>
        GetShippingOptionResponse GetShippingOptions(SUPGetShippingOptionProductRequest getShippingOptionProductRequest);
    }
}
