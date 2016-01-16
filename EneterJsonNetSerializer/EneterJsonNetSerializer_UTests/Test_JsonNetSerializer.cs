using Eneter.JsonNet;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.EndPoints.Rpc;
using Eneter.Messaging.EndPoints.TypedMessages;
using Eneter.Messaging.MessagingSystems.Composites.MessageBus;
using Eneter.Messaging.MessagingSystems.Composites.MonitoredMessagingComposit;
using Eneter.Messaging.Nodes.Broker;
using Eneter.Messaging.Nodes.ChannelWrapper;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace EneterJsonNetSerializerr_UTests
{
    [TestFixture]
    public class Test_JsonNetSerializer
    {
        [Serializable]
        public class TestMessage
        {
            public string Name;
            public int Value;
        }

        [Test]
        public void SerializeDeserializeTestMessage()
        {
            TestMessage aTestMessage = new TestMessage();
            aTestMessage.Name = "Hello";
            aTestMessage.Value = 123;

            JsonNetSerializer aJsonNetSerializer = new JsonNetSerializer();
            object aSerializedData = aJsonNetSerializer.Serialize<TestMessage>(aTestMessage);
            TestMessage aResult = aJsonNetSerializer.Deserialize<TestMessage>(aSerializedData);

            Assert.AreEqual(aTestMessage.Name, aResult.Name);
            Assert.AreEqual(aTestMessage.Value, aResult.Value);
        }

        [Test]
        public void SerializeDeserializePerformanceTest()
        {
            TestMessage aTestMessage = new TestMessage();
            aTestMessage.Name = "Hello";
            aTestMessage.Value = 123;
            JsonNetSerializer aJsonNetSerializer = new JsonNetSerializer();
            SerializerPerformanceTest<TestMessage>(aJsonNetSerializer, aTestMessage);

            TestMessage aTestMessage2 = new TestMessage();
            aTestMessage2.Name = "Hello";
            aTestMessage2.Value = 123;
            BinarySerializer aNetBinSerializer = new BinarySerializer();
            SerializerPerformanceTest<TestMessage>(aNetBinSerializer, aTestMessage2);

            XmlStringSerializer anXmlSerializer = new XmlStringSerializer();
            SerializerPerformanceTest<TestMessage>(anXmlSerializer, aTestMessage2);

            DataContractJsonStringSerializer aJsonSerializer = new DataContractJsonStringSerializer();
            SerializerPerformanceTest<TestMessage>(aJsonSerializer, aTestMessage2);
        }

        [Test]
        public void ThreadSafetyTest()
        {
            TestMessage aTestMessage = new TestMessage();
            aTestMessage.Name = "Hello";
            aTestMessage.Value = 123;

            JsonNetSerializer aJsonNetSerializer = new JsonNetSerializer();

            AutoResetEvent anAllThreadDone = new AutoResetEvent(false);
            int aCount = 0;

            Stopwatch aStopWatch = new Stopwatch();
            aStopWatch.Start();

            for (int i = 0; i < 10; ++i)
            {
                ThreadPool.QueueUserWorkItem(x =>
                {
                    SerializerPerformanceTest<TestMessage>(aJsonNetSerializer, aTestMessage);
                    ++aCount;

                    if (aCount == 10)
                    {
                        anAllThreadDone.Set();
                    }
                });
            }

            anAllThreadDone.WaitOne();

            aStopWatch.Stop();
            Console.WriteLine("Elapsed time: " + aStopWatch.Elapsed);
        }

        [Test]
        public void SerializeDeserializeMultiTypedMessage()
        {
            JsonNetSerializer aJsonNetSerializer = new JsonNetSerializer();

            MultiTypedMessage aSrc = new MultiTypedMessage();
            aSrc.TypeName = "String";
            aSrc.MessageData = "Hello";

            object aSerializedData = aJsonNetSerializer.Serialize<MultiTypedMessage>(aSrc);

            MultiTypedMessage aResult = aJsonNetSerializer.Deserialize<MultiTypedMessage>(aSerializedData);
            Assert.AreEqual(aSrc.TypeName, aResult.TypeName);
            Assert.AreEqual(aSrc.MessageData, aResult.MessageData);
        }

        [Test]
        public void SerializeDeserializeWithRSA()
        {
            JsonNetSerializer anUnderlyingSerializer = new JsonNetSerializer();

            // Generate public and private keys.
            RSACryptoServiceProvider aCryptoProvider = new RSACryptoServiceProvider();
            RSAParameters aPublicKey = aCryptoProvider.ExportParameters(false);
            RSAParameters aPrivateKey = aCryptoProvider.ExportParameters(true);

            RsaSerializer aSerializer = new RsaSerializer(aPublicKey, aPrivateKey, 128, anUnderlyingSerializer);


            MultiTypedMessage aSrc = new MultiTypedMessage();
            aSrc.TypeName = "String";
            aSrc.MessageData = "Hello";

            object aSerializedData = aSerializer.Serialize<MultiTypedMessage>(aSrc);

            MultiTypedMessage aResult = aSerializer.Deserialize<MultiTypedMessage>(aSerializedData);
            Assert.AreEqual(aSrc.TypeName, aResult.TypeName);
            Assert.AreEqual(aSrc.MessageData, aResult.MessageData);
        }

        [Test]
        public void SerializeDeserializeMessageBusMessage()
        {
            JsonNetSerializer aJsonNetSerializer = new JsonNetSerializer();

            MessageBusMessage aSrc = new MessageBusMessage();
            aSrc.Request = EMessageBusRequest.DisconnectClient;
            aSrc.Id = "1234";
            aSrc.MessageData = "Hello";
            object aSerializedData = aJsonNetSerializer.Serialize<MessageBusMessage>(aSrc);
            MessageBusMessage aResult = aJsonNetSerializer.Deserialize<MessageBusMessage>(aSerializedData);
            Assert.AreEqual(aSrc.Request, aResult.Request);
            Assert.AreEqual(aSrc.Id, aResult.Id);
            Assert.AreEqual(aSrc.MessageData, aResult.MessageData);

            aSrc.Request = EMessageBusRequest.SendResponseMessage;
            aSrc.Id = "";
            aSrc.MessageData = null;
            aSerializedData = aJsonNetSerializer.Serialize<MessageBusMessage>(aSrc);
            aResult = aJsonNetSerializer.Deserialize<MessageBusMessage>(aSerializedData);
            Assert.AreEqual(aSrc.Request, aResult.Request);
            Assert.AreEqual(aSrc.Id, aResult.Id);
            Assert.IsNull(aResult.MessageData);
        }

        [Test]
        public void SerializeDeserializeWrappedData()
        {
            JsonNetSerializer aJsonNetSerializer = new JsonNetSerializer();

            WrappedData aSrc = new WrappedData();
            aSrc.AddedData = "Hello";
            aSrc.OriginalData = "Hello2";
            object aSerializedData = aJsonNetSerializer.Serialize<WrappedData>(aSrc);
            WrappedData aResult = aJsonNetSerializer.Deserialize<WrappedData>(aSerializedData);
            Assert.AreEqual(aResult.AddedData, aSrc.AddedData);
            Assert.AreEqual(aResult.OriginalData, aSrc.OriginalData);


            aSrc.OriginalData = null;
            aSerializedData = aJsonNetSerializer.Serialize<WrappedData>(aSrc);
            aResult = aJsonNetSerializer.Deserialize<WrappedData>(aSerializedData);
            Assert.AreEqual(aResult.AddedData, aSrc.AddedData);
            Assert.IsNull(aResult.OriginalData);
        }

        [Test]
        public void SerializeDeserializeBrokerMessage()
        {
            JsonNetSerializer aJsonNetSerializer = new JsonNetSerializer();

            BrokerMessage aSrc = new BrokerMessage();
            aSrc.Request = EBrokerRequest.Subscribe;
            aSrc.MessageTypes = new String[] { "Hello1", "Hello2" };
            aSrc.Message = null;
            object aSerializedData = aJsonNetSerializer.Serialize<BrokerMessage>(aSrc);
            BrokerMessage aResult = aJsonNetSerializer.Deserialize<BrokerMessage>(aSerializedData);
            Assert.AreEqual(aResult.Request, aSrc.Request);
            Assert.IsTrue(Enumerable.SequenceEqual(aResult.MessageTypes, aSrc.MessageTypes));
            Assert.IsNull(aResult.Message);


            aSrc.Message = "Hello";
            aSerializedData = aJsonNetSerializer.Serialize<BrokerMessage>(aSrc);
            aResult = aJsonNetSerializer.Deserialize<BrokerMessage>(aSerializedData);
            Assert.AreEqual(aResult.Request, aSrc.Request);
            Assert.IsTrue(Enumerable.SequenceEqual(aResult.MessageTypes, aSrc.MessageTypes));
            Assert.AreEqual(aResult.Message, aSrc.Message);
        }

        [Test]
        public void SerializeDeserializeMonitorChannelMessage()
        {
            JsonNetSerializer aJsonNetSerializer = new JsonNetSerializer();

            MonitorChannelMessage aSrc = new MonitorChannelMessage();
            aSrc.MessageType = MonitorChannelMessageType.Message;
            aSrc.MessageContent = null;
            object aSerializedData = aJsonNetSerializer.Serialize<MonitorChannelMessage>(aSrc);
            MonitorChannelMessage aResult = aJsonNetSerializer.Deserialize<MonitorChannelMessage>(aSerializedData);
            Assert.AreEqual(aResult.MessageType, aSrc.MessageType);
            Assert.IsNull(aResult.MessageContent);


            aSrc.MessageContent = "Hello2";
            aSerializedData = aJsonNetSerializer.Serialize<MonitorChannelMessage>(aSrc);
            aResult = aJsonNetSerializer.Deserialize<MonitorChannelMessage>(aSerializedData);
            Assert.AreEqual(aResult.MessageType, aSrc.MessageType);
            Assert.AreEqual(aResult.MessageContent, aSrc.MessageContent);
        }

        [Test]
        public void serializeDeserializeVoidMessage()
        {
            JsonNetSerializer aJsonNetSerializer = new JsonNetSerializer();

            VoidMessage aSrc = new VoidMessage();
            object aSerializedData = aJsonNetSerializer.Serialize<VoidMessage>(aSrc);
            VoidMessage aResult = aJsonNetSerializer.Deserialize<VoidMessage>(aSerializedData);
            Assert.IsNotNull(aResult);
        }

        [Test]
        public void SerializeDeserializeEventArgs()
        {
            JsonNetSerializer aJsonNetSerializer = new JsonNetSerializer();

            object a = aJsonNetSerializer.Serialize<int>(0);

            EventArgs aSrc = new EventArgs();
            object aSerializedData = aJsonNetSerializer.Serialize<EventArgs>(aSrc);
            EventArgs aResult = aJsonNetSerializer.Deserialize<EventArgs>(aSerializedData);
            Assert.IsNotNull(aResult);
        }

        [Test]
        public void SerializeDeserializeRpcMessage()
        {
            JsonNetSerializer aJsonNetSerializer = new JsonNetSerializer();
            int[] aaa = new int[] { 10, 20 };
            object s = aJsonNetSerializer.Serialize<int[]>(aaa);
            int[] d = aJsonNetSerializer.Deserialize<int[]>(s);


            string aParam1 = aJsonNetSerializer.Serialize<string>("hello") as string;
            string aParam2 = aJsonNetSerializer.Serialize<bool>(true) as string;
            string aParam3 = aJsonNetSerializer.Serialize<bool>(false) as string;
            string aParam4 = aJsonNetSerializer.Serialize<byte>(255) as string;
            string aParam5 = aJsonNetSerializer.Serialize<char>('ž') as string;
            string aParam6 = aJsonNetSerializer.Serialize<short>(-1) as string;
            string aParam7 = aJsonNetSerializer.Serialize<int>(100) as string;
            string aParam8 = aJsonNetSerializer.Serialize<long>((long)-1236987) as string;
            string aParam9 = aJsonNetSerializer.Serialize<float>(1.2345f) as string;
            string aParam10 = aJsonNetSerializer.Serialize<double>(13.2345) as string;

            string[] st = { "hello 1", "hello 2" };
            bool[] bo = { true, false };
            byte[] by = { 123, 1 };
            char[] ch = { 'ž', 'A' };
            short[] sh = { -1, 6000 };
            int[] i = { -1, 60000 };
            long[] lo = { -1, 60000000 };
            float[] fl = { -1.0f, 1.234f };
            double[] dou = { -1.0, 100.4353 };

            string aParam11 = aJsonNetSerializer.Serialize<string[]>(st) as string;
            string aParam12 = aJsonNetSerializer.Serialize<bool[]>(bo) as string;
            string aParam13 = aJsonNetSerializer.Serialize<byte[]>(by) as string;
            string aParam14 = aJsonNetSerializer.Serialize<char[]>(ch) as string;
            string aParam15 = aJsonNetSerializer.Serialize<short[]>(sh) as string;
            string aParam16 = aJsonNetSerializer.Serialize<int[]>(i) as string;
            string aParam17 = aJsonNetSerializer.Serialize<long[]>(lo) as string;
            string aParam18 = aJsonNetSerializer.Serialize<float[]>(fl) as string;
            string aParam19 = aJsonNetSerializer.Serialize<double[]>(dou) as string;

            RpcMessage anRpcMessage = new RpcMessage();
            anRpcMessage.Id = 102;
            anRpcMessage.Request = ERpcRequest.InvokeMethod;
            anRpcMessage.OperationName = "DummyOperation";
            anRpcMessage.SerializedReturn = aJsonNetSerializer.Serialize<string>("DummyReturn") as string;
            anRpcMessage.ErrorType = "DummyErrorType";
            anRpcMessage.ErrorMessage = "DummyError";
            anRpcMessage.ErrorDetails = "DummyDetails";

            anRpcMessage.SerializedParams = new object[]
                { aParam1, aParam2, aParam3, aParam4, aParam5, aParam6, aParam7, aParam8, aParam9, aParam10,
                  aParam11, aParam12, aParam13, aParam14, aParam15, aParam16, aParam17, aParam18, aParam19};

            object aSerialized = aJsonNetSerializer.Serialize<RpcMessage>(anRpcMessage);

            RpcMessage aDeserialized = aJsonNetSerializer.Deserialize<RpcMessage>(aSerialized);

            Assert.AreEqual(anRpcMessage.Id, aDeserialized.Id);
            Assert.AreEqual(anRpcMessage.Request, aDeserialized.Request);
            Assert.AreEqual(anRpcMessage.OperationName, aDeserialized.OperationName);
            Assert.AreEqual(anRpcMessage.ErrorType, aDeserialized.ErrorType);
            Assert.AreEqual(anRpcMessage.ErrorMessage, aDeserialized.ErrorMessage);
            Assert.AreEqual(anRpcMessage.ErrorDetails, aDeserialized.ErrorDetails);

            Assert.AreEqual(19, aDeserialized.SerializedParams.Length);

            string aD1 = aJsonNetSerializer.Deserialize<string>(aDeserialized.SerializedParams[0]);
            bool aD2 = aJsonNetSerializer.Deserialize<bool>(aDeserialized.SerializedParams[1]);
            bool aD3 = aJsonNetSerializer.Deserialize<bool>(aDeserialized.SerializedParams[2]);
            byte aD4 = aJsonNetSerializer.Deserialize<byte>(aDeserialized.SerializedParams[3]);
            char aD5 = aJsonNetSerializer.Deserialize<char>(aDeserialized.SerializedParams[4]);
            short aD6 = aJsonNetSerializer.Deserialize<short>(aDeserialized.SerializedParams[5]);
            int aD7 = aJsonNetSerializer.Deserialize<int>(aDeserialized.SerializedParams[6]);
            long aD8 = aJsonNetSerializer.Deserialize<long>(aDeserialized.SerializedParams[7]);
            float aD9 = aJsonNetSerializer.Deserialize<float>(aDeserialized.SerializedParams[8]);
            double aD10 = aJsonNetSerializer.Deserialize<double>(aDeserialized.SerializedParams[9]);

            string[] aD11 = aJsonNetSerializer.Deserialize<string[]>(aDeserialized.SerializedParams[10]);
            bool[] aD12 = aJsonNetSerializer.Deserialize<bool[]>(aDeserialized.SerializedParams[11]);
            byte[] aD13 = aJsonNetSerializer.Deserialize<byte[]>(aDeserialized.SerializedParams[12]);
            char[] aD14 = aJsonNetSerializer.Deserialize<char[]>(aDeserialized.SerializedParams[13]);
            short[] aD15 = aJsonNetSerializer.Deserialize<short[]>(aDeserialized.SerializedParams[14]);
            int[] aD16 = aJsonNetSerializer.Deserialize<int[]>(aDeserialized.SerializedParams[15]);
            long[] aD17 = aJsonNetSerializer.Deserialize<long[]>(aDeserialized.SerializedParams[16]);
            float[] aD18 = aJsonNetSerializer.Deserialize<float[]>(aDeserialized.SerializedParams[17]);
            double[] aD19 = aJsonNetSerializer.Deserialize<double[]>(aDeserialized.SerializedParams[18]);

            Assert.AreEqual("hello", aD1);
            Assert.AreEqual(true, aD2);
            Assert.AreEqual(false, aD3);
            Assert.AreEqual((byte)255, aD4);
            Assert.AreEqual('ž', aD5);
            Assert.AreEqual(-1, aD6);
            Assert.AreEqual(100, aD7);
            Assert.AreEqual((long)-1236987, aD8);
            Assert.True(Math.Abs((float)1.2345 - aD9) < 0.00001);
            Assert.True(Math.Abs((double)13.2345 - aD10) < 0.00001);


            Assert.True(st.SequenceEqual(aD11));

            Assert.True(bo.SequenceEqual(aD12));
            Assert.True(by.SequenceEqual(aD13));
            Assert.True(ch.SequenceEqual(aD14));
            Assert.True(sh.SequenceEqual(aD15));
            Assert.True(i.SequenceEqual(aD16));
            Assert.True(lo.SequenceEqual(aD17));
            Assert.True(fl.SequenceEqual(aD18));
            Assert.True(dou.SequenceEqual(aD19));

            object aDeserializedReturn = aJsonNetSerializer.Deserialize<string>(aDeserialized.SerializedReturn);
            Assert.AreEqual("DummyReturn", aDeserializedReturn);
        }

        private void SerializerPerformanceTest<T>(ISerializer serializer, T dataToSerialize)
        {
            int aMessageSize = 0;

            Stopwatch aStopWatch = new Stopwatch();
            aStopWatch.Start();

            for (int i = 0; i < 100000; ++i)
            {
                object aSerializedData = serializer.Serialize<T>(dataToSerialize);
                if (aSerializedData is byte[])
                {
                    aMessageSize = ((byte[])aSerializedData).Length;
                }
                else if (aSerializedData is string)
                {
                    aMessageSize = ((string)aSerializedData).Length;
                }
                T aResult2 = serializer.Deserialize<T>(aSerializedData);
            }

            aStopWatch.Stop();
            Console.WriteLine(serializer.GetType().Name + ": " + aStopWatch.Elapsed + " Message Size: " + aMessageSize);
        }

    }
}
