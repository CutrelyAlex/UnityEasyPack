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

}
