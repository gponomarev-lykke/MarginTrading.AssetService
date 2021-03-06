// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.AssetService.Core;
using MarginTrading.AssetService.Core.Domain;
using MarginTrading.AssetService.Core.Services;
using MarginTrading.AssetService.Core.Settings;
using MarginTrading.AssetService.StorageInterfaces.Repositories;
using Microsoft.Extensions.Internal;

namespace MarginTrading.AssetService.Services
{
    public class MarketDayOffService : IMarketDayOffService
    {
        private readonly IScheduleSettingsRepository _scheduleSettingsRepository;
        private readonly ISystemClock _systemClock;
        private readonly PlatformSettings _platformSettings;

        public MarketDayOffService(
            IScheduleSettingsRepository scheduleSettingsRepository,
            ISystemClock systemClock,
            PlatformSettings platformSettings)
        {
            _scheduleSettingsRepository = scheduleSettingsRepository;
            _systemClock = systemClock;
            _platformSettings = platformSettings;
        }

        public async Task<Dictionary<string, TradingDayInfo>> GetMarketsInfo(string[] marketIds, DateTime? dateTime)
        {
            var scheduleSettings = (await _scheduleSettingsRepository.GetFilteredAsync())
                .Where(x => !string.IsNullOrWhiteSpace(x.MarketId))
                .Cast<ScheduleSettings>()
                .GroupBy(x => x.MarketId)
                .ToDictionary(x => x.Key, x => x.ToList());
            var currentDateTime = dateTime ?? _systemClock.UtcNow.UtcDateTime;

            var rawPlatformSchedule =
                scheduleSettings.TryGetValue(_platformSettings.PlatformMarketId, out var platformSettings)
                    ? platformSettings
                    : new List<ScheduleSettings>();

            var result = marketIds.Except(scheduleSettings.Keys).ToDictionary(
                marketWithoutSchedule => marketWithoutSchedule,
                _ => GetTradingDayInfo(rawPlatformSchedule, currentDateTime));

            foreach (var marketToCompile in marketIds.Except(result.Keys))
            {
                var schedule = scheduleSettings[marketToCompile].Concat(
                    rawPlatformSchedule.WithRank(int.MaxValue)).ToList();

                var tradingDayInfo = GetTradingDayInfo(schedule, currentDateTime);

                result.Add(marketToCompile, tradingDayInfo);
            }

            return result;
        }

        public async Task<TradingDayInfo> GetPlatformInfo(DateTime? dateTime)
        {
            var rawPlatformSchedule = (await _scheduleSettingsRepository.GetFilteredAsync())
                .Where(x => x.MarketId == _platformSettings.PlatformMarketId)
                .Cast<ScheduleSettings>()
                .ToList();
            var currentDateTime = dateTime ?? _systemClock.UtcNow.UtcDateTime;

            return GetTradingDayInfo(rawPlatformSchedule, currentDateTime);
        }

        private static TradingDayInfo GetTradingDayInfo(
            IEnumerable<ScheduleSettings> scheduleSettings, DateTime currentDateTime)
        {
            var compiledSchedule = CompileSchedule(scheduleSettings, currentDateTime);

            var currentInterval = compiledSchedule
                .Where(x => IsBetween(currentDateTime, x.Start, x.End))
                .OrderByDescending(x => x.Schedule.Rank)
                .FirstOrDefault();

            var isEnabled = currentInterval.Enabled();
            var lastTradingDay = GetPreviousTradingDay(compiledSchedule, currentInterval, currentDateTime);
            var nextTradingDay = GetNextTradingDay(compiledSchedule, currentInterval, currentDateTime, lastTradingDay);    

            var result = new TradingDayInfo
            {
                IsTradingEnabled = isEnabled,
                LastTradingDay = lastTradingDay,
                NextTradingDayStart = nextTradingDay
            };

            return result;
        }

        private static DateTime GetPreviousTradingDay(List<CompiledScheduleTimeInterval>
            compiledSchedule, CompiledScheduleTimeInterval currentInterval, DateTime currentDateTime)
        {
            if (currentInterval.Enabled())
                return currentDateTime.Date;
            
            var timestampBeforeCurrentIntervalStart = currentInterval.Start.AddTicks(-1);

            // search for the interval just before the current interval started
            var previousInterval = compiledSchedule
                .Where(x => IsBetween(timestampBeforeCurrentIntervalStart, x.Start, x.End))
                .OrderByDescending(x => x.Schedule.Rank)
                .FirstOrDefault();

            // if trading was enabled, then at that moment was the last trading day
            if (previousInterval.Enabled())
                return timestampBeforeCurrentIntervalStart.Date;

            // if no, there was one more disabled interval and we should go next
            return GetPreviousTradingDay(compiledSchedule, previousInterval, previousInterval.Start);
        }

