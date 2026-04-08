using EasyPack.CustomData;
using EasyPack.EmeCardSystem;
using EasyPack.GamePropertySystem;
using NUnit.Framework;

namespace EasyPack.EmeCardTests
{
    [TestFixture]
    public class CardCopyTest
    {
        private CardFactory _factory;
        private CardEngine _engine;

        [SetUp]
        public void Setup()
        {
            _factory = new CardFactory();
            _factory.RegisterData("parent", new CardData("parent", "Parent", category: "Card.Object"));
            _factory.RegisterData("child", new CardData("child", "Child", category: "Card.Attribute"));
            _engine = new CardEngine(_factory);
        }

        [TearDown]
        public void TearDown()
        {
            _engine = null;
            _factory = null;
        }

        [Test]
        public void CopyCard_ClonesTreePropertiesAndMetadata_WithNewUIDAndIndex()
        {
            Card source = new Card("parent");
            source.Properties.Add(new GameProperty("HP", 10).AddModifier(new EasyPack.Modifiers.FloatModifier(EasyPack.Modifiers.ModifierType.Add, 1, 5f)) as GameProperty);

            Card child = new Card("child");
            child.Properties.Add(new GameProperty("Damage", 3));
            source.AddChild(child, true);

            _engine.AddCard(source);
            source.ModifyRuntimeMetadata(meta =>
            {
                meta.Set("Level", 7);
                meta.Set("Name", "source-root");
            });
            child.ModifyRuntimeMetadata(meta => meta.Set("Kind", "child-meta"));

            Card copy = _engine.CopyCard(source);

            Assert.IsNotNull(copy);
            Assert.AreEqual(source.Id, copy.Id);
            Assert.AreNotEqual(source.UID, copy.UID);
            Assert.AreNotEqual(source.Index, copy.Index);
            Assert.AreEqual(1, copy.Children.Count);

            Card copiedChild = copy.Children[0];
            Assert.AreEqual(child.Id, copiedChild.Id);
            Assert.AreNotEqual(child.UID, copiedChild.UID);
            Assert.AreNotEqual(child.Index, copiedChild.Index);
            Assert.IsTrue(copy.IsIntrinsic(copiedChild));
            Assert.AreSame(copy, copiedChild.Owner);

            Assert.AreEqual(source.GetProperty("HP").GetValue(), copy.GetProperty("HP").GetValue());
            Assert.AreEqual(child.GetProperty("Damage").GetValue(), copiedChild.GetProperty("Damage").GetValue());
            Assert.AreNotSame(source.GetProperty("HP"), copy.GetProperty("HP"));

            CustomDataCollection copyMeta = _engine.CategoryManager.GetMetadata(copy.UID);
            CustomDataCollection copyChildMeta = _engine.CategoryManager.GetMetadata(copiedChild.UID);
            Assert.IsNotNull(copyMeta);
            Assert.IsNotNull(copyChildMeta);
            Assert.AreEqual(7, copyMeta.Get<int>("Level"));
            Assert.AreEqual("source-root", copyMeta.Get<string>("Name"));
            Assert.AreEqual("child-meta", copyChildMeta.Get<string>("Kind"));

            copy.GetProperty("HP").SetBaseValue(999);
            copy.ModifyRuntimeMetadata(meta => meta.Set("Level", 100));

            Assert.AreEqual(15f, source.GetProperty("HP").GetValue());
            Assert.AreEqual(7, _engine.CategoryManager.GetMetadata(source.UID).Get<int>("Level"));
        }
    }
}