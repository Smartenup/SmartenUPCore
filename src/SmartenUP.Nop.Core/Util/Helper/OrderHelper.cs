using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Shipping;
using SmartenUP.Core.Services;
using SmartenUP.Core.Util.Extensions;
using System;
using System.Linq;
using System.Text;

namespace SmartenUP.Core.Util.Helper
{
    public class OrderHelper
    {
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IWorkflowMessageService _workflowMessageService;
        private readonly IWorkContext _workContext;
        private readonly IShippingService _shippingService;
        private readonly IHolidayService _holidayService;
        private readonly ILogger _logger;

        public OrderHelper(
            IOrderService orderService,
            IOrderProcessingService orderProcessingService,
            IWorkflowMessageService workflowMessageService,
            IWorkContext workContext,
            IShippingService shippingService,
            IHolidayService holidayService
            )
        {
            _workContext = workContext;
            _orderService = orderService;
            _orderProcessingService = orderProcessingService;
            _workflowMessageService = workflowMessageService;
            _shippingService = shippingService;
            _holidayService = holidayService;
        }

        public void AddOrderNote(string note, bool showNoteToCustomer, Order order, bool sendEmail = false)
        {
            OrderNote orderNote = new OrderNote();
            orderNote.CreatedOnUtc = DateTime.UtcNow;
            orderNote.DisplayToCustomer = showNoteToCustomer;
            orderNote.Note = note;
            order.OrderNotes.Add(orderNote);

            _orderService.UpdateOrder(order);

            //new order notification
            if (sendEmail)
            {
                //email
                _workflowMessageService.SendNewOrderNoteAddedCustomerNotification(
                    orderNote, _workContext.WorkingLanguage.Id);
            }
        }

        public string GetOrdeNoteRecievedPayment(Order order)
        {
            OrderItem orderItem;
            int? biggestAmountDays;

            DeliveryDate biggestDeliveryDate = GetBiggestDeliveryDate(order, out biggestAmountDays, out orderItem);

            DateTime dateShipment = DateTime.Now.AddWorkDays(biggestAmountDays.Value, );

            var str = new StringBuilder();

            str.AppendLine("Recebemos a liberação do pagamento pelo PagSeguro e será dado andamento no seu pedido.");
            str.AppendLine();
            str.AppendFormat("Lembramos que o maior prazo é da fabricante {0} de {1}",
                            orderItem.Product.ProductManufacturers.FirstOrDefault().Manufacturer.Name,
                            biggestDeliveryDate.GetLocalized(dd => dd.Name));
            str.AppendLine();
            str.AppendLine();
            str.AppendLine("*OBS: Caso o seu pedido tenha produtos com prazos diferentes, o prazo de entrega a ser considerado será o maior.");
            str.AppendLine();


            str.AppendFormat("Data máxima para postar nos correios: {0}", dateShipment.ToString("dd/MM/yyyy"));
            str.AppendLine();

            if (order.ShippingMethod.Contains("PAC") || order.ShippingMethod.Contains("SEDEX"))
            {
                try
                {
                    var shippingOption = _shippingService.GetShippingOption(order);

                    str.AppendFormat("Correios: {0} - {1} após a postagem", shippingOption.Name, shippingOption.Description);
                    str.AppendLine();
                }
                catch (Exception ex)
                {
                    _logger.Error("Erro no calculo do frete pela ordem", ex);
                }
                finally
                {
                    str.AppendLine();
                }
            }

            return str.ToString();

        }

        private DeliveryDate GetBiggestDeliveryDate(Order order, out int? biggestAmountDays, out OrderItem orderItem)
        {

            DeliveryDate deliveryDate = null;

            biggestAmountDays = 0;

            orderItem = null;

            foreach (var item in order.OrderItems)
            {
                var deliveryDateItem = _shippingService.GetDeliveryDateById(item.Product.DeliveryDateId);

                string deliveryDateText = deliveryDateItem.GetLocalized(dd => dd.Name);

                int? deliveryBigestInteger = GetBiggestInteger(deliveryDateText);

                if (deliveryBigestInteger.HasValue)
                {
                    if (deliveryBigestInteger.Value > biggestAmountDays)
                    {
                        biggestAmountDays = deliveryBigestInteger.Value;
                        deliveryDate = deliveryDateItem;
                        orderItem = item;
                    }
                }
            }


            return deliveryDate;
        }
        [NonAction]
        private int? GetBiggestInteger(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var integerResultsList = new List<int>();
            string integerSituation = string.Empty;
            int integerPosition = 0;

            for (int i = 0; i < text.Length; i++)
            {
                if (int.TryParse(text[i].ToString(), out integerPosition))
                {
                    integerSituation += text[i].ToString();
                }
                else
                {
                    if (!string.IsNullOrEmpty(integerSituation))
                    {
                        integerResultsList.Add(int.Parse(integerSituation));
                        integerSituation = string.Empty;
                    }
                }
            }

            int integerResult = 0;
            foreach (var item in integerResultsList)
            {
                if (item > integerResult)
                    integerResult = item;
            }


            return integerResult;
        }
    }
}
