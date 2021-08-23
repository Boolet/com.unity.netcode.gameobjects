﻿using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Multiplayer.Netcode;
using Unity.Netcode.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = System.Random;

namespace Unity.Netcode.EditorTests
{
    public class BufferSerializerTests
    {
        [Test]
        public void TestIsReaderIsWriter()
        {
            FastBufferWriter writer = new FastBufferWriter(4, Allocator.Temp);
            using (writer)
            {
                BufferSerializer<BufferSerializerWriter> serializer =
                    new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(ref writer));
                Assert.IsFalse(serializer.IsReader);
                Assert.IsTrue(serializer.IsWriter);
            }
            byte[] readBuffer = new byte[4];
            FastBufferReader reader = new FastBufferReader(readBuffer, Allocator.Temp);
            using (reader)
            {
                BufferSerializer<BufferSerializerReader> serializer =
                    new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(ref reader));
                Assert.IsTrue(serializer.IsReader);
                Assert.IsFalse(serializer.IsWriter);
            }
        }
        [Test]
        public unsafe void TestGetUnderlyingStructs()
        {
            FastBufferWriter writer = new FastBufferWriter(4, Allocator.Temp);
            using (writer)
            {
                BufferSerializer<BufferSerializerWriter> serializer =
                    new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(ref writer));
                ref FastBufferWriter underlyingWriter = ref serializer.GetFastBufferWriter();
                fixed (FastBufferWriter* ptr = &underlyingWriter)
                {
                    Assert.IsTrue(ptr == &writer);
                }
                // Can't use Assert.Throws() because ref structs can't be passed into lambdas.
                try
                {
                    serializer.GetFastBufferReader();
                }
                catch (InvalidOperationException)
                {
                    // pass
                }

            }
            byte[] readBuffer = new byte[4];
            FastBufferReader reader = new FastBufferReader(readBuffer, Allocator.Temp);
            using (reader)
            {
                BufferSerializer<BufferSerializerReader> serializer =
                    new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(ref reader));
                ref FastBufferReader underlyingReader = ref serializer.GetFastBufferReader();
                fixed (FastBufferReader* ptr = &underlyingReader)
                {
                    Assert.IsTrue(ptr == &reader);
                }
                // Can't use Assert.Throws() because ref structs can't be passed into lambdas.
                try
                {
                    serializer.GetFastBufferWriter();
                }
                catch (InvalidOperationException)
                {
                    // pass
                }
            }
        }
        
        // Not reimplementing the entire suite of all value tests for BufferSerializer since they're already tested
        // for the underlying structures. These are just basic tests to make sure the correct underlying functions
        // are being called.
        [Test]
        public void TestSerializingObjects()
        {
            Random random = new Random();
            int value = random.Next();
            object asObj = value;

            FastBufferWriter writer = new FastBufferWriter(100, Allocator.Temp);
            using (writer)
            {
                BufferSerializer<BufferSerializerWriter> serializer =
                    new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(ref writer));
                serializer.SerializeValue(ref asObj, typeof(int));

                FastBufferReader reader = new FastBufferReader(ref writer, Allocator.Temp);
                using (reader)
                {
                    BufferSerializer<BufferSerializerReader> deserializer =
                        new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(ref reader));
                    object readValue = 0;
                    deserializer.SerializeValue(ref readValue, typeof(int));
                    
                    Assert.AreEqual(value, readValue);
                }
            }
        }
        
        [Test]
        public void TestSerializingValues()
        {
            Random random = new Random();
            int value = random.Next();

            FastBufferWriter writer = new FastBufferWriter(100, Allocator.Temp);
            using (writer)
            {
                BufferSerializer<BufferSerializerWriter> serializer =
                    new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(ref writer));
                serializer.SerializeValue(ref value);

                FastBufferReader reader = new FastBufferReader(ref writer, Allocator.Temp);
                using (reader)
                {
                    BufferSerializer<BufferSerializerReader> deserializer =
                        new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(ref reader));
                    int readValue = 0;
                    deserializer.SerializeValue(ref readValue);
                    
                    Assert.AreEqual(value, readValue);
                }
            }
        }
        [Test]
        public void TestSerializingBytes()
        {
            Random random = new Random();
            byte value = (byte)random.Next();

            FastBufferWriter writer = new FastBufferWriter(100, Allocator.Temp);
            using (writer)
            {
                BufferSerializer<BufferSerializerWriter> serializer =
                    new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(ref writer));
                serializer.SerializeValue(ref value);

                FastBufferReader reader = new FastBufferReader(ref writer, Allocator.Temp);
                using (reader)
                {
                    BufferSerializer<BufferSerializerReader> deserializer =
                        new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(ref reader));
                    byte readValue = 0;
                    deserializer.SerializeValue(ref readValue);
                    
                    Assert.AreEqual(value, readValue);
                }
            }
        }
        [Test]
        public void TestSerializingArrays()
        {
            Random random = new Random();
            int[] value = {random.Next(), random.Next(), random.Next()};

            FastBufferWriter writer = new FastBufferWriter(100, Allocator.Temp);
            using (writer)
            {
                BufferSerializer<BufferSerializerWriter> serializer =
                    new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(ref writer));
                serializer.SerializeValue(ref value);

                FastBufferReader reader = new FastBufferReader(ref writer, Allocator.Temp);
                using (reader)
                {
                    BufferSerializer<BufferSerializerReader> deserializer =
                        new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(ref reader));
                    int[] readValue = null;
                    deserializer.SerializeValue(ref readValue);
                    
                    Assert.AreEqual(value, readValue);
                }
            }
        }
        [Test]
        public void TestSerializingStrings([Values] bool oneBytChars)
        {
            string value = "I am a test string";

            FastBufferWriter writer = new FastBufferWriter(100, Allocator.Temp);
            using (writer)
            {
                BufferSerializer<BufferSerializerWriter> serializer =
                    new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(ref writer));
                serializer.SerializeValue(ref value, oneBytChars);

                FastBufferReader reader = new FastBufferReader(ref writer, Allocator.Temp);
                using (reader)
                {
                    BufferSerializer<BufferSerializerReader> deserializer =
                        new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(ref reader));
                    string readValue = null;
                    deserializer.SerializeValue(ref readValue, oneBytChars);
                    
                    Assert.AreEqual(value, readValue);
                }
            }
        }
        
        
        [Test]
        public void TestSerializingValuesPreChecked()
        {
            Random random = new Random();
            int value = random.Next();

            FastBufferWriter writer = new FastBufferWriter(100, Allocator.Temp);
            using (writer)
            {
                BufferSerializer<BufferSerializerWriter> serializer =
                    new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(ref writer));
                try
                {
                    serializer.SerializeValuePreChecked(ref value);
                }
                catch (OverflowException e)
                {
                    // Pass
                }

                Assert.IsTrue(serializer.PreCheck(FastBufferWriter.GetWriteSize(value)));
                serializer.SerializeValuePreChecked(ref value);

                FastBufferReader reader = new FastBufferReader(ref writer, Allocator.Temp);
                using (reader)
                {
                    BufferSerializer<BufferSerializerReader> deserializer =
                        new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(ref reader));
                    int readValue = 0;
                    try
                    {
                        deserializer.SerializeValuePreChecked(ref readValue);
                    }
                    catch (OverflowException e)
                    {
                        // Pass
                    }

                    Assert.IsTrue(deserializer.PreCheck(FastBufferWriter.GetWriteSize(value)));
                    deserializer.SerializeValuePreChecked(ref readValue);
                    
                    Assert.AreEqual(value, readValue);
                }
            }
        }
        [Test]
        public void TestSerializingBytesPreChecked()
        {
            Random random = new Random();
            byte value = (byte)random.Next();

            FastBufferWriter writer = new FastBufferWriter(100, Allocator.Temp);
            using (writer)
            {
                BufferSerializer<BufferSerializerWriter> serializer =
                    new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(ref writer));
                try
                {
                    serializer.SerializeValuePreChecked(ref value);
                }
                catch (OverflowException e)
                {
                    // Pass
                }

                Assert.IsTrue(serializer.PreCheck(FastBufferWriter.GetWriteSize(value)));
                serializer.SerializeValuePreChecked(ref value);

                FastBufferReader reader = new FastBufferReader(ref writer, Allocator.Temp);
                using (reader)
                {
                    BufferSerializer<BufferSerializerReader> deserializer =
                        new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(ref reader));
                    byte readValue = 0;
                    try
                    {
                        deserializer.SerializeValuePreChecked(ref readValue);
                    }
                    catch (OverflowException e)
                    {
                        // Pass
                    }

                    Assert.IsTrue(deserializer.PreCheck(FastBufferWriter.GetWriteSize(value)));
                    deserializer.SerializeValuePreChecked(ref readValue);
                    
                    Assert.AreEqual(value, readValue);
                }
            }
        }
        [Test]
        public void TestSerializingArraysPreChecked()
        {
            Random random = new Random();
            int[] value = {random.Next(), random.Next(), random.Next()};

            FastBufferWriter writer = new FastBufferWriter(100, Allocator.Temp);
            using (writer)
            {
                BufferSerializer<BufferSerializerWriter> serializer =
                    new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(ref writer));
                try
                {
                    serializer.SerializeValuePreChecked(ref value);
                }
                catch (OverflowException e)
                {
                    // Pass
                }

                Assert.IsTrue(serializer.PreCheck(FastBufferWriter.GetWriteSize(value)));
                serializer.SerializeValuePreChecked(ref value);

                FastBufferReader reader = new FastBufferReader(ref writer, Allocator.Temp);
                using (reader)
                {
                    BufferSerializer<BufferSerializerReader> deserializer =
                        new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(ref reader));
                    int[] readValue = null;
                    try
                    {
                        deserializer.SerializeValuePreChecked(ref readValue);
                    }
                    catch (OverflowException e)
                    {
                        // Pass
                    }

                    Assert.IsTrue(deserializer.PreCheck(FastBufferWriter.GetWriteSize(value)));
                    deserializer.SerializeValuePreChecked(ref readValue);
                    
                    Assert.AreEqual(value, readValue);
                }
            }
        }
        [Test]
        public void TestSerializingStringsPreChecked([Values] bool oneBytChars)
        {
            string value = "I am a test string";

            FastBufferWriter writer = new FastBufferWriter(100, Allocator.Temp);
            using (writer)
            {
                BufferSerializer<BufferSerializerWriter> serializer =
                    new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(ref writer));
                try
                {
                    serializer.SerializeValuePreChecked(ref value, oneBytChars);
                }
                catch (OverflowException e)
                {
                    // Pass
                }

                Assert.IsTrue(serializer.PreCheck(FastBufferWriter.GetWriteSize(value, oneBytChars)));
                serializer.SerializeValuePreChecked(ref value, oneBytChars);

                FastBufferReader reader = new FastBufferReader(ref writer, Allocator.Temp);
                using (reader)
                {
                    BufferSerializer<BufferSerializerReader> deserializer =
                        new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(ref reader));
                    string readValue = null;
                    try
                    {
                        deserializer.SerializeValuePreChecked(ref readValue, oneBytChars);
                    }
                    catch (OverflowException e)
                    {
                        // Pass
                    }

                    Assert.IsTrue(deserializer.PreCheck(FastBufferWriter.GetWriteSize(value, oneBytChars)));
                    deserializer.SerializeValuePreChecked(ref readValue, oneBytChars);
                    
                    Assert.AreEqual(value, readValue);
                }
            }
        }
        /*
        public void SerializeValue(ref object value, Type type, bool isNullable = false);
        public void SerializeValue(ref INetworkSerializable value);
        public void SerializeValue(ref GameObject value);
        public void SerializeValue(ref NetworkObject value);
        public void SerializeValue(ref NetworkBehaviour value);

        public bool PreCheck(int amount);
        public void SerializeValuePreChecked(ref GameObject value);
        public void SerializeValuePreChecked(ref NetworkObject value);
        public void SerializeValuePreChecked(ref NetworkBehaviour value);*/
        
        
        private delegate void GameObjectTestDelegate(GameObject obj, NetworkBehaviour networkBehaviour,
            NetworkObject networkObject);
        private void RunGameObjectTest(GameObjectTestDelegate testCode)
        {
            var obj = new GameObject("Object");
            var networkBehaviour = obj.AddComponent<NetworkObjectTests.EmptyNetworkBehaviour>();
            var networkObject = obj.AddComponent<NetworkObject>();
            // Create networkManager component
            var networkManager = obj.AddComponent<NetworkManager>();
            networkManager.SetSingleton();
            networkObject.NetworkManagerOwner = networkManager;

            // Set the NetworkConfig
            networkManager.NetworkConfig = new NetworkConfig()
            {
                // Set the current scene to prevent unexpected log messages which would trigger a failure
                RegisteredScenes = new List<string>() { SceneManager.GetActiveScene().name },
                // Set transport
                NetworkTransport = obj.AddComponent<DummyTransport>()
            };

            networkManager.StartServer();

            try
            {
                testCode(obj, networkBehaviour, networkObject);
            }
            finally
            {
                GameObject.DestroyImmediate(obj);
                networkManager.StopServer();
            }
        }
        
        [Test]
        public void TestSerializingGameObjects()
        {
            RunGameObjectTest((obj, networkBehaviour, networkObject) =>
                {
                    FastBufferWriter writer = new FastBufferWriter(100, Allocator.Temp);
                    using (writer)
                    {
                        BufferSerializer<BufferSerializerWriter> serializer =
                            new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(ref writer));
                        serializer.SerializeValue(ref obj);

                        FastBufferReader reader = new FastBufferReader(ref writer, Allocator.Temp);
                        using (reader)
                        {
                            BufferSerializer<BufferSerializerReader> deserializer =
                                new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(ref reader));
                            GameObject readValue = null;
                            deserializer.SerializeValue(ref readValue);
                    
                            Assert.AreEqual(obj, readValue);
                        }
                    }
                }
            );
        }
        
        [Test]
        public void TestSerializingNetworkObjects()
        {
            RunGameObjectTest((obj, networkBehaviour, networkObject) =>
                {
                    FastBufferWriter writer = new FastBufferWriter(100, Allocator.Temp);
                    using (writer)
                    {
                        BufferSerializer<BufferSerializerWriter> serializer =
                            new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(ref writer));
                        serializer.SerializeValue(ref networkObject);

                        FastBufferReader reader = new FastBufferReader(ref writer, Allocator.Temp);
                        using (reader)
                        {
                            BufferSerializer<BufferSerializerReader> deserializer =
                                new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(ref reader));
                            NetworkObject readValue = null;
                            deserializer.SerializeValue(ref readValue);
                    
                            Assert.AreEqual(networkObject, readValue);
                        }
                    }
                }
            );
        }
        
        [Test]
        public void TestSerializingNetworkBehaviours()
        {
            RunGameObjectTest((obj, networkBehaviour, networkObject) =>
                {
                    FastBufferWriter writer = new FastBufferWriter(100, Allocator.Temp);
                    using (writer)
                    {
                        BufferSerializer<BufferSerializerWriter> serializer =
                            new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(ref writer));
                        serializer.SerializeValue(ref networkBehaviour);

                        FastBufferReader reader = new FastBufferReader(ref writer, Allocator.Temp);
                        using (reader)
                        {
                            BufferSerializer<BufferSerializerReader> deserializer =
                                new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(ref reader));
                            NetworkBehaviour readValue = null;
                            deserializer.SerializeValue(ref readValue);
                    
                            Assert.AreEqual(networkBehaviour, readValue);
                        }
                    }
                }
            );
        }
        
        [Test]
        public void TestSerializingGameObjectsPreChecked()
        {
            RunGameObjectTest((obj, networkBehaviour, networkObject) =>
                {
                    FastBufferWriter writer = new FastBufferWriter(100, Allocator.Temp);
                    using (writer)
                    {
                        BufferSerializer<BufferSerializerWriter> serializer =
                            new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(ref writer));
                        try
                        {
                            serializer.SerializeValuePreChecked(ref obj);
                        }
                        catch (OverflowException e)
                        {
                            // Pass
                        }

                        Assert.IsTrue(serializer.PreCheck(FastBufferWriter.GetWriteSize(obj)));
                        serializer.SerializeValuePreChecked(ref obj);

                        FastBufferReader reader = new FastBufferReader(ref writer, Allocator.Temp);
                        using (reader)
                        {
                            BufferSerializer<BufferSerializerReader> deserializer =
                                new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(ref reader));
                            GameObject readValue = null;
                            try
                            {
                                deserializer.SerializeValuePreChecked(ref readValue);
                            }
                            catch (OverflowException e)
                            {
                                // Pass
                            }

                            Assert.IsTrue(deserializer.PreCheck(FastBufferWriter.GetWriteSize(readValue)));
                            deserializer.SerializeValuePreChecked(ref readValue);
                    
                            Assert.AreEqual(obj, readValue);
                        }
                    }
                }
            );
        }
        
        [Test]
        public void TestSerializingNetworkObjectsPreChecked()
        {
            RunGameObjectTest((obj, networkBehaviour, networkObject) =>
                {
                    FastBufferWriter writer = new FastBufferWriter(100, Allocator.Temp);
                    using (writer)
                    {
                        BufferSerializer<BufferSerializerWriter> serializer =
                            new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(ref writer));
                        try
                        {
                            serializer.SerializeValuePreChecked(ref networkObject);
                        }
                        catch (OverflowException e)
                        {
                            // Pass
                        }

                        Assert.IsTrue(serializer.PreCheck(FastBufferWriter.GetWriteSize(networkObject)));
                        serializer.SerializeValuePreChecked(ref networkObject);

                        FastBufferReader reader = new FastBufferReader(ref writer, Allocator.Temp);
                        using (reader)
                        {
                            BufferSerializer<BufferSerializerReader> deserializer =
                                new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(ref reader));
                            NetworkObject readValue = null;
                            try
                            {
                                deserializer.SerializeValuePreChecked(ref readValue);
                            }
                            catch (OverflowException e)
                            {
                                // Pass
                            }

                            Assert.IsTrue(deserializer.PreCheck(FastBufferWriter.GetWriteSize(readValue)));
                            deserializer.SerializeValuePreChecked(ref readValue);
                    
                            Assert.AreEqual(networkObject, readValue);
                        }
                    }
                }
            );
        }
        
        [Test]
        public void TestSerializingNetworkBehavioursPreChecked()
        {
            RunGameObjectTest((obj, networkBehaviour, networkObject) =>
                {
                    FastBufferWriter writer = new FastBufferWriter(100, Allocator.Temp);
                    using (writer)
                    {
                        BufferSerializer<BufferSerializerWriter> serializer =
                            new BufferSerializer<BufferSerializerWriter>(new BufferSerializerWriter(ref writer));
                        try
                        {
                            serializer.SerializeValuePreChecked(ref networkBehaviour);
                        }
                        catch (OverflowException e)
                        {
                            // Pass
                        }

                        Assert.IsTrue(serializer.PreCheck(FastBufferWriter.GetWriteSize(networkBehaviour)));
                        serializer.SerializeValuePreChecked(ref networkBehaviour);

                        FastBufferReader reader = new FastBufferReader(ref writer, Allocator.Temp);
                        using (reader)
                        {
                            BufferSerializer<BufferSerializerReader> deserializer =
                                new BufferSerializer<BufferSerializerReader>(new BufferSerializerReader(ref reader));
                            NetworkBehaviour readValue = null;
                            try
                            {
                                deserializer.SerializeValuePreChecked(ref readValue);
                            }
                            catch (OverflowException e)
                            {
                                // Pass
                            }

                            Assert.IsTrue(deserializer.PreCheck(FastBufferWriter.GetWriteSize(readValue)));
                            deserializer.SerializeValuePreChecked(ref readValue);
                    
                            Assert.AreEqual(networkBehaviour, readValue);
                        }
                    }
                }
            );
        }
    }
}