namespace EasyPack.EmeCardSystem
{
    public readonly struct CardEvent
    {
        public CardEventType Type { get; }
        public string ID { get; }
        public object Data { get; }

        public CardEvent(CardEventType type, string id = "Default", object data = null)
        {
            Type = type;
            ID = id;
            Data = data;
        }
    }

}
