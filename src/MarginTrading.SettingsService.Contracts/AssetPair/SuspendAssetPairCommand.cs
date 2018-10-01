using MessagePack;

namespace MarginTrading.SettingsService.Contracts.AssetPair
{
    [MessagePackObject]
    public class SuspendAssetPairCommand
    {
        [Key(0)]
        public string OperationId { get; set; }
        
        [Key(1)]
        public string AssetPairId { get; set; }
    }
}