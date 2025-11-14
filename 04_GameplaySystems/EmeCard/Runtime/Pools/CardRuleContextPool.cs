using EasyPack.EmeCardSystem;

namespace EasyPack
{
    /// <summary>
    /// CardRuleContext 专用对象池，用于 EmeCard 系统中的规则上下文。
    /// </summary>
    public static class CardRuleContextPool
    {
        private static ObjectPool<CardRuleContext> _pool;

        /// <summary>
        /// 初始化规则上下文池。
        /// </summary>
        /// <param name="poolService">对象池服务实例。</param>
        /// <param name="maxCapacity">池的最大容量，默认为64。</param>
        public static void Initialize(IObjectPoolService poolService, int maxCapacity = 64)
        {
            _pool = poolService.CreatePool<CardRuleContext>(
                factory: () => null, // CardRuleContext 通过外部创建
                cleanup: null, // CardRuleContext 是不可变对象，无需清理
                maxCapacity: maxCapacity
            );
        }

        /// <summary>
        /// 从池中租用一个规则上下文。
        /// </summary>
        /// <returns>规则上下文实例（可能为 null，需要手动创建）。</returns>
        public static CardRuleContext Rent()
        {
            if (_pool == null)
            {
                throw new System.InvalidOperationException("CardRuleContextPool 未初始化，请先调用 Initialize");
            }
            return _pool.Rent();
        }

        /// <summary>
        /// 将规则上下文归还到池中。
        /// </summary>
        /// <param name="context">要归还的规则上下文。</param>
        public static void Return(CardRuleContext context)
        {
            if (_pool == null)
            {
                return;
            }
            _pool.Return(context);
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
