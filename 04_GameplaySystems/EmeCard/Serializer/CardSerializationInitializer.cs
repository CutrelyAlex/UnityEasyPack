using UnityEngine;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    /// EmeCard 序列化系统初始化器
    /// </summary>
    public static class CardSerializationInitializer
    {
        private static bool _isInitialized = false;
        private static readonly object _lock = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            lock (_lock)
            {
                if (_isInitialized)
                {
                    Debug.LogWarning("[EmeCard] Card 序列化系统已初始化，跳过重复注册");
                    return;
                }

                try
                {
                    SerializationServiceManager.RegisterSerializer(new CardJsonSerializer());
                    _isInitialized = true;
                    Debug.Log("[EmeCard] Card 序列化系统初始化完成");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[EmeCard] Card 序列化系统初始化失败: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        internal static bool IsInitialized => _isInitialized;

        internal static void ResetForTesting()
        {
            lock (_lock)
            {
                _isInitialized = false;
            }
        }
    }
}

