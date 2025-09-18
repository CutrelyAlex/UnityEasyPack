namespace EasyPack
{
    public enum CardEventType
    {
        AddedToOwner,
        RemovedFromOwner,
        Tick,           // 按时
        Use,            // 主动使用
        Custom,
        Condition
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