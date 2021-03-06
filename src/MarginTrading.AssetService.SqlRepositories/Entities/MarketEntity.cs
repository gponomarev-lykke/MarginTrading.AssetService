﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.AssetService.Core.Interfaces;

namespace MarginTrading.AssetService.SqlRepositories.Entities
{
    public class MarketEntity : IMarket
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}