﻿using JetBrains.Annotations;
using MarginTrading.SettingsService.Contracts.Enums;
using MessagePack;

namespace MarginTrading.SettingsService.Contracts.AssetPair
{
    /// <summary>
    /// AssetPair contract. All properties are of reference types, and will be updated only if value != null.
    /// </summary>
    [MessagePackObject]
    public class AssetPairContract
    {
        [Key(0)]
        public string Id { get; set; }
        
        [Key(1)]
        public string Name { get; set; }
        
        [Key(2)]
        public string BaseAssetId { get; set; }
        
        [Key(3)]
        public string QuoteAssetId { get; set; }
        
        [Key(4)]
        public int? Accuracy { get; set; }
        
        [Key(5)]
        public string MarketId { get; set; }
        
        [Key(6)]
        public string LegalEntity { get; set; }
        
        [Key(7)]
        public string BasePairId { get; set; }
        
        [Key(8)]
        public MatchingEngineModeContract? MatchingEngineMode { get; set; }
        
        [Key(9)]
        public decimal? StpMultiplierMarkupBid { get; set; }
        
        [Key(10)]
        public decimal? StpMultiplierMarkupAsk { get; set; }
        
        /// <summary>
        /// Flag will not be changed on API call, only MTCore can change it
        /// </summary>
        [UsedImplicitly]
        [Key(11)]
        public bool? IsSuspended { get; set; }
        
        [Key(12)]
        public bool? IsFrozen { get; set; }
        
        [Key(13)]
        public bool? IsDiscontinued { get; set; }
    }
}
