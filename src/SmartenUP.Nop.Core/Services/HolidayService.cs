using Nop.Core.Caching;
using Nop.Core.Data;
using SmartenUP.Core.Domain;
using System;
using System.Linq;

namespace SmartenUP.Core.Services
{
    public class HolidayService : IHolidayService
    {
        private readonly IRepository<Holiday> _holidayRepository;
        private readonly ICacheManager _cacheManager;

        private const string HOLIDAY_DATA_BY_DATES = "SmartenUP.Nop.Core.HolidayDates";

        public HolidayService(IRepository<Holiday> holidayRepository,
            ICacheManager cacheManager)
        {
            _holidayRepository = holidayRepository;
            _cacheManager = cacheManager;
        }

        public bool IsHoliday(DateTime date)
        {
            IQueryable<Holiday> holidayDates = _cacheManager.Get(HOLIDAY_DATA_BY_DATES, () => GetList());

            foreach (var holiday in holidayDates)
            {
                if (holiday.HolidayDate.Date == date.Date)
                    return true;
            }

            return false;
        }

        private IQueryable<Holiday> GetList()
        {
            var rows = from myRow in _holidayRepository.Table
                       select myRow;

            return rows;
        }
    }
}
