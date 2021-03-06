﻿using System;
using System.Collections.Generic;

namespace GServer.Containers
{
    public static class CollectionExtensions
    {
        
        public static void Invoke<T>(this IList<T> collection, Action<T> action) {
            foreach (var element in collection) {
                action.Invoke(element);
            }
        }

        #region List Serialization
        public static void SerializeTo(this IList<int> collection, DataStorage ds) {
            ds.Push(collection.Count);
            foreach (var element in collection) {
                ds.Push(element);
            }
        }
        public static byte[] Serialize(this IList<int> collection) {
            var ds = DataStorage.CreateForWrite();
            collection.SerializeTo(ds);
            return ds.Serialize();
        }
        public static void DeserializeFrom(this IList<int> collection, DataStorage ds) {
            var len = ds.ReadInt32();
            for (var i = 0; i < len; i++) {
                collection.Add(ds.ReadInt32());
            }
        }
        public static void FillDeserialize(this IList<int> collection, byte[] buffer)
        {
            var ds = DataStorage.CreateForRead(buffer);
            collection.DeserializeFrom(ds);
        }

        public static void DeserializeFrom(this IList<byte> collection, DataStorage ds) {
            var len = ds.ReadInt32();
            for (var i = 0; i < len; i++) {
                collection.Add(ds.ReadByte());
            }
        }
        public static void SerializeTo(this IList<byte> collection, DataStorage ds)
        {
            ds.Push(collection.Count);
            foreach (var element in collection)
            {
                ds.Push(element);
            }
        }
        public static byte[] Serialize(this IList<byte> collection)
        {
            var ds = DataStorage.CreateForWrite();
            collection.SerializeTo(ds);
            return ds.Serialize();
        }
        public static void FillDeserialize(this IList<byte> collection, byte[] buffer)
        {
            var ds = DataStorage.CreateForRead(buffer);
            collection.DeserializeFrom(ds);
        }

        public static void SerializeTo(this IList<long> collection, DataStorage ds) {
            ds.Push(collection.Count);
            foreach (var element in collection) {
                ds.Push(element);
            }
        }
        public static byte[] Serialize(this IList<long> collection) {
            var ds = DataStorage.CreateForWrite();
            collection.SerializeTo(ds);
            return ds.Serialize();
        }
        public static void DeserializeFrom(this IList<long> collection, DataStorage ds) {
            var len = ds.ReadInt32();
            for (var i = 0; i < len; i++) {
                collection.Add(ds.ReadInt64());
            }
        }
        public static void FillDeserialize(this IList<long> collection, byte[] buffer) {
            var ds = DataStorage.CreateForRead(buffer);
            collection.DeserializeFrom(ds);
        }

        public static void SerializeTo(this IList<short> collection, DataStorage ds) {
            ds.Push(collection.Count);
            foreach (var element in collection) {
                ds.Push(element);
            }
        }
        public static byte[] Serialize(this IList<short> collection) {
            var ds = DataStorage.CreateForWrite();
            collection.SerializeTo(ds);
            return ds.Serialize();
        }
        public static void DeserializeFrom(this IList<short> collection, DataStorage ds) {
            var len = ds.ReadInt32();
            for (var i = 0; i < len; i++) {
                collection.Add(ds.ReadInt16());
            }
        }
        public static void FillDeserialize(this IList<short> collection, byte[] buffer) {
            var ds = DataStorage.CreateForRead(buffer);
            collection.DeserializeFrom(ds);
        }

        public static void SerializeTo(this IList<float> collection, DataStorage ds) {
            ds.Push(collection.Count);
            foreach (var element in collection) {
                ds.Push(element);
            }
        }
        public static byte[] Serialize(this IList<float> collection) {
            var ds = DataStorage.CreateForWrite();
            collection.SerializeTo(ds);
            return ds.Serialize();
        }
        public static void DeserializeFrom(this IList<float> collection, DataStorage ds) {
            var len = ds.ReadInt32();
            for (var i = 0; i < len; i++) {
                collection.Add(ds.ReadFloat());
            }
        }
        public static void FillDeserialize(this IList<float> collection, byte[] buffer) {
            var ds = DataStorage.CreateForRead(buffer);
            collection.DeserializeFrom(ds);
        }

