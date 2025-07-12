using EasyPack;
using System;
using UnityEngine;

public class InventoryExample : MonoBehaviour
{
    private void Start()
    {
        TestLinerContainer();
    }

    private void TestLinerContainer()
    {
        Debug.Log("====== 开始测试 LinerContainer ======");

        // 测试容器创建
        TestContainerCreation();

        // 测试物品添加
        TestAddingItems();

        // 测试物品堆叠
        TestStackingItems();


        // 测试物品移动
        TestMovingItems();

        // 测试物品移除
        TestRemovingItems();

        // 测试容器排序
        TestContainerSorting();

        Debug.Log("====== LinerContainer 测试完成 ======");
    }

    // 测试容器创建
    private void TestContainerCreation()
    {
        Debug.Log("测试: 容器创建");

        // 创建有限容量的容器
        var container1 = new LinerContainer("container1", "背包", "Bag", 5);
        Assert(container1.ID == "container1", "容器ID应为container1");
        Assert(container1.Name == "背包", "容器名称应为背包");
        Assert(container1.Type == "Bag", "容器类型应为Bag");
        Assert(container1.Capacity == 5, "容器容量应为5");
        Assert(container1.Slots.Count == 5, "容器应有5个槽位");
        Assert(!container1.Full, "新容器不应该是满的");
        Assert(!container1.IsGrid, "线性容器isGrid应为false");

        // 创建无限容量的容器
        var container2 = new LinerContainer("container2", "无限背包", "InfiniteBag", -1);
        Assert(container2.Capacity == -1, "无限容器容量应为-1");
        Assert(container2.Slots.Count == 0, "无限容器初始应有0个槽位");
        Assert(!container2.Full, "无限容器不应该是满的");

        Debug.Log("容器创建测试通过");
    }

    // 测试物品添加
    private void TestAddingItems()
    {
        Debug.Log("测试: 物品添加");

        var container = new LinerContainer("container", "背包", "Bag", 5);

        // 创建测试物品
        var item1 = CreateTestItem("item1", "测试物品1", true, 10);
        var item2 = CreateTestItem("item2", "测试物品2", false);

        // 添加可堆叠物品
        var result1 = container.AddItems(item1, 5);
        Assert(result1.result == AddItemResult.Success, "应成功添加可堆叠物品");
        Assert(result1.addedCount == 5, "应添加5个物品");
        Assert(container.GetItemCount("item1") == 5, "容器中应有5个item1");
        Assert(container.ContainsItem("item1"), "容器应包含item1");

        // 添加非堆叠物品
        var result2 = container.AddItems(item2);
        Assert(result2.result == AddItemResult.Success, "应成功添加非堆叠物品");
        Assert(result2.addedCount == 1, "应添加1个物品");
        Assert(container.GetItemCount("item2") == 1, "容器中应有1个item2");

        // 指定槽位添加
        var result3 = container.AddItems(item1, 3, 2);
        Assert(result3.result == AddItemResult.Success, "应成功添加到指定槽位");
        Assert(container.Slots[2].Item.ID == "item1", "槽位2应包含item1");
        Assert(container.Slots[2].ItemCount == 3, "槽位2应有3个item1");

        // 添加超出索引范围
        var result4 = container.AddItems(item1, 1, 10);
        Assert(result4.result == AddItemResult.SlotNotFound, "添加到不存在槽位应失败");

        // 测试添加直到容器满
        var item3 = CreateTestItem("item3", "测试物品3", false);
        container.AddItems(item3); // 第3个槽位
        container.AddItems(item3); // 第4个槽位
        var result5 = container.AddItems(item3); // 尝试第6个槽位，但容量只有5
        Assert(result5.result == AddItemResult.ContainerIsFull, $"容器满时应返回ContainerIsFull,实际为{result5.result}");

        Debug.Log("物品添加测试通过");
    }

