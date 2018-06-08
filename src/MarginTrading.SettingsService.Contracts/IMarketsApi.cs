﻿using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.SettingsService.Contracts.Market;
using Refit;

namespace MarginTrading.SettingsService.Contracts
{
    [PublicAPI]
    public interface IMarketsApi
    {
        [Get("/api/markets")]
        Task<List<MarketContract>> List();


        [Post("/api/markets")]
        Task<MarketContract> Insert([Body] MarketContract market);

        
        [ItemCanBeNull]
        [Get("/api/markets/{marketId}")]
        Task<MarketContract> Get([NotNull] string marketId);


        [Put("/api/markets/{marketId}")]
        Task<MarketContract> Update([NotNull] string marketId, [Body] MarketContract market);


        [Delete("/api/markets/{marketId}")]
        Task Delete([NotNull] string marketId);

    }
}