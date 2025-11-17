using System;
using System.Collections.Generic;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    /// 全局Tag注册表，将字符串Tag映射到int位标志
    /// </summary>
    public static class TagRegistry
    {
        private static readonly Dictionary<string, ulong> _tagToMask = new(StringComparer.Ordinal);
        private static readonly Dictionary<ulong, string> _maskToTag = new();
        private static int _nextBit = 0;
        private static readonly object _lock = new object();

        /// <summary>
        /// 注册一个Tag并分配位标志，如果已存在则返回现有标志
        /// </summary>
        /// <param name="tag">Tag字符串</param>
        /// <returns>分配的位掩码，如果Tag数量超过64则返回0</returns>
        public static ulong RegisterTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return 0;

            lock (_lock)
            {
                if (_tagToMask.TryGetValue(tag, out var existingMask))
                {
                    return existingMask;
                }

                if (_nextBit >= 64)
                {
                    UnityEngine.Debug.LogWarning($"[TagRegistry] Tag数量已达到上限64个，无法注册新Tag: {tag}");
                    return 0;
                }

                ulong mask = 1ul << _nextBit;
                _tagToMask[tag] = mask;
                _maskToTag[mask] = tag;
                _nextBit++;

                return mask;
            }
        }

        /// <summary>
        /// 获取Tag对应的位掩码，如果不存在则自动注册
        /// </summary>
        public static ulong GetTagMask(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return 0;

            if (_tagToMask.TryGetValue(tag, out var mask))
            {
                return mask;
            }

            return RegisterTag(tag);
        }

        /// <summary>
        /// 批量获取Tag掩码的组合
        /// </summary>
        public static ulong GetCombinedMask(IEnumerable<string> tags)
        {
            ulong combined = 0;
            foreach (var tag in tags)
            {
                combined |= GetTagMask(tag);
            }
            return combined;
        }

        /// <summary>
        /// 检查掩码中是否包含指定Tag
        /// </summary>
        public static bool HasTag(ulong mask, string tag)
        {
            var tagMask = GetTagMask(tag);
            return tagMask != 0 && (mask & tagMask) != 0;
        }

        /// <summary>
        /// 添加Tag到掩码
        /// </summary>
        public static ulong AddTag(ulong mask, string tag)
        {
            return mask | GetTagMask(tag);
        }

        /// <summary>
        /// 从掩码移除Tag
        /// </summary>
        public static ulong RemoveTag(ulong mask, string tag)
        {
            var tagMask = GetTagMask(tag);
            if (tagMask == 0) return mask;
            return mask & ~tagMask;
        }

        /// <summary>
        /// 获取掩码中所有Tag的字符串列表（调试用）
        /// </summary>
        public static List<string> GetTagsFromMask(ulong mask)
        {
            var tags = new List<string>();
            foreach (var kvp in _maskToTag)
            {
                if ((mask & kvp.Key) != 0)
                {
                    tags.Add(kvp.Value);
                }
            }
            return tags;
        }

        /// <summary>
        /// 获取当前已注册的Tag数量
        /// </summary>
        public static int RegisteredTagCount => _nextBit;

        /// <summary>
        /// 清空所有注册
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _tagToMask.Clear();
                _maskToTag.Clear();
                _nextBit = 0;
            }
        }
    }
}
