using System;
using System.Collections.Generic;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    /// 全局Tag注册表，将字符串Tag映射到int位标志
    /// </summary>
    public static class TagRegistry
    {
        private static readonly Dictionary<string, int> _tagToId = new(StringComparer.Ordinal);
        private static readonly Dictionary<int, string> _idToTag = new();
        private static int _nextId = 0;
        private static readonly object _lock = new object();

        /// <summary>
        /// 注册一个Tag并分配位标志，如果已存在则返回现有标志
        /// </summary>
        /// <param name="tag">Tag字符串</param>
        /// <returns>唯一的Tag ID，如果注册失败返回-1</returns>
        public static int RegisterTag(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return 0;

            lock (_lock)
            {
                if (_tagToId.TryGetValue(tag, out var existingId))
                {
                    return existingId;
                }

                int id = _nextId++;
                _tagToId[tag] = id;
                _idToTag[id] = tag;
                return id;
            }
        }

        /// <summary>
        /// 获取Tag对应的ID，如果不存在则自动注册
        /// </summary>
        public static int GetTagId(string tag)
        {
            if (string.IsNullOrEmpty(tag)) return 0;

            if (_tagToId.TryGetValue(tag, out var id))
            {
                return id;
            }

            return RegisterTag(tag);
        }

        /// <summary>
        /// 批量获取Tag ID
        /// </summary>
        public static IEnumerable<int> GetTagIds(IEnumerable<string> tags)
        {
            if (tags == null) yield break;
            foreach (var tag in tags)
            {
                var id = GetTagId(tag);
                if (id >= 0)
                {
                    yield return id;
                }
            }
        }

        /// <summary>
        /// 检查当前集合是否包含指定Tag
        /// </summary>
        public static bool HasTag(HashSet<int> tagIds, string tag)
        {
            if (tagIds == null) return false;
            if (TryGetTagId(tag, out var id))
            {
                return tagIds.Contains(id);
            }
            return false;
        }

        /// <summary>
        /// 添加Tag ID
        /// </summary>
        public static void AddTag(HashSet<int> tagIds, string tag)
        {
            if (tagIds == null) return;
            var id = GetTagId(tag);
            if (id >= 0)
            {
                tagIds.Add(id);
            }
        }

        /// <summary>
        /// 从集合移除Tag
        /// </summary>
        public static void RemoveTag(HashSet<int> tagIds, string tag)
        {
            if (tagIds == null) return;
            if (TryGetTagId(tag, out var id))
            {
                tagIds.Remove(id);
            }
        }

        /// <summary>
        /// 获取整数ID集合中的Tag字符串（调试用）
        /// </summary>
        public static List<string> GetTagsFromIds(IEnumerable<int> tagIds)
        {
            var tags = new List<string>();
            if (tagIds == null) return tags;
            foreach (var id in tagIds)
            {
                if (_idToTag.TryGetValue(id, out var tag))
                {
                    tags.Add(tag);
                }
            }
            return tags;
        }

        /// <summary>
        /// 获取当前已注册的Tag数量
        /// </summary>
        public static int RegisteredTagCount => _nextId;

        /// <summary>
        /// 清空所有注册
        /// </summary>
        public static void Clear()
        {
            lock (_lock)
            {
                _tagToId.Clear();
                _idToTag.Clear();
                _nextId = 0;
            }
        }

        private static bool TryGetTagId(string tag, out int id)
        {
            if (string.IsNullOrEmpty(tag))
            {
                id = -1;
                return false;
            }

            return _tagToId.TryGetValue(tag, out id);
        }
    }
}
