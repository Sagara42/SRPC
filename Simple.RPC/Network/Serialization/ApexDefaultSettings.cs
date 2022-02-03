using Apex.Serialization;
using Simple.RPC.Network.Message;

namespace Simple.RPC.Network.Serialization
{
    public static class ApexDefaultSettings
    {
        public static Settings DefaultSettings;

        static ApexDefaultSettings()
        {
            DefaultSettings = GetDefaultSettings();
        }

        private static Settings GetDefaultSettings()
        {
            return new Settings
            {
                FlattenClassHierarchy = false,
                UseSerializedVersionId = false
            }
            .MarkSerializable(typeof(IMessage))
            .MarkSerializable(typeof(RpcCallMessage))
            .MarkSerializable(typeof(RpcResponseMessage));
        }
    }
}
