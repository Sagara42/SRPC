using Apex.Serialization;
using Simple.RPC.Network.Message;
using System;
using System.IO;

namespace Simple.RPC.Network.Serialization
{
    public class ApexSerializer
    {
        public static ApexSerializer Instance
        {
            get
            {
                if(_instance == null)
                    _instance = new ApexSerializer();
                return _instance;
            }
        }

        private static ApexSerializer _instance;

        private IBinary _serializer;
        private IBinary _deserializer;

        private object _serializerKeeper;
        private object _deserializerKeeper;

        private MemoryStream _serializerStream;
        private MemoryStream _deserializeStream;

        public ApexSerializer()
        {
            var settings = ApexDefaultSettings.DefaultSettings;
            
            _serializer = Binary.Create(settings);
            _deserializer = Binary.Create(settings);
            _serializerKeeper = new object();
            _deserializerKeeper = new object();
            _serializerStream = new MemoryStream();
            _deserializeStream = new MemoryStream();
        }

        private void ClearMemoryStream(MemoryStream stream)
        {
            var buffer = stream.GetBuffer();
            Array.Clear(buffer, 0, buffer.Length);
            stream.Position = 0;
            stream.SetLength(0);
            stream.Capacity = 0;
        }

        public byte[] Serialize<T>(T data) where T : IMessage
        {
            lock(_serializerKeeper)
            {
                try
                {
                    ClearMemoryStream(_serializerStream);

                    _serializer.Write(data, _serializerStream);

                    return _serializerStream.ToArray();
                }
                catch(Exception ex)
                {
                    throw;
                }
            }
        }

        public T Deserialize<T>(byte[] data) where T : IMessage
        {
            lock (_deserializerKeeper)
            {
                ClearMemoryStream(_deserializeStream);

                _deserializeStream.Write(data);
                _deserializeStream.Seek(0, SeekOrigin.Begin);

                return _deserializer.Read<T>(_deserializeStream);
            }
        }
    }
}
