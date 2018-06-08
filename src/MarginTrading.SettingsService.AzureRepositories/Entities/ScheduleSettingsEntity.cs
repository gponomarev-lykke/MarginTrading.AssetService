﻿using System;
using System.Collections.Generic;
using MarginTrading.SettingsService.Core.Domain;
using MarginTrading.SettingsService.Core.Interfaces;
using Newtonsoft.Json;

namespace MarginTrading.SettingsService.AzureRepositories.Entities
{
    public class ScheduleSettingsEntity : SimpleAzureEntity, IScheduleSettings
    {
        internal override string SimplePartitionKey => "ScheduleSettings";
        
        // Id comes from parent type
        public int Rank { get; set; }
        public string AssetPairRegex { get; set; }
        HashSet<string> IScheduleSettings.AssetPairs => JsonConvert.DeserializeObject<HashSet<string>>(AssetPairs); 
        public string AssetPairs { get; set; }
        public string MarketId { get; set; }

        bool? IScheduleSettings.IsTradeEnabled => bool.TryParse(IsTradeEnabled, out var parsed)
            ? parsed : (bool?) null;
        public string IsTradeEnabled { get; set; }
        TimeSpan? IScheduleSettings.PendingOrdersCutOff => TimeSpan.TryParse(PendingOrdersCutOff, out var parsed) 
            ? parsed : (TimeSpan?)null; 
        public string PendingOrdersCutOff { get; set; }
        ScheduleConstraint IScheduleSettings.Start => JsonConvert.DeserializeObject<ScheduleConstraint>(Start); 
        public string Start { get; set; }
        ScheduleConstraint IScheduleSettings.End => JsonConvert.DeserializeObject<ScheduleConstraint>(End); 
        public string End { get; set; }
    }
}