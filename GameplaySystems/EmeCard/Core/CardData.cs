using EasyPack;

public sealed class CardData
{
    public string ID { get; }
    public string Name { get; }
    public string Description { get; }
    public CardCategory Category { get; }
    public string[] DefaultTags { get; }

    public CardData(string id, string name = "Default", string desc = "",
                    CardCategory category = CardCategory.Item, string[] defaultTags = null)
    {
        ID = id;
        Name = name;
        Description = desc;
        Category = category;
        DefaultTags = defaultTags ?? System.Array.Empty<string>();
    }
}