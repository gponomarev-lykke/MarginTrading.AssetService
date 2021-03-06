﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Dapper;
using MarginTrading.AssetService.Core.Domain;
using MarginTrading.AssetService.Core.Interfaces;
using MarginTrading.AssetService.Core.Services;
using MarginTrading.AssetService.SqlRepositories.Entities;
using MarginTrading.AssetService.StorageInterfaces.Repositories;

namespace MarginTrading.AssetService.SqlRepositories.Repositories
{
    public class ScheduleSettingsRepository : IScheduleSettingsRepository
    {
        private const string TableName = "ScheduleSettings";
        private const string CreateTableScript = "CREATE TABLE [{0}](" +
                                                 "[Oid] [bigint] NOT NULL IDENTITY(1,1) PRIMARY KEY," +
                                                 "[Id] [nvarchar] (64) NOT NULL, " +
                                                 "[Rank] [int] NOT NULL, " +
                                                 "[AssetPairRegex] [nvarchar] (MAX) NULL, " +
                                                 "[AssetPairs] [nvarchar] (MAX) NULL, " +
                                                 "[MarketId] [nvarchar] (64) NULL, " +
                                                 "[IsTradeEnabled] [bit] NULL, " +
                                                 "[PendingOrdersCutOff] [nvarchar] (64) NULL, " +
                                                 "[Start] [nvarchar] (MAX) NULL, " +
                                                 "[End] [nvarchar] (MAX) NULL, " +
                                                 "CONSTRAINT {0}_Id UNIQUE(Id), " +
                                                 "INDEX IX_{0}_Market (MarketId)" +
                                                 ");";
        
        private static Type DataType => typeof(IScheduleSettings);
        private static readonly string GetColumns = "[" + string.Join("],[", DataType.GetProperties().Select(x => x.Name)) + "]";
        private static readonly string GetFields = string.Join(",", DataType.GetProperties().Select(x => "@" + x.Name));
        private static readonly string GetUpdateClause = string.Join(",",
            DataType.GetProperties().Select(x => "[" + x.Name + "]=@" + x.Name));

        private readonly IConvertService _convertService;
        private readonly string _connectionString;
        private readonly ILog _log;
        
        public ScheduleSettingsRepository(IConvertService convertService, string connectionString, ILog log)
        {
            _convertService = convertService;
            _log = log;
            _connectionString = connectionString;
            
            using (var conn = new SqlConnection(_connectionString))
            {
                try { conn.CreateTableIfDoesntExists(CreateTableScript, TableName); }
                catch (Exception ex)
                {
                    _log?.WriteErrorAsync(nameof(ScheduleSettingsRepository), "CreateTableIfDoesntExists", null, ex);
                    throw;
                }
            }
        }

        public async Task<IReadOnlyList<IScheduleSettings>> GetFilteredAsync(string marketId = null)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var whereClause = "WHERE 1=1 "
                    + (string.IsNullOrEmpty(marketId) ? "" : " AND MarketId=@marketId");
                var objects = await conn.QueryAsync<ScheduleSettingsEntity>(
                    $"SELECT * FROM {TableName} {whereClause}",
                    new {marketId});
                
                return objects.Select(_convertService.Convert<ScheduleSettingsEntity, ScheduleSettings>).ToList();
            }
        }

        public async Task<IScheduleSettings> GetAsync(string scheduleSettingsId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var objects = await conn.QueryAsync<ScheduleSettingsEntity>(
                    $"SELECT * FROM {TableName} WHERE Id=@id", new {id = scheduleSettingsId});
                
                return objects.Select(_convertService.Convert<ScheduleSettingsEntity, ScheduleSettings>).FirstOrDefault();
            }
        }

        public async Task<bool> TryInsertAsync(IScheduleSettings scheduleSettings)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                try
                {
                    await conn.ExecuteAsync(
                        $"insert into {TableName} ({GetColumns}) values ({GetFields})",
                        _convertService.Convert<IScheduleSettings, ScheduleSettingsEntity>(scheduleSettings));
                }
                catch (Exception ex)
                {
                    _log?.WriteWarningAsync(nameof(AssetPairsRepository), nameof(TryInsertAsync),
                        $"Failed to insert an schedule setting with Id {scheduleSettings.Id}", ex);
                    return false;
                }

                return true;
            }
        }

        public async Task UpdateAsync(IScheduleSettings scheduleSettings)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.ExecuteAsync(
                    $"update {TableName} set {GetUpdateClause} where Id=@Id", 
                    _convertService.Convert<IScheduleSettings, ScheduleSettingsEntity>(scheduleSettings));
            }
        }

        public async Task DeleteAsync(string scheduleSettingsId)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.ExecuteAsync(
                    $"DELETE {TableName} WHERE Id=@Id", new { Id = scheduleSettingsId});
            }
        }
    }
}