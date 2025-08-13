namespace EasyPack
{
    public enum CustomDataType
    {
        None = 0,
        Int,
        Float,
        Bool,
        String,
        Vector2,
        Vector3,
        Color,
        Json,   // 使用 JsonValue 储存的可序列化对象
        Custom  // 使用自定义序列化器
    }
}