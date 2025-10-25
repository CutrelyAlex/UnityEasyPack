namespace EasyPack.EmeCardSystem
{
    public enum CardEventType
    {
        /// <summary>
        /// 向子卡分发
        /// </summary>
        AddedToOwner,
        /// <summary>
        /// 向子卡分发
        /// </summary>
        RemovedFromOwner,
        Tick,           // 按时
        Use,            // 主动使用
        Custom
    }
    public readonly struct CardEvent
    {
        public CardEventType Type { get; }
        public string ID { get; }
        public object Data { get; }

        public CardEvent(CardEventType type, string id = null, object data = null)
        {
            Type = type;
            ID = id;
            Data = data;
        }
    }

}
