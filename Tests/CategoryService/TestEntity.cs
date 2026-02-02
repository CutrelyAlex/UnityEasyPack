using System;
using EasyPack.Serialization;
using UnityEngine;

namespace EasyPack.CategoryTests
{
    /// <summary>
    /// 测试用的简单实体类
    /// </summary>
    [Serializable]
    public class TestEntity : ISerializable
    {
        [SerializeField]private string _id;
        [SerializeField]private string _name;
        [SerializeField]private string _description;
        private DateTime _createdTime;
        [SerializeField]private int _value;

        /// <summary>
        /// 实体唯一标识
        /// </summary>
        public string Id
        {
            get => _id;
            set => _id = value;
        }

        /// <summary>
        /// 实体名称
        /// </summary>
        public string Name
        {
            get => _name;
            set => _name = value;
        }

        /// <summary>
        /// 实体描述
        /// </summary>
        public string Description
        {
            get => _description;
            set => _description = value;
        }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime
        {
            get => _createdTime;
            set => _createdTime = value;
        }

        /// <summary>
        /// 数值属性（用于测试复杂场景）
        /// </summary>
        public int Value
        {
            get => _value;
            set => _value = value;
        }

        public TestEntity()
        {
            CreatedTime = DateTime.UtcNow;
        }

        public TestEntity(string id, string name)
        {
            Id = id;
            Name = name;
            CreatedTime = DateTime.UtcNow;
        }

        public TestEntity(string id, string name, string description)
        {
            Id = id;
            Name = name;
            Description = description;
            CreatedTime = DateTime.UtcNow;
        }

        public override string ToString()
        {
            return $"TestEntity(Id={Id}, Name={Name})";
        }
    }

    /// <summary>
    /// 测试实体构建器，用于流式创建测试数据
    /// </summary>
    public class TestEntityBuilder
    {
        private string _id;
        private string _name;
        private string _description;
        private int _value;

        public TestEntityBuilder WithId(string id)
        {
            _id = id;
            return this;
        }

        public TestEntityBuilder WithName(string name)
        {
            _name = name;
            return this;
        }

        public TestEntityBuilder WithDescription(string description)
        {
            _description = description;
            return this;
        }

        public TestEntityBuilder WithValue(int value)
        {
            _value = value;
            return this;
        }

        public TestEntity Build()
        {
            var entity = new TestEntity
            {
                Id = _id ?? Guid.NewGuid().ToString(),
                Name = _name ?? "Default",
                Description = _description ?? "",
                Value = _value
            };
            return entity;
        }

        public static TestEntityBuilder Create()
        {
            return new TestEntityBuilder();
        }

        public static TestEntity CreateDefault(string id = null)
        {
            var entity = new TestEntity
            {
                Id = id ?? Guid.NewGuid().ToString(),
                Name = "Default",
                Description = ""
            };
            return entity;
        }
    }
}
