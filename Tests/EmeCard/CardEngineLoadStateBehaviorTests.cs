using EasyPack.CustomData;
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

        [Test]
        public void LoadState_PreservesRootPositionIndex_WhenChildSharesRootPosition()
        {
            CardFactory sourceFactory = CreateFactory();
            var sourceEngine = new CardEngine(sourceFactory);

            Card root = sourceEngine.CreateCard("root");
            root.Position = new Vector3Int(4, 0, 7);
            Card child = sourceEngine.CreateCard("child");
            root.AddChild(child);

            string json = sourceEngine.SerializeToJson();

            var restoredEngine = new CardEngine(CreateFactory());
            restoredEngine.DeserializeFromJson(json);

            Card restoredRoot = restoredEngine.GetCardByUID(root.UID);
            Card restoredChild = restoredEngine.GetCardByUID(child.UID);

            Assert.IsNotNull(restoredRoot, "应恢复根卡");
            Assert.IsNotNull(restoredChild, "应恢复子卡");
            Assert.AreEqual(restoredRoot, restoredEngine.GetCardByPosition(root.Position.Value),
                "位置索引应继续指向根卡，而不是在恢复过程中被子卡覆盖后丢失");
            Assert.IsNull(restoredEngine.GetPositionByUID(restoredChild.UID), "子卡不应持有独立位置索引");
            Assert.AreEqual(root.Position, restoredChild.Position, "子卡逻辑位置应继续继承根卡位置");
        }

        [Test]
        public void LoadState_RestoresRuntimeMetadata()
        {
            CardFactory sourceFactory = CreateFactory();
            var sourceEngine = new CardEngine(sourceFactory);

            Card root = sourceEngine.CreateCard("root");
            Card child = sourceEngine.CreateCard("child");
            root.AddChild(child);

            var rootMetadata = new CustomDataCollection();
            rootMetadata.Set("Stack", 3);
            sourceEngine.CategoryManager.UpdateMetadata(root.UID, rootMetadata);

            var childMetadata = new CustomDataCollection();
            childMetadata.Set("Heat", 12);
            sourceEngine.CategoryManager.UpdateMetadata(child.UID, childMetadata);

            string json = sourceEngine.SerializeToJson();

            var restoredEngine = new CardEngine(CreateFactory());
            restoredEngine.DeserializeFromJson(json);

            Card restoredRoot = restoredEngine.GetCardByUID(root.UID);
            Card restoredChild = restoredEngine.GetCardByUID(child.UID);

            Assert.IsNotNull(restoredRoot, "应恢复根卡");
            Assert.IsNotNull(restoredChild, "应恢复子卡");
            Assert.AreEqual(3, restoredEngine.CategoryManager.GetMetadata(restoredRoot.UID).Get<int>("Stack"),
                "根卡运行时 metadata 应恢复");
            Assert.AreEqual(12, restoredEngine.CategoryManager.GetMetadata(restoredChild.UID).Get<int>("Heat"),
                "子卡运行时 metadata 应恢复");
        }

        [Test]
        public void ClearAllCards_UnsubscribesOldCardsFromEngine()
        {
            var engine = new CardEngine(CreateFactory());
            Card card = engine.CreateCard("root");

            bool triggered = false;
            engine.RegisterRule(builder => builder
                .On("Clear_Test")
                .When(_ =>
                {
                    triggered = true;
                    return true;
                }));

            engine.ClearAllCards();
            card.RaiseEvent("Clear_Test");

            Assert.IsFalse(triggered, "ClearAllCards 后旧卡牌事件不应再回流到原引擎");
            Assert.IsNull(card.Engine, "ClearAllCards 后旧卡牌应解除对原引擎的引用");
        }
    }
}