using Nop.Data.Mapping;
using SmartenUP.Core.Domain;

namespace SmartenUP.Core.Data
{
    public class HolidayMap : NopEntityTypeConfiguration<Holiday>
    {
        public HolidayMap()
        {
            this.ToTable("Holiday");
            this.HasKey(x => x.Id);
        }
    }
}
