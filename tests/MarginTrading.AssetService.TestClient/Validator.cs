﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.AssetService.TestClient
{
    public class Validator
    {
        public bool Equals<T>(T contract, T model)
        {
            foreach (var property in typeof(T).GetProperties())
            {
                if (property.PropertyType.IsValueType 
                    && !property.GetValue(contract, null).Equals(property.GetValue(model, null)))
                {
                    return false;
                }
                //TODO impl comparers
            }

            return true;
        }
    }
}