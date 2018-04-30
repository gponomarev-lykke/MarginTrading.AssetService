﻿using MarginTrading.SettingsService.Core.Domain;
using MarginTrading.SettingsService.Core.Interfaces;

namespace MarginTrading.SettingsService.StorageInterfaces.Repositories
{
    public interface ITradingConditionsRepository : IGenericCrudRepository<ITradingCondition>
    {
        
    }
}
