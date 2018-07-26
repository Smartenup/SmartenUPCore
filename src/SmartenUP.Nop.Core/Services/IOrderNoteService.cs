using Nop.Core.Domain.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartenUP.Core.Services
{
    public interface IOrderNoteService
    {
        void AddOrderNote(string note, bool showNoteToCustomer, Order order, bool sendEmail = false);

        string GetOrdeNoteRecievedPayment(Order order, string meioPagamento);
    }
}
