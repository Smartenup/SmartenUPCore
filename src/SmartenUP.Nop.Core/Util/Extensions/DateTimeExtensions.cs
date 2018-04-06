using SmartenUP.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartenUP.Core.Util.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime AddWorkDays(this DateTime date, int workingDays, IHolidayService holidayService)
        {
            return date.GetDates(workingDays < 0)
                .Where(newDate =>
                    (newDate.DayOfWeek != DayOfWeek.Saturday &&
                     newDate.DayOfWeek != DayOfWeek.Sunday &&
                     !newDate.IsHoliday(holidayService)))
                .Take(Math.Abs(workingDays))
                .Last();
        }

        private static IEnumerable<DateTime> GetDates(this DateTime date, bool isForward)
        {
            while (true)
            {
                date = date.AddDays(isForward ? -1 : 1);
                yield return date;
            }
        }

        public static bool IsHoliday(this DateTime date, IHolidayService holidayService)
        {
            bool retorno = false;

            retorno = holidayService.IsHoliday(date);

            return retorno;
        }
    }
}
