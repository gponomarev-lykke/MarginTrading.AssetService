using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.SettingsService.Contracts;
using MarginTrading.SettingsService.Contracts.TradingConditions;
using MarginTrading.SettingsService.Core;
using MarginTrading.SettingsService.Core.Domain;
using MarginTrading.SettingsService.Core.Interfaces;
using MarginTrading.SettingsService.Core.Services;
using MarginTrading.SettingsService.Core.Settings;
using MarginTrading.SettingsService.Extensions;
using MarginTrading.SettingsService.StorageInterfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.IISIntegration;

namespace MarginTrading.SettingsService.Controllers
{
    /// <summary>
    /// Trading conditions management
    /// </summary>
    [Route("api/tradingConditions")]
    public class TradingConditionsController : Controller, ITradingConditionsApi
    {
        private readonly IAssetsRepository _assetsRepository;
        private readonly ITradingConditionsRepository _tradingConditionsRepository;
        private readonly IConvertService _convertService;
        private readonly IEventSender _eventSender;
        private readonly DefaultLegalEntitySettings _defaultLegalEntitySettings;
        
        public TradingConditionsController(
            IAssetsRepository assetsRepository,
            ITradingConditionsRepository tradingConditionsRepository,
            IConvertService convertService,
            IEventSender eventSender,
            DefaultLegalEntitySettings defaultLegalEntitySettings)
        {
            _assetsRepository = assetsRepository;
            _tradingConditionsRepository = tradingConditionsRepository;
            _convertService = convertService;
            _eventSender = eventSender;
            _defaultLegalEntitySettings = defaultLegalEntitySettings;
        }
        
        /// <summary>
        /// Get the list of trading conditions
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("")]
        public async Task<List<TradingConditionContract>> List([FromQuery] bool? isDefault = null)
        {
            var data = await _tradingConditionsRepository.GetAsync();
            
            return data
                .Where(x => isDefault == null || x.IsDefault == isDefault)
                .Select(x => _convertService.Convert<ITradingCondition, TradingConditionContract>(x)).ToList();
        }

        /// <summary>
        /// Create new trading condition
        /// </summary>
        /// <param name="tradingCondition"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("")]
        public async Task<TradingConditionContract> Insert([FromBody] TradingConditionContract tradingCondition)
        {
            await ValidateTradingCondition(tradingCondition);
            
            var defaultTradingCondition =
                (await _tradingConditionsRepository.GetAsync(x => x.IsDefault)).FirstOrDefault();

            if (tradingCondition.IsDefault 
                && defaultTradingCondition != null && defaultTradingCondition.Id != tradingCondition.Id)
            {
                await SetDefault(defaultTradingCondition, false);
            }

            if (defaultTradingCondition == null)
            {
                tradingCondition.IsDefault = true;
            }
            
            _defaultLegalEntitySettings.Set(tradingCondition);
                
            if (!await _tradingConditionsRepository.TryInsertAsync(
                _convertService.Convert<TradingConditionContract, TradingCondition>(tradingCondition)))
            {
                throw new ArgumentException($"Trading condition with id {tradingCondition.Id} already exists",
                    nameof(tradingCondition.Id));
            }

            await _eventSender.SendSettingsChangedEvent($"{Request.Path}", SettingsChangedSourceType.TradingCondition);

            return tradingCondition;
        }

        /// <summary>
        /// Get the trading condition
        /// </summary>
        /// <param name="tradingConditionId"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("{tradingConditionId}")]
        public async Task<TradingConditionContract> Get(string tradingConditionId)
        {
            var obj = await _tradingConditionsRepository.GetAsync(tradingConditionId);
            
            return _convertService.Convert<ITradingCondition, TradingConditionContract>(obj);
        }

        /// <summary>
        /// Update the trading condition
        /// </summary>
        /// <param name="tradingConditionId"></param>
        /// <param name="tradingCondition"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("{tradingConditionId}")]
        public async Task<TradingConditionContract> Update(string tradingConditionId, 
            [FromBody] TradingConditionContract tradingCondition)
        {
            ValidateId(tradingConditionId, tradingCondition);
            
            await ValidateTradingCondition(tradingCondition);
            
            var defaultTradingCondition =
                (await _tradingConditionsRepository.GetAsync(x => x.IsDefault)).FirstOrDefault();
            if (defaultTradingCondition == null && !tradingCondition.IsDefault)
            {
                tradingCondition.IsDefault = true;
            }

            if (defaultTradingCondition != null 
                && tradingCondition.IsDefault && defaultTradingCondition.Id != tradingCondition.Id)
            {
                await SetDefault(defaultTradingCondition, false);
            }
            
            _defaultLegalEntitySettings.Set(tradingCondition);

            var existingCondition = await _tradingConditionsRepository.GetAsync(tradingCondition.Id);
            if (existingCondition == null)
            {
                throw new Exception($"Trading condition with Id = {tradingCondition.Id} not found");
            }

            if (existingCondition.LegalEntity != tradingCondition.LegalEntity)
            {
                throw new Exception("LegalEntity cannot be changed");
            }

            await _tradingConditionsRepository.ReplaceAsync(
                _convertService.Convert<TradingConditionContract, TradingCondition>(tradingCondition));

            await _eventSender.SendSettingsChangedEvent($"{Request.Path}", SettingsChangedSourceType.TradingCondition);
            
            return tradingCondition;
        }

        private async Task SetDefault(ITradingCondition obj, bool state)
        {
            var defaultTrConDomain =
                _convertService.Convert<ITradingCondition, TradingCondition>(obj);
            defaultTrConDomain.IsDefault = state;
            await _tradingConditionsRepository.ReplaceAsync(defaultTrConDomain);
        }

        private void ValidateId(string id, TradingConditionContract contract)
        {
            if (contract?.Id != id)
            {
                throw new ArgumentException("Id must match with contract id");
            }
        }

        private async Task ValidateTradingCondition(TradingConditionContract tradingCondition)
        {
            if (string.IsNullOrWhiteSpace(tradingCondition?.Id))
            {
                throw new ArgumentNullException(nameof(tradingCondition.Id), "TradingCondition Id must be set");
            }

            if (!string.IsNullOrEmpty(tradingCondition.LimitCurrency)
                && await _assetsRepository.GetAsync(tradingCondition.LimitCurrency) == null)
            {
                throw new InvalidOperationException($"LimitCurrency asset {tradingCondition.LimitCurrency} does not exist");
            }

            foreach (var baseAsset in tradingCondition.BaseAssets)
            {//TODO optimization may be applied here
                if (await _assetsRepository.GetAsync(baseAsset) == null)
                {
                    throw new InvalidOperationException($"Base asset {baseAsset} does not exist");
                }
            }
        }
    }
}