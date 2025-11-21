using System;

namespace EasyPack.ObjectPool
{
    /// <summary>
    /// 64 位对象池标记。高 16 位用于类型标识，低 48 位用于自定义配置标识。
    /// 允许同一类型在不同配置下创建多个对象池且互不冲突。
    /// </summary>
    public readonly struct PoolTag : IEquatable<PoolTag>, IComparable<PoolTag>
    {
        private const int TypeBits = 16;
        private const ulong ConfigMask = 0x0000FFFFFFFFFFFFUL;

        /// <summary>
        /// 默认标记，适用于未显式指定配置的池。
        /// </summary>
        public static PoolTag Default => new PoolTag(0);

        /// <summary>
        /// 64 位原始值。
        /// </summary>
        public ulong Value { get; }

        private PoolTag(ulong value)
        {
            Value = value;
        }

        /// <summary>
        /// 使用类型和可选配置键创建标记。
        /// </summary>
        public static PoolTag Create<T>(string configKey = "default") where T : class
        {
            var typeHash = (ushort)(typeof(T).GetHashCode() & 0xFFFF);
            var configHash = unchecked((ulong)(configKey?.GetHashCode() ?? 0)) & ConfigMask;
            var packed = ((ulong)typeHash << (64 - TypeBits)) | configHash;
            return new PoolTag(packed);
        }

        /// <summary>
        /// 使用自定义组件创建标记。
        /// </summary>
        public static PoolTag Compose(ushort typeDiscriminator, ulong configDiscriminator)
        {
            var packed = ((ulong)typeDiscriminator << (64 - TypeBits)) | (configDiscriminator & ConfigMask);
            return new PoolTag(packed);
        }

        /// <summary>
        /// 从现有值恢复标记。
        /// </summary>
        public static PoolTag FromValue(ulong value) => new PoolTag(value);

        /// <summary>
        /// 获取类型部分的哈希值。
        /// </summary>
        public ushort GetTypeDiscriminator() => (ushort)(Value >> (64 - TypeBits));

        /// <summary>
        /// 获取配置部分的哈希值。
        /// </summary>
        public ulong GetConfigDiscriminator() => Value & ConfigMask;

        public bool Equals(PoolTag other) => Value == other.Value;

        public override bool Equals(object obj) => obj is PoolTag other && Equals(other);

        public override int GetHashCode() => Value.GetHashCode();

        public int CompareTo(PoolTag other) => Value.CompareTo(other.Value);

        public override string ToString()
        {
            return $"PoolTag(Type:0x{GetTypeDiscriminator():X4}, Config:0x{GetConfigDiscriminator():X12})";
        }

        public static bool operator ==(PoolTag left, PoolTag right) => left.Equals(right);
        public static bool operator !=(PoolTag left, PoolTag right) => !left.Equals(right);
        public static bool operator <(PoolTag left, PoolTag right) => left.CompareTo(right) < 0;
        public static bool operator >(PoolTag left, PoolTag right) => left.CompareTo(right) > 0;
    }
}

