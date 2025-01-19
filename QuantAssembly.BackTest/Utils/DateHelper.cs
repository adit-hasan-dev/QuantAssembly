namespace QuantAssembly.BackTesting.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Alpaca.Markets;

    public static class DateHelper
    {
        private record struct MarketHours(
            DateTime EarlyOpen,
            DateTime NormalOpen,
            DateTime NormalClose,
            DateTime LateClose)
        {
            private static readonly TimeSpan _earlyOpenTime = new(4, 0, 0);
            private static readonly TimeSpan _normalOpenTime = new(9, 30, 0);
            private static readonly TimeSpan _normalCloseTime = new(16, 0, 0);
            private static readonly TimeSpan _lateCloseTime = new(20, 0, 0);

            public MarketHours(IIntervalCalendar calendar)
                : this(
                    calendar.Session.OpenEst.DateTime,
                    calendar.Trading.OpenEst.DateTime,
                    calendar.Trading.CloseEst.DateTime,
                    calendar.Session.CloseEst.DateTime)
            {
            }

            public MarketHours(DateTime date)
                : this(date, date, date, date)
            {
            }

            public static MarketHours CreateNormal(DateTime date) =>
                new(
                    date.Add(_earlyOpenTime),
                    date.Add(_normalOpenTime),
                    date.Add(_normalCloseTime),
                    date.Add(_lateCloseTime));

            public static MarketHours CreateLate(DateTime holiday) =>
                new(
                    holiday.Date.Add(_earlyOpenTime),
                    holiday.Date.Add(_normalOpenTime),
                    holiday,
                    holiday);

            public MarketStatus GetMarketStatus(DateTime dateTime)
            {
                if (IsWeekend(dateTime))
                {
                    return MarketStatus.Closed;
                }

                if (dateTime >= EarlyOpen && dateTime < NormalOpen)
                {
                    return MarketStatus.PreMarket;
                }

                if (dateTime > NormalClose && dateTime <= LateClose)
                {
                    return MarketStatus.PostMarket;
                }

                if (dateTime > LateClose || dateTime < EarlyOpen)
                {
                    return MarketStatus.Closed;
                }

                return MarketStatus.Open;
            }
        }

        public enum MarketStatus
        {
            PreMarket,
            Open,
            Closed,
            PostMarket
        }

        public static MarketStatus GetLocalMarketStatus(DateTime dateTime) =>
            GetMarketHours(dateTime).GetMarketStatus(dateTime);

        private static MarketHours GetMarketHours(DateTime dateTime)
        {
            var date = dateTime.Date;

            if (IsWeekend(dateTime))
            {
                return new MarketHours(date);
            }

            var holiday = GetHolidays(dateTime.Year).FirstOrDefault(x => x.Date == date);
            if (holiday == default)
            {
                return MarketHours.CreateNormal(date);
            }

            if (holiday.Hour != 0 && dateTime.Hour < holiday.Hour)
            {
                return MarketHours.CreateLate(holiday);
            }

            return MarketHours.CreateNormal(date);
        }

        private static IEnumerable<DateTime> GetHolidays(int year)
        {
            yield return AdjustForWeekendHoliday(new DateTime(year, 1, 1)); // New Year's Day
            yield return MartinLutherKingDay(year);
            yield return WashingtonDay(year);
            yield return GoodFriday(year);
            yield return MemorialDay(year);
            yield return JuneteenthDay(year);
            var independenceDay = AdjustForWeekendHoliday(new DateTime(year, 7, 4));
            yield return independenceDay;
            if (independenceDay.Day == 4 && independenceDay.DayOfWeek != DayOfWeek.Monday)
            {
                yield return new DateTime(year, 7, 3, 13, 0, 0); // Independence Day Eve
            }
            yield return LaborDay(year);
            var thanksgiving = ThanksgivingDay(year);
            yield return thanksgiving;
            yield return new DateTime(year, 11, thanksgiving.Day + 1, 13, 0, 0); // Black Friday
            var christmasDay = AdjustForWeekendHoliday(new DateTime(year, 12, 25));
            yield return christmasDay;
            if (christmasDay.Day == 25 && christmasDay.DayOfWeek != DayOfWeek.Monday)
            {
                yield return new DateTime(year, 12, 24, 13, 0, 0); // Christmas Eve
            }
            var nextYearNewYearsDate = AdjustForWeekendHoliday(new DateTime(year + 1, 1, 1));
            if (nextYearNewYearsDate.Year == year)
            {
                yield return nextYearNewYearsDate;
            }
        }

        private static bool IsWeekend(DateTime dateTime) =>
            dateTime.DayOfWeek == DayOfWeek.Saturday || dateTime.DayOfWeek == DayOfWeek.Sunday;

        private static DateTime AdjustForWeekendHoliday(DateTime holiday) =>
            holiday.DayOfWeek switch
            {
                DayOfWeek.Saturday => holiday.AddDays(-1),
                DayOfWeek.Sunday => holiday.AddDays(1),
                _ => holiday
            };

        private static DateTime MartinLutherKingDay(int year) =>
            GetNext(DayOfWeek.Monday, new DateTime(year, 1, 1)).AddDays(14);

        private static DateTime WashingtonDay(int year) =>
            GetNext(DayOfWeek.Monday, new DateTime(year, 2, 1)).AddDays(14);

        private static DateTime GoodFriday(int year)
        {
            var g = year % 19;
            var c = year / 100;
            var h = (c - c / 4 - (8 * c + 13) / 25 + 19 * g + 15) % 30;
            var i = h - h / 28 * (1 - h / 28 * (29 / (h + 1)) * ((21 - g) / 11));
            var day = i - (year + year / 4 + i + 2 - c + c / 4) % 7 + 28;
            var month = 3;
            if (day > 31)
            {
                month++;
                day -= 31;
            }
            return new DateTime(year, month, day).AddDays(-2);
        }

        private static DateTime MemorialDay(int year) =>
            GetPrev(DayOfWeek.Monday, new DateTime(year, 5, 31));

        private static DateTime JuneteenthDay(int year) =>
            GetNext(DayOfWeek.Monday, new DateTime(year, 6, 19));

        private static DateTime LaborDay(int year) =>
            GetNext(DayOfWeek.Monday, new DateTime(year, 9, 1));

        private static DateTime ThanksgivingDay(int year) =>
            GetNext(DayOfWeek.Thursday, new DateTime(year, 11, 1)).AddDays(21);

        private static DateTime GetNext(DayOfWeek dayOfWeek, DateTime date) =>
            date.AddDays((dayOfWeek - date.DayOfWeek + 7) % 7);

        private static DateTime GetPrev(DayOfWeek dayOfWeek, DateTime date) =>
            date.AddDays((dayOfWeek - date.DayOfWeek - 7) % 7);
    }

}