        private static DateTime GetNextTradingDay(List<CompiledScheduleTimeInterval>
            compiledSchedule, CompiledScheduleTimeInterval currentInterval, DateTime currentDateTime, DateTime lastTradingDay)
        {
            // search for the interval right after the current interval finished
            var ordered = compiledSchedule
                .Where(x => x.End > (currentInterval?.End ?? currentDateTime)
                            || currentInterval != null && x.Schedule.Rank > currentInterval.Schedule.Rank &&
                            x.End > currentInterval.End)
                .OrderBy(x => x.Start)
                .ThenByDescending(x => x.Schedule.Rank)
                .ToList();
            
            var nextInterval = ordered.FirstOrDefault();
            
            if (nextInterval == null)
            {
                if (!currentInterval.Enabled() && currentInterval.End.Date > lastTradingDay.Date)
                {
                    return currentInterval.End;
                }
                else // means no any intervals (current or any in the future)
                {
                    return currentDateTime.Date.AddDays(1); 
                }
            }

            var stateIsChangedToEnabled = nextInterval.Schedule.IsTradeEnabled != currentInterval.Enabled() && nextInterval.Enabled();
            var intervalIsMissing = currentInterval != null && nextInterval.Start > currentInterval.End;

            if (stateIsChangedToEnabled || intervalIsMissing && currentInterval.End.Date > lastTradingDay.Date)
            {
                // ReSharper disable once PossibleNullReferenceException
                // if status was changed and next is enabled, that means current interval is disable == it not null
                return currentInterval.End;
            }

            // if we have long enabled interval with overnight, next day will start at 00:00:00
            if (currentInterval.Enabled() && currentDateTime.Date.AddDays(1) < nextInterval.Start)
            {
                return currentDateTime.Date.AddDays(1);
            }

            return GetNextTradingDay(compiledSchedule, nextInterval, nextInterval.End.AddTicks(1), lastTradingDay);
        }

        private static bool IsBetween(DateTime currentDateTime, DateTime start, DateTime end)
        {
            return start <= currentDateTime && currentDateTime < end;
        }

        private static List<CompiledScheduleTimeInterval> CompileSchedule(
            IEnumerable<ScheduleSettings> scheduleSettings, DateTime currentDateTime)
        {
            var scheduleSettingsByType = scheduleSettings
                .GroupBy(x => x.Start.GetConstraintType())
                .ToDictionary(x => x.Key, value => value);

            //handle weekly
            var weekly = scheduleSettingsByType.TryGetValue(ScheduleConstraintType.Weekly, out var weeklySchedule)
                ? weeklySchedule.SelectMany(sch =>
                {
                    // ReSharper disable PossibleInvalidOperationException - validated previously
                    var currentStart = CurrentWeekday(currentDateTime, sch.Start.DayOfWeek.Value)
                        .Add(sch.Start.Time.Subtract(sch.PendingOrdersCutOff ?? TimeSpan.Zero));
                    var currentEnd = CurrentWeekday(currentDateTime, sch.End.DayOfWeek.Value)
                        .Add(sch.End.Time.Add(sch.PendingOrdersCutOff ?? TimeSpan.Zero));

                    if (currentEnd < currentStart)
                    {
                        currentEnd = currentEnd.AddDays(7);
                    }

                    return new[]
                    {
                        new CompiledScheduleTimeInterval(sch, currentStart.AddDays(-7), currentEnd.AddDays(-7)),
                        new CompiledScheduleTimeInterval(sch, currentStart, currentEnd),
                        new CompiledScheduleTimeInterval(sch, currentStart.AddDays(7), currentEnd.AddDays(7))
                    };
                })
                : new List<CompiledScheduleTimeInterval>();

            //handle single
            var single = scheduleSettingsByType.TryGetValue(ScheduleConstraintType.Single, out var singleSchedule)
                ? singleSchedule.Select(sch => new CompiledScheduleTimeInterval(sch,
                    sch.Start.Date.Value.Add(sch.Start.Time.Subtract(sch.PendingOrdersCutOff ?? TimeSpan.Zero)),
                    sch.End.Date.Value.Add(sch.End.Time.Add(sch.PendingOrdersCutOff ?? TimeSpan.Zero))))
                // ReSharper restore PossibleInvalidOperationException - validated previously
                : new List<CompiledScheduleTimeInterval>();

            //handle daily
            var daily = scheduleSettingsByType.TryGetValue(ScheduleConstraintType.Daily, out var dailySchedule)
                ? dailySchedule.SelectMany(sch =>
                {
                    var start = currentDateTime.Date.Add(sch.Start.Time);
                    var end = currentDateTime.Date.Add(sch.End.Time);
                    if (end < start)
                    {
                        end = end.AddDays(1);
                    }

                    return new[]
                    {
                        new CompiledScheduleTimeInterval(sch, start.AddDays(-1), end.AddDays(-1)),
                        new CompiledScheduleTimeInterval(sch, start, end),
                        new CompiledScheduleTimeInterval(sch, start.AddDays(1), end.AddDays(1))
                    };
                })
                : new List<CompiledScheduleTimeInterval>();

            return weekly.Concat(single).Concat(daily).ToList();
        }

        private static DateTime CurrentWeekday(DateTime start, DayOfWeek day)
        {
            return start.Date.AddDays((int) day - (int) start.DayOfWeek);
        }
    }
}