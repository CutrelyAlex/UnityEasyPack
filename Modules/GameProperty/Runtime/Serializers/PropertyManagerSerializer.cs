using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EasyPack.Category;
using EasyPack.CustomData;
using EasyPack.Serialization;
using UnityEngine;

namespace EasyPack.GamePropertySystem
{
    /// <summary>
    ///     GamePropertyManager 序列化器
    /// </summary>
    public class PropertyManagerSerializer : ITypeSerializer<GamePropertyService, PropertyManagerDTO>
    {
        private readonly GamePropertyJsonSerializer _propertySerializer = new();
        private readonly CategoryManagerJsonSerializer<GameProperty, long> _categorySerializer = new(p => p.UID);

        public PropertyManagerDTO ToSerializable(GamePropertyService obj)
        {
            if (obj == null) return null;

            var propertiesList = new List<PropertyEntry>();
            var propertyDisplayInfoList = new List<PropertyDisplayInfoEntry>();

            foreach (string propertyId in obj.GetAllPropertyIds())
            {
                GameProperty property = obj.Get(propertyId);
                if (property == null) continue;

                string propertyJson = _propertySerializer.SerializeToJson(property);
                propertiesList.Add(new() { ID = propertyId, SerializedProperty = propertyJson });

                PropertyDisplayInfo propertyDisplayInfo = obj.GetPropertyDisplayInfo(propertyId);
                if (propertyDisplayInfo != null)
                {
                    propertyDisplayInfoList.Add(new()
                    {
                        PropertyID = propertyId,
                        DisplayName = propertyDisplayInfo.DisplayName,
                        Description = propertyDisplayInfo.Description,
                        IconPath = propertyDisplayInfo.IconPath,
                    });
                }
            }

            var dto = new PropertyManagerDTO
            {
                Properties = propertiesList.ToArray(),
                PropertyDisplayInfo = propertyDisplayInfoList.ToArray()
            };

            // 序列化分类系统状态
            if (obj._categoryManager is CategoryManager<GameProperty, long> concreteManager)
            {
                dto.CategoryState = concreteManager.GetSerializableState(
                    entity => _propertySerializer.SerializeToJson(entity),
                    key => key.ToString(),
                    metadata => JsonUtility.ToJson(new CustomDataWrapper { Entries = metadata.ToArray() })
                );
            }

            return dto;
        }

        public GamePropertyService FromSerializable(PropertyManagerDTO dto)
        {
            if (dto == null)
            {
                throw new SerializationException("DTO 为 null", typeof(GamePropertyService),
                    SerializationErrorCode.DeserializationFailed);
            }

            var manager = new GamePropertyService();
            Task.Run(async () => await manager.InitializeAsync()).Wait();

            // 1. 恢复分类系统状态
            if (dto.CategoryState != null && manager._categoryManager is CategoryManager<GameProperty, long> concreteManager)
            {
                var restoredManager = _categorySerializer.FromSerializable(dto.CategoryState);
                if (manager._categoryManager is IDisposable disposable) disposable.Dispose();
                manager._categoryManager = restoredManager;
            }

            // 2. 注册属性（RegisterInternal 会自动处理与 CategoryManager 的同步）
            if (dto.Properties != null)
            {
                foreach (PropertyEntry entry in dto.Properties)
                {
                    GameProperty property = _propertySerializer.DeserializeFromJson(entry.SerializedProperty);
                    if (property == null) continue;

                    PropertyDisplayInfo propertyDisplayInfo = null;
                    if (dto.PropertyDisplayInfo != null)
                    {
                        PropertyDisplayInfoEntry propertyDisplayInfoEntry = dto.PropertyDisplayInfo.FirstOrDefault(m => m.PropertyID == entry.ID);
                        if (!string.IsNullOrEmpty(propertyDisplayInfoEntry.PropertyID))
                        {
                            propertyDisplayInfo = new PropertyDisplayInfo
                            {
                                DisplayName = propertyDisplayInfoEntry.DisplayName,
                                Description = propertyDisplayInfoEntry.Description,
                                IconPath = propertyDisplayInfoEntry.IconPath,
                            };
                        }
                    }

                    // 注意：此时 CategoryManager 已经有了分类、标签和自定义数据，
                    // RegisterInternal 会优先使用这些数据。
                    manager.Register(property, "Default", propertyDisplayInfo);
                }
            }

            return manager;
        }

        public string ToJson(PropertyManagerDTO dto) => dto == null ? null : JsonUtility.ToJson(dto);

        public PropertyManagerDTO FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;

            try
            {
                var dto = JsonUtility.FromJson<PropertyManagerDTO>(json);
                if (dto == null)
                {
                    throw new SerializationException("JSON 解析失败", typeof(GamePropertyService),
                        SerializationErrorCode.DeserializationFailed);
                }

                return dto;
            }
            catch (Exception ex) when (ex is not SerializationException)
            {
                throw new SerializationException($"JSON 解析失败: {ex.Message}",
                    typeof(GamePropertyService), SerializationErrorCode.DeserializationFailed, ex);
            }
        }

        public string SerializeToJson(GamePropertyService obj) => ToJson(ToSerializable(obj));

        public GamePropertyService DeserializeFromJson(string json) => FromSerializable(FromJson(json));

        [Serializable]
        private class CustomDataWrapper
        {
            public CustomDataEntry[] Entries;
        }
    }
}