using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace JsonSubTypes
{
    internal class JsonSubtypesByDiscriminatorValueConverter : JsonSubtypesConverter
    {
        [ThreadStatic] private static bool _isInsideWrite;
        [ThreadStatic] private static bool _allowNextWrite;

        private readonly Dictionary<Type, object> _supportedTypes = new Dictionary<Type, object>();
        private readonly Dictionary<object, Type> _subTypeMapping;
        private readonly bool _serializeDiscriminatorProperty;
        private readonly bool _addDiscriminatorFirst;
        
        internal JsonSubtypesByDiscriminatorValueConverter(Type baseType, string discriminatorProperty,
            Dictionary<object, Type> subTypeMapping, bool serializeDiscriminatorProperty, bool addDiscriminatorFirst) : base(baseType, discriminatorProperty)
        {
            _serializeDiscriminatorProperty = serializeDiscriminatorProperty;
            _subTypeMapping = subTypeMapping;
            _addDiscriminatorFirst = addDiscriminatorFirst;
            foreach (var type in _subTypeMapping)
            {
                _supportedTypes.Add(type.Value, type.Key);
            }
        }

        public override bool CanWrite
        {
            get
            {
                if (!_serializeDiscriminatorProperty)
                    return false;

                if (!_isInsideWrite)
                    return true;

                if (_allowNextWrite)
                {
                    _allowNextWrite = false;
                    return true;
                }

                _allowNextWrite = true;
                return false;
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return base.CanConvert(objectType) || _supportedTypes.ContainsKey(objectType);
        }

        protected override Dictionary<object, Type> GetSubTypeMapping(Type type)
        {
            return _subTypeMapping;
        }

        private protected override Dictionary<string, Type> GetTypesByPropertyPresence(Type parentType)
        {
            return new Dictionary<string, Type>();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            JObject jsonObj;
            _isInsideWrite = true;
            _allowNextWrite = false;
            try
            {
                jsonObj = JObject.FromObject(value, serializer);
            }
            finally
            {
                _isInsideWrite = false;
            }

            Fill(jsonObj, value, serializer);

            jsonObj.WriteTo(writer);
        }
        
        private void Fill(JObject jsonObj, object value, JsonSerializer serializer)
        {
            var supportedType = _supportedTypes[value.GetType()];
            var typeMappingPropertyValue = JToken.FromObject(supportedType, serializer);
            if (_addDiscriminatorFirst)
            {
                jsonObj.AddFirst(new JProperty(JsonDiscriminatorPropertyName, typeMappingPropertyValue));
            }
            else
            {
                jsonObj.Add(JsonDiscriminatorPropertyName, typeMappingPropertyValue);
            }
        }
    }
}
