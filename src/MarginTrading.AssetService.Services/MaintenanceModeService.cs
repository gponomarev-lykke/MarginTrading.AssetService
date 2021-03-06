﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using MarginTrading.AssetService.Core.Services;

namespace MarginTrading.AssetService.Services
{
    public class MaintenanceModeService : IMaintenanceModeService
    {
        private static bool IsEnabled { get; set; }

        public bool CheckIsEnabled()
        {
            return IsEnabled;
        }

        public void SetMode(bool isEnabled)
        {
            IsEnabled = isEnabled;
        }
    }
}