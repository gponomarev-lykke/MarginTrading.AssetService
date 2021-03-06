﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using MarginTrading.AssetService.Core.Interfaces;
using Newtonsoft.Json;

namespace MarginTrading.AssetService.AzureRepositories.Entities
{
    public class TradingConditionEntity : SimpleAzureEntity, ITradingCondition
    {
        public static readonly string Pk = "TradingConditions"; 
        internal override string SimplePartitionKey => Pk;
        
        // Id comes from parent type
        public string Name { get; set; }
        public string LegalEntity { get; set; }
        public decimal MarginCall1 { get; set; }
        public decimal MarginCall2 { get; set; }
        public decimal StopOut { get; set; }
        public decimal DepositLimit { get; set; }
        public decimal WithdrawalLimit { get; set; }
        public string LimitCurrency { get; set; }
        List<string> ITradingCondition.BaseAssets => JsonConvert.DeserializeObject<List<string>>(BaseAssets); 
        public string BaseAssets { get; set; }
        public bool IsDefault { get; set; }
    }
}