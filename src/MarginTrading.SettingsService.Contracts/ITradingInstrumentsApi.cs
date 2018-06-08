﻿using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.SettingsService.Contracts.TradingConditions;
using Refit;

namespace MarginTrading.SettingsService.Contracts
{
    [PublicAPI]
    public interface ITradingInstrumentsApi
    {
        [Get("/api/tradingInstruments")]
        Task<List<TradingInstrumentContract>> List([Query, CanBeNull] string tradingConditionId);


        [Post("/api/tradingInstruments")]
        Task<TradingInstrumentContract> Insert([Body] TradingInstrumentContract instrument);


        /// <summary>
        /// Assign trading instrument to a trading condition with default values
        /// </summary>
        /// <param name="tradingConditionId"></param>
        /// <param name="instruments"></param>
        /// <returns></returns>
        [Post("/api/tradingInstruments/{tradingConditionId}")]
        Task<List<TradingInstrumentContract>> AssignCollection(
            [NotNull] string tradingConditionId,
            [Body] string[] instruments);
        

        [ItemCanBeNull]
        [Get("/api/tradingInstruments/{tradingConditionId}/{assetPairId}")]
        Task<TradingInstrumentContract> Get(
            [NotNull] string tradingConditionId,
            [NotNull] string assetPairId);


        [Put("/api/tradingInstruments/{tradingConditionId}/{assetPairId}")]
        Task<TradingInstrumentContract> Update(
            [NotNull] string tradingConditionId,
            [NotNull] string assetPairId,
            [Body] TradingInstrumentContract instrument);


        [Delete("/api/tradingInstruments/{tradingConditionId}/{assetPairId}")]
        Task Delete(
            [NotNull] string tradingConditionId,
            [NotNull] string assetPairId);

    }
}