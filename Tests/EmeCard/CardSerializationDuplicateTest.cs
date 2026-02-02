using System.Collections.Generic;
using System.Linq;
using EasyPack.Architecture;
using EasyPack.Category;
using EasyPack.EmeCardSystem;
using EasyPack.Serialization;
using NUnit.Framework;
using UnityEngine;

namespace EasyPack.EmeCardTests
{
    [TestFixture]
    public class CardSerializationDuplicateTest
    {
        private ISerializationService _serializationService;
        private CardEngine _engine;
        private CardFactory _factory;

        [SetUp]
        public void Setup()
        {
            _serializationService = EasyPackArchitecture.Instance.ResolveAsync<ISerializationService>().GetAwaiter().GetResult();
            _factory = new CardFactory();
            _engine = new CardEngine(_factory);
        }

        [TearDown]
        public void TearDown()
        {
            _engine = null;
            _factory = null;
        }

        [Test]
        public void Test_DuplicateSerialization_Consistency()
        {
            // 1. Setup: Player with a Child (Attribute)
            var player = new Card(new CardData("player", "Player"));
            var attribute = new Card(new CardData("attr", "Attribute"));
            
            // Add child first
            player.AddChild(attribute);
            
            // Register to engine. This should register both Player and Attribute (recursively).
            _engine.AddCard(player);
            
            // Verify setup
            Assert.IsNotNull(player.Engine);
            Assert.IsNotNull(attribute.Engine);
            Assert.AreEqual(player, attribute.Owner);
            Assert.AreEqual(attribute, _engine.GetCardByUID(attribute.UID));
            
            // 2. Serialize
            var dto = _engine.GetSerializableState();
            string json = JsonUtility.ToJson(dto);
            
            // 3. Deserialize into a NEW engine to simulate loading
            var newFactory = new CardFactory();
            var newEngine = new CardEngine(newFactory);
            var newDto = JsonUtility.FromJson<CardEngineDTO>(json);
            
            newEngine.LoadState(newDto);
            
            // 4. Verify Consistency
            
            // Find the attribute card by UID
            long attrUid = attribute.UID;
            var loadedAttribute = newEngine.GetCardByUID(attrUid);
            
            Assert.IsNotNull(loadedAttribute, "Attribute card should be loaded");
            
            // Find the player card
            long playerUid = player.UID;
            var loadedPlayer = newEngine.GetCardByUID(playerUid);
            
            Assert.IsNotNull(loadedPlayer, "Player card should be loaded");
            
            // Check if loadedPlayer has the child
            Assert.AreEqual(1, loadedPlayer.Children.Count);
            var childOfPlayer = loadedPlayer.Children[0];
            
            Assert.AreEqual(attrUid, childOfPlayer.UID, "Child of player should have same UID as attribute");
            
            // THE BUG: loadedAttribute (from _cardsByUID) might be a different instance than childOfPlayer
            // and loadedAttribute might have Owner == null
            
            Assert.AreEqual(loadedPlayer, childOfPlayer.Owner, "Child of player should have player as owner");
            
            // This assertion fails if the bug exists:
            Assert.AreSame(childOfPlayer, loadedAttribute, "Card in registry and card in hierarchy should be the SAME instance");
            
            Assert.AreEqual(loadedPlayer, loadedAttribute.Owner, "Card in registry should have correct owner");
        }
    }
}
