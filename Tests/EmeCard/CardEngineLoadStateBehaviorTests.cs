using EasyPack.EmeCardSystem;
using NUnit.Framework;
using UnityEngine;

namespace EasyPack.EmeCardTests
{
    [TestFixture]
    public class CardEngineLoadStateBehaviorTests
    {
        private static CardFactory CreateFactory()
        {
            var factory = new CardFactory();
            factory.Register("root", () => new Card(new CardData("root", "Root", category: "Card.Object")));
            factory.Register("child", () => new Card(new CardData("child", "Child", category: "Card.Object")));
            return factory;
        }

        [Test]
        public void LoadState_DoesNotTriggerAddedToOwnerRules()
        {
            CardFactory sourceFactory = CreateFactory();
            var sourceEngine = new CardEngine(sourceFactory);

            Card root = sourceEngine.CreateCard("root");
            root.Position = new Vector3Int(2, 0, 1);
            Card child = sourceEngine.CreateCard("child");
            root.AddChild(child);

            string json = sourceEngine.SerializeToJson();

            CardFactory restoreFactory = CreateFactory();
            var restoredEngine = new CardEngine(restoreFactory);

            bool triggered = false;
            restoredEngine.RegisterRule(builder => builder
                .OnAddedToOwner()
                .When(_ =>
                {
                    triggered = true;
                    return true;
                }));

            restoredEngine.DeserializeFromJson(json);

            Assert.IsFalse(triggered, "LoadState 重建父子关系时不应真实触发 AddedToOwner 规则");
        }
    }
}