using NUnit.Framework;
using System.Collections.Generic;
using EasyPack.ENekoFramework;

namespace EasyPack.ENekoFrameworkTest.Unit
{
    /// <summary>
    /// 测试ReactiveCollection的响应式集合功能。
    /// 验证Add/Remove/Clear事件和集合同步是否正确工作。
    /// </summary>
    [TestFixture]
    public class BindableCollectionTests
    {
        private BindableCollection<string> _collection;
        private List<string> _addedItems;
        private List<string> _removedItems;
        private int _clearCount;

        [SetUp]
        public void Setup()
        {
            _collection = new BindableCollection<string>();
            _addedItems = new List<string>();
            _removedItems = new List<string>();
            _clearCount = 0;

            _collection.OnItemAdded += item => _addedItems.Add(item);
            _collection.OnItemRemoved += item => _removedItems.Add(item);
            _collection.OnCollectionCleared += () => _clearCount++;
        }

        [TearDown]
        public void TearDown()
        {
            _collection = null;
            _addedItems = null;
            _removedItems = null;
        }

        [Test]
        public void Add_SingleItem_TriggersOnItemAdded()
        {
            // Act
            _collection.Add("Item1");

            // Assert
            Assert.AreEqual(1, _addedItems.Count, "应该触发一次OnItemAdded事件");
            Assert.AreEqual("Item1", _addedItems[0], "添加的项应该是Item1");
            Assert.AreEqual(1, _collection.Count, "集合应该包含1个元素");
        }

        [Test]
        public void Add_MultipleItems_TriggersMultipleEvents()
        {
            // Act
            _collection.Add("Item1");
            _collection.Add("Item2");
            _collection.Add("Item3");

            // Assert
            Assert.AreEqual(3, _addedItems.Count, "应该触发3次OnItemAdded事件");
            Assert.AreEqual(3, _collection.Count, "集合应该包含3个元素");
            CollectionAssert.AreEqual(new[] { "Item1", "Item2", "Item3" }, _addedItems);
        }

        [Test]
        public void Remove_ExistingItem_TriggersOnItemRemoved()
        {
            // Arrange
            _collection.Add("Item1");
            _collection.Add("Item2");
            _addedItems.Clear();

            // Act
            bool removed = _collection.Remove("Item1");

            // Assert
            Assert.IsTrue(removed, "Remove应该返回true");
            Assert.AreEqual(1, _removedItems.Count, "应该触发一次OnItemRemoved事件");
            Assert.AreEqual("Item1", _removedItems[0], "移除的项应该是Item1");
            Assert.AreEqual(1, _collection.Count, "集合应该剩余1个元素");
        }

        [Test]
        public void Remove_NonExistingItem_NoEvent()
        {
            // Arrange
            _collection.Add("Item1");

            // Act
            bool removed = _collection.Remove("NonExisting");

            // Assert
            Assert.IsFalse(removed, "Remove应该返回false");
            Assert.AreEqual(0, _removedItems.Count, "不应该触发OnItemRemoved事件");
        }

        [Test]
        public void Clear_NonEmptyCollection_TriggersOnCollectionCleared()
        {
            // Arrange
            _collection.Add("Item1");
            _collection.Add("Item2");
            _collection.Add("Item3");

            // Act
            _collection.Clear();

            // Assert
            Assert.AreEqual(1, _clearCount, "应该触发一次OnCollectionCleared事件");
            Assert.AreEqual(0, _collection.Count, "集合应该为空");
        }

        [Test]
        public void Clear_EmptyCollection_StillTriggersEvent()
        {
            // Act
            _collection.Clear();

            // Assert
            Assert.AreEqual(1, _clearCount, "即使集合为空也应触发OnCollectionCleared事件");
        }

        [Test]
        public void Indexer_Get_ReturnsCorrectItem()
        {
            // Arrange
            _collection.Add("Item1");
            _collection.Add("Item2");

            // Act & Assert
            Assert.AreEqual("Item1", _collection[0]);
            Assert.AreEqual("Item2", _collection[1]);
        }

        [Test]
        public void Indexer_Set_TriggersRemovedAndAdded()
        {
            // Arrange
            _collection.Add("OldItem");
            _addedItems.Clear();

            // Act
            _collection[0] = "NewItem";

            // Assert
            Assert.AreEqual(1, _removedItems.Count, "应该触发一次OnItemRemoved");
            Assert.AreEqual("OldItem", _removedItems[0]);
            Assert.AreEqual(1, _addedItems.Count, "应该触发一次OnItemAdded");
            Assert.AreEqual("NewItem", _addedItems[0]);
            Assert.AreEqual("NewItem", _collection[0]);
        }

        [Test]
        public void Contains_ExistingItem_ReturnsTrue()
        {
            // Arrange
            _collection.Add("Item1");

            // Act & Assert
            Assert.IsTrue(_collection.Contains("Item1"));
        }

        [Test]
        public void Contains_NonExistingItem_ReturnsFalse()
        {
            // Arrange
            _collection.Add("Item1");

            // Act & Assert
            Assert.IsFalse(_collection.Contains("Item2"));
        }

        [Test]
        public void GetEnumerator_IteratesAllItems()
        {
            // Arrange
            _collection.Add("Item1");
            _collection.Add("Item2");
            _collection.Add("Item3");

            // Act
            var items = new List<string>();
            foreach (var item in _collection)
            {
                items.Add(item);
            }

            // Assert
            CollectionAssert.AreEqual(new[] { "Item1", "Item2", "Item3" }, items);
        }

        [Test]
        public void MultipleListeners_AllReceiveEvents()
        {
            // Arrange
            var listener1Added = new List<string>();
            var listener2Added = new List<string>();

            _collection.OnItemAdded += item => listener1Added.Add(item);
            _collection.OnItemAdded += item => listener2Added.Add(item);

            // Act
            _collection.Add("Item1");

            // Assert
            Assert.AreEqual(1, listener1Added.Count, "监听器1应该收到事件");
            Assert.AreEqual(1, listener2Added.Count, "监听器2应该收到事件");
        }
    }
}
