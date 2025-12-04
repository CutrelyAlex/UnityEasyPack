using System;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     EmeCard专用事件队列
    /// </summary>
    internal sealed class EventQueue
    {
        // 平行数组存储，避免装箱
        private Card[] _sources;
        private ICardEvent[] _events;
        
        private int _head;      // 出队位置
        private int _tail;      // 入队位置

        private const int DefaultCapacity = 256;
        private const int MaxCapacity = 8192;

        public EventQueue(int initialCapacity = DefaultCapacity)
        {
            int capacity = Math.Max(initialCapacity, 16);
            _sources = new Card[capacity];
            _events = new ICardEvent[capacity];
            _head = 0;
            _tail = 0;
            Count = 0;
        }

        /// <summary>当前队列中的元素数量</summary>
        public int Count { get; private set; }

        /// <summary>
        ///     将事件入队。
        /// </summary>
        public void Enqueue(Card source, ICardEvent evt)
        {
            // 确保容量
            if (Count == _sources.Length)
            {
                Grow();
            }

            _sources[_tail] = source;
            _events[_tail] = evt;
            
            _tail = (_tail + 1) % _sources.Length;
            Count++;
        }

        /// <summary>
        ///     尝试出队。
        /// </summary>
        public bool TryDequeue(out Card source, out ICardEvent evt)
        {
            if (Count == 0)
            {
                source = null;
                evt = null;
                return false;
            }

            source = _sources[_head];
            evt = _events[_head];
            
            // 清除引用，允许 GC
            _sources[_head] = null;
            _events[_head] = null;
            
            _head = (_head + 1) % _sources.Length;
            Count--;
            
            return true;
        }

        /// <summary>
        ///     查看队首元素但不移除。
        /// </summary>
        public bool TryPeek(out Card source, out ICardEvent evt)
        {
            if (Count == 0)
            {
                source = null;
                evt = null;
                return false;
            }

            source = _sources[_head];
            evt = _events[_head];
            return true;
        }

        /// <summary>
        ///     清空队列。
        /// </summary>
        public void Clear()
        {
            // 清除引用
            if (Count > 0)
            {
                if (_head < _tail)
                {
                    Array.Clear(_sources, _head, Count);
                    Array.Clear(_events, _head, Count);
                }
                else
                {
                    Array.Clear(_sources, _head, _sources.Length - _head);
                    Array.Clear(_sources, 0, _tail);
                    Array.Clear(_events, _head, _events.Length - _head);
                    Array.Clear(_events, 0, _tail);
                }
            }
            
            _head = 0;
            _tail = 0;
            Count = 0;
        }

        /// <summary>
        ///     扩展容量。
        /// </summary>
        private void Grow()
        {
            int newCapacity = Math.Min(_sources.Length * 2, MaxCapacity);
            if (newCapacity == _sources.Length)
            {
                throw new InvalidOperationException($"EventQueue 已达到最大容量 {MaxCapacity}");
            }

            var newSources = new Card[newCapacity];
            var newEvents = new ICardEvent[newCapacity];

            // 复制元素到新数组
            if (_head < _tail)
            {
                Array.Copy(_sources, _head, newSources, 0, Count);
                Array.Copy(_events, _head, newEvents, 0, Count);
            }
            else if (Count > 0)
            {
                int headCount = _sources.Length - _head;
                Array.Copy(_sources, _head, newSources, 0, headCount);
                Array.Copy(_sources, 0, newSources, headCount, _tail);
                Array.Copy(_events, _head, newEvents, 0, headCount);
                Array.Copy(_events, 0, newEvents, headCount, _tail);
            }

            _sources = newSources;
            _events = newEvents;
            _head = 0;
            _tail = Count;
        }
    }
}
