using System.Collections.Generic;
using EasyPack.EmeCardSystem;

namespace EasyPack
{
    /// <summary>
    /// List&lt;Card&gt; 专用对象池，用于 EmeCard 系统中的临时卡牌列表。
    /// </summary>
    public static class CardListPool
    {
        private static ObjectPool<List<Card>> _pool;

        /// <summary>
        /// 初始化卡牌列表池。
        /// </summary>
        /// <param name="poolService">对象池服务实例。</param>
        /// <param name="maxCapacity">池的最大容量，默认为64。</param>
        public static void Initialize(IObjectPoolService poolService, int maxCapacity = 64)
        {
            _pool = poolService.CreatePool(
                factory: () => new List<Card>(),
                cleanup: list => list.Clear(),
                maxCapacity: maxCapacity
            );
        }

        /// <summary>
        /// 从池中租用一个卡牌列表。
        /// </summary>
        /// <returns>清洁的卡牌列表实例。</returns>
        public static List<Card> Rent()
        {
            if (_pool == null)
            {
                throw new System.InvalidOperationException("CardListPool 未初始化，请先调用 Initialize");
            }
            return _pool.Rent();
        }

        /// <summary>
        /// 将卡牌列表归还到池中。列表将自动清空。
        /// </summary>
        /// <param name="list">要归还的卡牌列表。</param>
        public static void Return(List<Card> list)
        {
            if (_pool == null)
            {
                return;
            }
            _pool.Return(list);
        }

        /// <summary>
        /// 获取池的统计信息。
        /// </summary>
        public static PoolStatistics GetStatistics()
        {
            return _pool?.GetStatistics();
        }
    }
}
