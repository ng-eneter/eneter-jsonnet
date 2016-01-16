/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2016
*/


using Eneter.Messaging.DataProcessing.Serializing;
using Newtonsoft.Json;
using System;

namespace Eneter.JsonNet
{
    /// <summary>
    /// EneterJsonNetSerializer is a serializer for Eneter Messaging Framework based on popular Json.NET serializer.
    /// </summary>
    /// <remarks>
    /// This serializer is intended to be used with Eneter Messaging Framework to serialize/deserialize messages.
    /// The implementation of the serializer is based on Json.NET serializer created by James Newton-King.
    /// http://www.newtonsoft.com/json
    /// </remarks>
    public class JsonNetSerializer : ISerializer
    {
        /// <summary>
        /// Constructs the serializer.
        /// </summary>
        public JsonNetSerializer()
        {
            myJsonSettings = new JsonSerializerSettings();
        }

        /// <summary>
        /// Constructs the serializer.
        /// </summary>
        /// <param name="jsonParameters">JSON parameters.</param>
        public JsonNetSerializer(JsonSerializerSettings jsonParameters)
        {
            myJsonSettings = jsonParameters;
        }

        /// <summary>
        /// Deserializes data.
        /// </summary>
        /// <typeparam name="T">Type of the object which shall be deserialized.</typeparam>
        /// <param name="serializedData">JSON serialized string which shall be deserialized.</param>
        /// <returns>instance of deserialized object</returns>
        public T Deserialize<T>(object serializedData)
        {
            T aDeserialized = JsonConvert.DeserializeObject<T>((string)serializedData, myJsonSettings);
            return aDeserialized;
        }

        /// <summary>
        /// Serializes data.
        /// </summary>
        /// <typeparam name="T">Type of the objec which shall be serialized.</typeparam>
        /// <param name="dataToSerialize">instance of the object which shall be serialized.</param>
        /// <returns>string containing JSON serialization</returns>
        public object Serialize<T>(T dataToSerialize)
        {
            Type aTypeToSerialize = typeof(T);
            return JsonConvert.SerializeObject(dataToSerialize, aTypeToSerialize, myJsonSettings);
        }

        private JsonSerializerSettings myJsonSettings;
    }
}
