using System;
using System.Collections.Generic;
using System.Threading;

namespace EasyPack.Category
{
    /// <summary>
    ///     双向字符串↔整数映射器，用于将词汇（标签、分类）映射为整数 ID。
    ///     使用 ReaderWriterLockSlim
    /// </summary>
    public class IntegerMapper
    {
        /// <summary>
        ///     字符串 → 整数 ID 的映射表
        /// </summary>
        private readonly Dictionary<string, int> _stringToInt;

        /// <summary>
        ///     整数 ID → 字符串的反向映射表
        /// </summary>
        private readonly Dictionary<int, string> _intToString;

        /// <summary>
        ///     下一个待分配的 ID
        /// </summary>
        private int _nextId;

        /// <summary>
        ///     读写锁
        /// </summary>
        private readonly ReaderWriterLockSlim _lock;

        /// <summary>
        ///     初始化映射器
        /// </summary>
        /// <param name="initialCapacity">初始容量</param>
        public IntegerMapper(int initialCapacity = CategoryConstants.DEFAULT_MAPPER_CAPACITY)
        {
            _stringToInt = new(initialCapacity);
            _intToString = new(initialCapacity);
            _nextId = 0;
            _lock = new(LockRecursionPolicy.NoRecursion);
        }

        /// <summary>
        ///     <para>获取或分配 ID </para>
        ///     首次查询不获取锁,未找到时获取写锁并二次检查
        /// </summary>
        /// <param name="term">词汇字符串</param>
        /// <returns>分配的整数 ID</returns>
        /// <exception cref="OverflowException">当 ID 超过上限时抛出</exception>
        public int GetOrAssignId(string term)
        {
            if (string.IsNullOrEmpty(term))
                throw new ArgumentException("Term cannot be null or empty.", nameof(term));

            // 快路径：尝试读取，避免锁竞争
            _lock.EnterReadLock();
            try
            {
                if (_stringToInt.TryGetValue(term, out int existingId))
                    return existingId;
            }
            finally
            {
                _lock.ExitReadLock();
            }

            // 慢路径：获取写锁并分配新 ID
            _lock.EnterWriteLock();
            try
            {
                // 二次检查：防止其他线程已分配
                if (_stringToInt.TryGetValue(term, out int existingId))
                    return existingId;

                // 检查 ID 溢出
                if (_nextId >= CategoryConstants.ID_OVERFLOW_THRESHOLD)
                    throw new OverflowException(
                        $"Term ID assignment overflow. Current: {_nextId}, Threshold: {CategoryConstants.ID_OVERFLOW_THRESHOLD}");

                // 分配新 ID
                int newId = _nextId++;
                _stringToInt[term] = newId;
                _intToString[newId] = term;

                return newId;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        ///     尝试将 ID 转换回字符串
        /// </summary>
        /// <param name="id">整数 ID</param>
        /// <param name="term">返回的字符串（若失败则为 null）</param>
        /// <returns>是否成功转换</returns>
        public bool TryGetString(int id, out string term)
        {
            _lock.EnterReadLock();
            try
            {
                return _intToString.TryGetValue(id, out term);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        ///     尝试获取词汇对应的整数 ID
        /// </summary>
        /// <param name="term">词汇字符串</param>
        /// <param name="id">返回的整数 ID（若失败则为 -1）</param>
        /// <returns>是否成功获取</returns>
        public bool TryGetId(string term, out int id)
        {
            id = -1;
            if (string.IsNullOrEmpty(term))
                return false;

            _lock.EnterReadLock();
            try
            {
                return _stringToInt.TryGetValue(term, out id);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        ///     检查映射表是否包含指定的 ID
        /// </summary>
        /// <param name="id">整数 ID</param>
        /// <returns>是否存在</returns>
        public bool ContainsId(int id)
        {
            if (id < 0)
                return false;

            _lock.EnterReadLock();
            try
            {
                return _intToString.ContainsKey(id);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        ///     获取映射表中的词汇总数
        /// </summary>
        public int Count
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _stringToInt.Count;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        /// <summary>
        ///     获取下一个将被分配的 ID
        /// </summary>
        public int NextId
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _nextId;
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            }
        }

        /// <summary>
        ///     重命名映射中的词汇
        /// </summary>
        /// <param name="oldTerm">原始词汇字符串</param>
        /// <param name="newTerm">新词汇字符串</param>
        /// <returns>是否成功重命名（返回 false 表示原词汇不存在）</returns>
        public bool RemapTerm(string oldTerm, string newTerm)
        {
            if (string.IsNullOrEmpty(oldTerm) || string.IsNullOrEmpty(newTerm))
                throw new ArgumentException("Terms cannot be null or empty.");

            _lock.EnterWriteLock();
            try
            {
                // 检查原词汇是否存在
                if (!_stringToInt.TryGetValue(oldTerm, out int id))
                    return false;

                // 检查新词汇是否已存在（防止冲突）
                if (_stringToInt.ContainsKey(newTerm))
                    throw new InvalidOperationException($"New term '{newTerm}' already exists in the mapper.");

                // 更新双向映射
                _stringToInt.Remove(oldTerm);
                _stringToInt[newTerm] = id;
                _intToString[id] = newTerm;

                return true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        ///     清空所有映射（需要写锁）
        /// </summary>
        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                _stringToInt.Clear();
                _intToString.Clear();
                _nextId = 0;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        /// <summary>
        ///     获取所有已映射的词汇
        /// </summary>
        public IReadOnlyList<string> GetAllTerms()
        {
            _lock.EnterReadLock();
            try
            {
                return new List<string>(_stringToInt.Keys).AsReadOnly();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        ///     获取所有已映射的 ID 和字符串对
        /// </summary>
        public IReadOnlyDictionary<int, string> GetSnapshot()
        {
            _lock.EnterReadLock();
            try
            {
                return new Dictionary<int, string>(_intToString);
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        ///     释放所有资源
        /// </summary>
        public void Dispose() { _lock?.Dispose(); }
    }
}