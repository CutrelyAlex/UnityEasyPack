using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using EasyPack.GamePropertySystem;

namespace EasyPack
{
    /// <summary>
    /// GamePropertyManager 序列化器
    /// </summary>
    public class PropertyManagerSerializer : ITypeSerializer<GamePropertyManager, PropertyManagerDTO>
    {
        private readonly GamePropertyJsonSerializer _propertySerializer = new GamePropertyJsonSerializer();

        public PropertyManagerDTO ToSerializable(GamePropertyManager obj)
        {
            if (obj == null) return null;

            var propertiesList = new List<PropertyEntry>();
            var metadataList = new List<MetadataEntry>();

            foreach (var propertyId in obj.GetAllPropertyIds())
            {
                var property = obj.Get(propertyId);
                if (property == null) continue;

                string propertyJson = _propertySerializer.SerializeToJson(property);

                string category = "Default";
                foreach (var cat in obj.GetAllCategories())
                {
                    if (obj.GetByCategory(cat).Any(p => p.ID == propertyId))
                    {
                        category = cat;
                        break;
                    }
                }

                propertiesList.Add(new PropertyEntry
                {
                    ID = propertyId,
                    Category = category,
                    SerializedProperty = propertyJson
                });

                var metadata = obj.GetMetadata(propertyId);
                if (metadata != null)
                {
                    metadataList.Add(new MetadataEntry
                    {
                        PropertyID = propertyId,
                        DisplayName = metadata.DisplayName,
                        Description = metadata.Description,
                        IconPath = metadata.IconPath,
                        Tags = metadata.Tags,
                        CustomDataJson = SerializeCustomData(metadata.CustomData)
                    });
                }
            }

            return new PropertyManagerDTO
            {
                Properties = propertiesList.ToArray(),
                Metadata = metadataList.ToArray()
            };
        }

        public GamePropertyManager FromSerializable(PropertyManagerDTO dto)
        {
            if (dto == null)
                throw new SerializationException("DTO 为 null", typeof(GamePropertyManager),
                    SerializationErrorCode.DeserializationFailed);

            var manager = new GamePropertyManager();
            manager.InitializeAsync().Wait();

            if (dto.Properties != null)
            {
                foreach (var entry in dto.Properties)
                {
                    var property = _propertySerializer.DeserializeFromJson(entry.SerializedProperty);
                    if (property == null) continue;

                    PropertyMetadata metadata = null;
                    if (dto.Metadata != null)
                    {
                        var metadataEntry = dto.Metadata.FirstOrDefault(m => m.PropertyID == entry.ID);
                        if (!string.IsNullOrEmpty(metadataEntry.PropertyID))
                        {
                            metadata = new PropertyMetadata
                            {
                                DisplayName = metadataEntry.DisplayName,
                                Description = metadataEntry.Description,
                                IconPath = metadataEntry.IconPath,
                                Tags = metadataEntry.Tags,
                                CustomData = DeserializeCustomData(metadataEntry.CustomDataJson)
                            };
                        }
                    }

                    manager.Register(property, entry.Category ?? "Default", metadata);
                }
            }

            return manager;
        }

        public string ToJson(PropertyManagerDTO dto) =>
            dto == null ? null : JsonUtility.ToJson(dto);

        public PropertyManagerDTO FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            var dto = JsonUtility.FromJson<PropertyManagerDTO>(json);
            if (dto == null)
                throw new SerializationException("JSON 解析失败", typeof(GamePropertyManager),
                    SerializationErrorCode.DeserializationFailed);
            return dto;
        }

        public string SerializeToJson(GamePropertyManager obj) =>
            ToJson(ToSerializable(obj));

        public GamePropertyManager DeserializeFromJson(string json) =>
            FromSerializable(FromJson(json));

        private string SerializeCustomData(List<CustomDataEntry> customData)
        {
            if (customData == null || customData.Count == 0) return null;
            var wrapper = new CustomDataWrapper { Entries = customData.ToArray() };
            return JsonUtility.ToJson(wrapper);
        }

        private List<CustomDataEntry> DeserializeCustomData(string json)
        {
            if (string.IsNullOrEmpty(json)) return new List<CustomDataEntry>();
            var wrapper = JsonUtility.FromJson<CustomDataWrapper>(json);
            return wrapper?.Entries != null ? new List<CustomDataEntry>(wrapper.Entries) : new List<CustomDataEntry>();
        }

        [Serializable]
        private class CustomDataWrapper
        {
            public CustomDataEntry[] Entries;
        }
    }
}

