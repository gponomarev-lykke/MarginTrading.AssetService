﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.AssetService.Contracts;
using MarginTrading.AssetService.Contracts.Market;
using MarginTrading.AssetService.Core.Domain;
using MarginTrading.AssetService.Core.Interfaces;
using MarginTrading.AssetService.Core.Services;
using MarginTrading.AssetService.Middleware;
using MarginTrading.AssetService.StorageInterfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.AssetService.Controllers
{
    /// <summary>
    /// Markets management
    /// </summary>
    [Authorize]
    [Route("api/markets")]
    [MiddlewareFilter(typeof(RequestLoggingPipeline))]
    public class MarketsController : Controller, IMarketsApi
    {
        private readonly IMarketRepository _marketRepository;
        private readonly IConvertService _convertService;
        private readonly IEventSender _eventSender;
        
        public MarketsController(
            IMarketRepository marketRepository,
            IConvertService convertService,
            IEventSender eventSender)
        {
            _marketRepository = marketRepository;
            _convertService = convertService;
            _eventSender = eventSender;
        }
        
        /// <summary>
        /// Get the list of Markets
        /// </summary>
        [HttpGet]
        [Route("")]
        public async Task<List<MarketContract>> List()
        {
            var data = await _marketRepository.GetAsync();
            
            return data.Select(x => _convertService.Convert<IMarket, MarketContract>(x)).ToList();
        }
        
        /// <summary>
        /// Create new market
        /// </summary>
        [HttpPost]
        [Route("")]
        public async Task<MarketContract> Insert([FromBody] MarketContract market)
        {
            Validate(market);

            if (!await _marketRepository.TryInsertAsync(_convertService.Convert<MarketContract, Market>(market)))
            {
                throw new ArgumentException($"Market with id {market.Id} already exists", nameof(market.Id));
            }

            await _eventSender.SendSettingsChangedEvent($"{Request.Path}", SettingsChangedSourceType.Market,
                market.Id);

            return market;
        }

        /// <summary>
        /// Get the market
        /// </summary>
        [HttpGet]
        [Route("{marketId}")]
        public async Task<MarketContract> Get(string marketId)
        {
            var obj = await _marketRepository.GetAsync(marketId);
            
            return _convertService.Convert<IMarket, MarketContract>(obj);
        }

        /// <summary>
        /// Update the market
        /// </summary>
        [HttpPut]
        [Route("{marketId}")]
        public async Task<MarketContract> Update(string marketId, [FromBody] MarketContract market)
        {
            Validate(market);
            ValidateId(marketId, market);

            await _marketRepository.UpdateAsync(_convertService.Convert<MarketContract, Market>(market));

            await _eventSender.SendSettingsChangedEvent($"{Request.Path}", SettingsChangedSourceType.Market,
                marketId);
            
            return market;
        }

        /// <summary>
        /// Delete the market
        /// </summary>
        [HttpDelete]
        [Route("{marketId}")]
        public async Task Delete(string marketId)
        {
            await _marketRepository.DeleteAsync(marketId);

            await _eventSender.SendSettingsChangedEvent($"{Request.Path}", SettingsChangedSourceType.Market,
                marketId);
        }

        private void ValidateId(string id, MarketContract contract)
        {
            if (contract?.Id != id)
            {
                throw new ArgumentException("Id must match with contract id");
            }
        }

        private void Validate(MarketContract newValue)
        {
            if (newValue == null)
            {
                throw new ArgumentNullException("market", "Model is incorrect");
            }
            
            if (string.IsNullOrWhiteSpace(newValue.Id))
            {
                throw new ArgumentNullException(nameof(newValue.Id), "market Id must be set");
            }
        }
    }
}