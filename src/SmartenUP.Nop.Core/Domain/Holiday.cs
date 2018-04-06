using Nop.Core;
using System;

namespace SmartenUP.Core.Domain
{
    public class Holiday : BaseEntity
    {
        public DateTime HolidayDate { get; set; }

        public string Description { get; set; }
    }
}
