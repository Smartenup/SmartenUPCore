using Nop.Core.Domain.Orders;
using System.Text.RegularExpressions;

namespace SmartenUP.Core.Util.Helper
{
    public class ProductHelper
    {
        public static string AddItemDescrition(string productName, OrderItem item, int length = 100)
        {
            var attributeDescription = item.AttributeDescription;
            if (!string.IsNullOrWhiteSpace(attributeDescription))
            {
                attributeDescription = Regex.Replace(attributeDescription, @"<(.|\n)*?>", " - ");
            }
            productName = string.IsNullOrWhiteSpace(attributeDescription)
                ? productName
                : productName + " - " + attributeDescription;

            if (productName.Length > length)
            {
                return productName.Substring(0, length -1);
            }

            return productName;
        }

        public static string GetProcuctName(OrderItem item)
        {
            if (!string.IsNullOrWhiteSpace(item.Product.Name))
            {
                return item.Product.Name;
            }
            return "Nome não especificado";
        }
    }
}