    // 测试物品堆叠
    private void TestStackingItems()
    {
        Debug.Log("测试: 物品堆叠");

        var container = new LinerContainer("container", "背包", "Bag", 5);

        // 创建测试物品
        var stackableItem = CreateTestItem("stackItem", "可堆叠物品", true, 10);
        var nonStackableItem = CreateTestItem("nonStackItem", "不可堆叠物品", false);

        // 添加可堆叠物品
        container.AddItems(stackableItem, 5);

        // 再次添加相同物品，应堆叠
        var result1 = container.AddItems(stackableItem, 3);
        Assert(result1.result == AddItemResult.Success, "应成功堆叠物品");
        Assert(container.Slots[0].ItemCount == 8, "堆叠后应有8个物品");
        Assert(container.GetItemCount("stackItem") == 8, "容器中应有8个stackItem");

        // 测试堆叠上限
        var result2 = container.AddItems(stackableItem, 3);
        Assert(result2.result == AddItemResult.Success, "堆叠到上限应成功");
        Assert(container.Slots[0].ItemCount == 10, "应达到堆叠上限10");
        Assert(container.GetItemCount("stackItem") == 11, "容器中应有11个stackItem（10+1）");
        Assert(container.Slots[1].ItemCount == 1, "多余物品应在新槽位");

        var result3 = container.AddItems(stackableItem, 5);
        Assert(result3.result == AddItemResult.Success, "部分堆叠策略应成功");
        Assert(result3.addedCount == 5, "应添加5个物品");
        Assert(container.GetItemCount("stackItem") == 16, "容器中应有16个stackItem");

        Debug.Log("物品堆叠测试通过");
    }

    // 测试物品移动
    private void TestMovingItems()
    {
        Debug.Log("测试: 物品移动");

        var sourceContainer = new LinerContainer("source", "源背包", "Bag", 15);
        var targetContainer = new LinerContainer("target", "目标背包", "Bag", 15);

        // 创建测试物品
        var item1 = CreateTestItem("item1", "测试物品1", true, 10);
        var item2 = CreateTestItem("item2", "测试物品2", false);

        // 向源容器添加物品
        sourceContainer.AddItems(item1, 5);
        sourceContainer.AddItems(item2);

        // 测试移动物品
        var moveResult1 = sourceContainer.MoveItemToContainer(0, targetContainer);
        Assert(moveResult1.Success, "移动物品应成功");
        Assert(sourceContainer.GetItemCount("item1") == 0, "源容器应没有item1");
        Assert(targetContainer.GetItemCount("item1") == 5, "目标容器应有5个item1");

        // 测试移动非堆叠物品
        var moveResult2 = sourceContainer.MoveItemToContainer(1, targetContainer);
        Assert(moveResult2.Success, $"移动非堆叠物品应成功,结果:{moveResult2.Message}");
        Assert(sourceContainer.GetItemCount("item2") == 0, "源容器应没有item2");
        Assert(targetContainer.GetItemCount("item2") == 1, "目标容器应有1个item2");

        // 测试移动无效槽位
        var moveResult3 = sourceContainer.MoveItemToContainer(5, targetContainer);
        Assert(!moveResult3.Success, "移动无效槽位应失败");

        // 测试部分移动(当目标容器堆叠受限)
        sourceContainer.AddItems(item1, 14); // 添加14个物品到源 10 + 4
        targetContainer.AddItems(item1, 7); // 目标已有5个，再添加7个，共12个 10 + 2

        targetContainer.AddItems(item1, 6); // 10 + 2 -> 10 + 8
        Assert(targetContainer.GetItemCount("item1") == 18, $"目标容器应有18个item1, but its {targetContainer.GetItemCount("item1")}");
        var moveResult4 = sourceContainer.MoveItemToContainer(0, targetContainer); // 尝试移动10个item1，使得目标为 10 + 10 + 8
        Assert(moveResult4.Success, "部分移动应成功");
        Assert(sourceContainer.GetItemCount("item1") == 4, $"源容器应剩余4个item1, but its {sourceContainer.GetItemCount("item1")}");
        Assert(targetContainer.GetItemCount("item1") == 28, $"目标容器应有28个item1, but its {targetContainer.GetItemCount("item1")}");

        Debug.Log("物品移动测试通过");
    }