        public static void SerializeTo(this IList<double> collection, DataStorage ds) {
            ds.Push(collection.Count);
            foreach (var element in collection) {
                ds.Push(element);
            }
        }
        public static byte[] Serialize(this IList<double> collection) {
            var ds = DataStorage.CreateForWrite();
            collection.SerializeTo(ds);
            return ds.Serialize();
        }
        public static void DeserializeFrom(this IList<double> collection, DataStorage ds) {
            var len = ds.ReadInt32();
            for (var i = 0; i < len; i++) {
                collection.Add(ds.ReadDouble());
            }
        }
        public static void FillDeserialize(this IList<double> collection, byte[] buffer) {
            var ds = DataStorage.CreateForRead(buffer);
            collection.DeserializeFrom(ds);
        }

        public static void SerializeTo(this IList<string> collection, DataStorage ds) {
            ds.Push(collection.Count);
            foreach (var element in collection) {
                ds.Push(element);
            }
        }
        public static byte[] Serialize(this IList<string> collection) {
            var ds = DataStorage.CreateForWrite();
            collection.SerializeTo(ds);
            return ds.Serialize();
        }
        public static void DeserializeFrom(this IList<string> collection, DataStorage ds) {
            var len = ds.ReadInt32();
            for (var i = 0; i < len; i++) {
                collection.Add(ds.ReadString());
            }
        }
        public static void FillDeserialize(this IList<string> collection, byte[] buffer) {
            var ds = DataStorage.CreateForRead(buffer);
            collection.DeserializeFrom(ds);
        }

        public static void SerializeTo(this IList<char> collection, DataStorage ds) {
            ds.Push(collection.Count);
            foreach (var element in collection) {
                ds.Push(element);
            }
        }
        public static byte[] Serialize(this IList<char> collection) {
            var ds = DataStorage.CreateForWrite();
            collection.SerializeTo(ds);
            return ds.Serialize();
        }
        public static void DeserializeFrom(this IList<char> collection, DataStorage ds) {
            var len = ds.ReadInt32();
            for (var i = 0; i < len; i++) {
                collection.Add(ds.ReadChar());
            }
        }
        public static void FillDeserialize(this IList<char> collection, byte[] buffer) {
            var ds = DataStorage.CreateForRead(buffer);
            collection.DeserializeFrom(ds);
        }

        public static void SerializeTo<TData>(this IList<TData> collection, DataStorage ds)
            where TData : IDeepSerializable {
            ds.Push(collection.Count);
            foreach (var element in collection) {
                ds.Push(element);
            }
        }
        public static void DeserializeFrom<TData>(this IList<TData> collection, DataStorage ds)
            where TData : IDeepDeserializable, new() {
            var len = ds.ReadInt32();
            for (var i = 0; i < len; i++) {
                var element = new TData();
                element.ReadFromDs(ds);
                collection.Add(element);
            }
        }
        public static byte[] Serialize<TData>(this IList<TData> collection)
            where TData : IDeepSerializable {
            var ds = DataStorage.CreateForWrite();
            collection.SerializeTo(ds);
            return ds.Serialize();
        }
        public static void FillDeserialize<TData>(this IList<TData> collection, byte[] buffer)
            where TData : IDeepDeserializable, new() {
            var ds = DataStorage.CreateForRead(buffer);
            collection.DeserializeFrom(ds);
        }
        #endregion
        #region Dictionary serialization
        public static void SerializeTo(this IDictionary<object, object> collection, DataStorage ds)
        {
            var len = collection.Count;
            ds.Push(len);
            foreach (var pair in collection)
            {
                ds.Push(pair.Key.GetType()).Push(pair.Value.GetType());
                ds.Push(pair.Key).Push(pair.Value);
            }
        }
        public static void DeserializeFrom(this IDictionary<object, object> collection, DataStorage ds)
        {
            var len = ds.ReadInt32();
            for (var i = 0; i < len; i++)
            {
                var val1 = ds.ReadString();
                var val2 = ds.ReadString();
                collection.Add(ds.ReadObject(val1), ds.ReadObject(val2));
            }
        }
        public static byte[] Serialize(this IDictionary<object, object> collection)
        {
            var ds = DataStorage.CreateForWrite();
            collection.SerializeTo(ds);
            return ds.Serialize();
        }
        public static void FillDeserialize(this IDictionary<object, object> collection, byte[] buffer)
        {
            var ds = DataStorage.CreateForRead(buffer);
            collection.DeserializeFrom(ds);
        }
        #endregion
    }
}