    // 测试物品移除
    private void TestRemovingItems()
    {
        Debug.Log("测试: 物品移除");

        var container = new LinerContainer("container", "背包", "Bag", 5);

        // 创建测试物品
        var item1 = CreateTestItem("item1", "测试物品1", true, 10);
        var item2 = CreateTestItem("item2", "测试物品2", true, 5);

        // 添加物品
        container.AddItems(item1, 8);
        container.AddItems(item2, 3);

        // 测试通过ID移除物品
        var result1 = container.RemoveItem("item1", 3);
        Assert(result1 == RemoveItemResult.Success, "移除物品应成功");
        Assert(container.GetItemCount("item1") == 5, "容器中应剩余5个item1");

        // 测试移除全部物品
        var result2 = container.RemoveItem("item1", 5);
        Assert(result2 == RemoveItemResult.Success, "移除全部物品应成功");
        Assert(container.GetItemCount("item1") == 0, "容器中应没有item1");
        Assert(!container.ContainsItem("item1"), "容器不应包含item1");

        // 测试通过索引移除物品
        var result3 = container.RemoveItemAtIndex(1, 2);
        Assert(result3 == RemoveItemResult.Success, "通过索引移除物品应成功");
        Assert(container.GetItemCount("item2") == 1, "容器中应剩余1个item2");

        // 测试移除数量不足
        var result4 = container.RemoveItemAtIndex(1, 2);
        Assert(result4 == RemoveItemResult.InsufficientQuantity, "移除数量不足应失败");

        // 测试移除无效ID
        var result5 = container.RemoveItem("nonExistItem");
        Assert(result5 == RemoveItemResult.ItemNotFound, "移除无效ID应返回ItemNotFound");

        // 测试移除无效索引
        var result6 = container.RemoveItemAtIndex(10);
        Assert(result6 == RemoveItemResult.SlotNotFound, "移除无效索引应返回SlotOutOfRange");

        Debug.Log("物品移除测试通过");
    }

    // 测试容器排序
    private void TestContainerSorting()
    {
        Debug.Log("测试: 容器排序");

        var container = new LinerContainer("container", "背包", "Bag", 10);

        // 创建不同类型和名称的测试物品
        var item1 = CreateTestItem("item1", "A苹果", true, 10, "C水果");
        var item2 = CreateTestItem("item2", "B香蕉", true, 10, "C水果");
        var item3 = CreateTestItem("item3", "剑", false, 1, "B武器");
        var item4 = CreateTestItem("item4", "盾", false, 1, "A防具");

        // 打乱顺序添加物品
        container.AddItems(item3);
        container.AddItems(item1, 3);
        container.AddItems(item4);
        container.AddItems(item2, 2);

        // 排序前记录物品总数
        int totalItemCount = container.Slots.Count;
        int appleCount = container.GetItemCount("item1");
        int bananaCount = container.GetItemCount("item2");
        bool hasSword = container.ContainsItem("item3");
        bool hasShield = container.ContainsItem("item4");

        // 执行排序
        container.SortInventory();

        // 验证排序后的顺序和数量
        Assert(container.Slots.Count == totalItemCount, "排序后槽位总数应保持不变");
        Assert(container.GetItemCount("item1") == appleCount, "排序后苹果数量应保持不变");
        Assert(container.GetItemCount("item2") == bananaCount, "排序后香蕉数量应保持不变");
        Assert(container.ContainsItem("item3") == hasSword, "排序后应仍有剑");
        Assert(container.ContainsItem("item4") == hasShield, "排序后应仍有盾");

        // 验证排序顺序：物品类型优先，然后是名称
        // A防具 -> B武器 -> C水果(A苹果) -> C水果(B香蕉)
        Assert(container.Slots[0].Item.Type == "A防具", "第一个物品应为防具");
        Assert(container.Slots[1].Item.Type == "B武器", "第二个物品应为武器");
        Assert(container.Slots[2].Item.Type == "C水果" && container.Slots[2].Item.Name == "A苹果", "第三个物品应为苹果");
        Assert(container.Slots[3].Item.Type == "C水果" && container.Slots[3].Item.Name == "B香蕉", "第四个物品应为香蕉");

        Debug.Log("容器排序测试通过");
    }

    // 创建测试物品辅助方法
    private IItem CreateTestItem(string id, string name, bool isStackable, int maxStack = 1, string type = "Default")
    {
        return new Item
        {
            ID = id,
            Name = name,
            IsStackable = isStackable,
            MaxStackCount = maxStack,
            Type = type
        };
    }

    // 简单断言辅助方法
    private void Assert(bool condition, string message)
    {
        if (!condition)
        {
            Debug.LogError($"断言失败: {message}");
            throw new Exception($"测试失败: {message}");
        }
    }
}