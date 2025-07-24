using EasyPack;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// �������� - ��������������ĸ߼����ϵͳ
/// </summary>
public class InventoryManager
{
    #region �洢

    /// <summary>
    /// ��ID�����������ֵ�
    /// </summary>
    private readonly Dictionary<string, IContainer> _containers = new();

    /// <summary>
    /// �����ͷ�����������������ܵ���
    /// </summary>
    private readonly Dictionary<string, HashSet<string>> _containersByType = new();

    /// <summary>
    /// �������ȼ�����
    /// </summary>
    private readonly Dictionary<string, int> _containerPriorities = new();


    /// <summary>
    /// �����������ã�ҵ����
    /// </summary>
    /// ���ͱ�ʾ"��ʲô"�������ʾ"����˭/����ʲô"
    /// ���磺����Ϊ"����""װ��"������Ϊ"���""��ʱ"֮��
    private readonly Dictionary<string, string> _containerCategories = new();

    /// <summary>
    /// ȫ����Ʒ�����б�
    /// </summary>
    private readonly List<IItemCondition> _globalItemConditions = new();

    /// <summary>
    /// �Ƿ�����ȫ����Ʒ�������
    /// </summary>
    private bool _enableGlobalConditions = false;

    #endregion

    #region ����ע�����ѯ

    /// <summary>
    /// ע����������������
    /// </summary>
    /// <param name="container">Ҫע�������</param>
    /// <param name="priority">�������ȼ�����ֵԽ�����ȼ�Խ��</param>
    /// <param name="category">��������</param>
    /// <returns>ע���Ƿ�ɹ�</returns>
    public bool RegisterContainer(IContainer container, int priority = 0, string category = "Default")
    {
        try
        {
            if (container?.ID == null) return false;

            if (_containers.ContainsKey(container.ID))
            {
                UnregisterContainer(container.ID);
            }

            // ע������
            _containers[container.ID] = container;
            _containerPriorities[container.ID] = priority;
            _containerCategories[container.ID] = category ?? "Default";

            // �����ͽ�������
            string containerType = container.Type ?? "Unknown";
            if (!_containersByType.ContainsKey(containerType))
                _containersByType[containerType] = new HashSet<string>();

            _containersByType[containerType].Add(container.ID);

            // ���ȫ�����������ã���ӵ�������
            if (_enableGlobalConditions)
            {
                ApplyGlobalConditionsToContainer(container);
            }

            OnContainerRegistered?.Invoke(container);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// ע��ָ��ID������
    /// </summary>
    /// <param name="containerId">����ID</param>
    /// <returns>ע���Ƿ�ɹ�</returns>
    public bool UnregisterContainer(string containerId)
    {
        try
        {
            if (string.IsNullOrEmpty(containerId) || !_containers.TryGetValue(containerId, out var container))
                return false;

            // �Ƴ�ȫ������
            if (_enableGlobalConditions)
            {
                RemoveGlobalConditionsFromContainer(container);
            }

            // �����ֵ��Ƴ�
            _containers.Remove(containerId);

            // �����������Ƴ�
            string containerType = container.Type ?? "Unknown";
            if (_containersByType.TryGetValue(containerType, out var typeSet))
            {
                typeSet.Remove(containerId);
                if (typeSet.Count == 0)
                    _containersByType.Remove(containerType);
            }

            // ���������������
            _containerPriorities.Remove(containerId);
            _containerCategories.Remove(containerId);

            OnContainerUnregistered?.Invoke(container);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// ��ȡָ��ID������
    /// </summary>
    /// <param name="containerId">����ID</param>
    /// <returns>�ҵ���������δ�ҵ�����null</returns>
    public IContainer GetContainer(string containerId)
    {
        try
        {
            return string.IsNullOrEmpty(containerId) ? null : _containers.GetValueOrDefault(containerId);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// ��ȡ������ע�������
    /// </summary>
    /// <returns>����������ֻ���б�</returns>
    public IReadOnlyList<IContainer> GetAllContainers()
    {
        try
        {
            return _containers.Values.ToList().AsReadOnly();
        }
        catch
        {
            return new List<IContainer>().AsReadOnly();
        }
    }

    /// <summary>
    /// �����ͻ�ȡ����
    /// </summary>
    /// <param name="containerType">��������</param>
    /// <returns>ָ�����͵������б�</returns>
    public List<IContainer> GetContainersByType(string containerType)
    {
        try
        {
            if (string.IsNullOrEmpty(containerType) || !_containersByType.TryGetValue(containerType, out var containerIds))
                return new List<IContainer>();

            var result = new List<IContainer>();
            foreach (string containerId in containerIds)
            {
                if (_containers.TryGetValue(containerId, out var container))
                {
                    result.Add(container);
                }
            }
            return result;
        }
        catch
        {
            return new List<IContainer>();
        }
    }

    /// <summary>
    /// �������ȡ����
    /// </summary>
    /// <param name="category">��������</param>
    /// <returns>ָ������������б�</returns>
    public List<IContainer> GetContainersByCategory(string category)
    {
        try
        {
            if (string.IsNullOrEmpty(category))
                return new List<IContainer>();

            var result = new List<IContainer>();
            foreach (var kvp in _containerCategories)
            {
                if (kvp.Value == category && _containers.TryGetValue(kvp.Key, out var container))
                {
                    result.Add(container);
                }
            }
            return result;
        }
        catch
        {
            return new List<IContainer>();
        }
    }

    /// <summary>
    /// �����ȼ������ȡ����
    /// </summary>
    /// <param name="descending">�Ƿ������У����ȼ��ߵ���ǰ��</param>
    /// <returns>�����ȼ�����������б�</returns>
    public List<IContainer> GetContainersByPriority(bool descending = true)
    {
        try
        {
            var sortedContainers = _containers.Values.ToList();
            sortedContainers.Sort((a, b) =>
            {
                int priorityA = _containerPriorities.GetValueOrDefault(a.ID, 0);
                int priorityB = _containerPriorities.GetValueOrDefault(b.ID, 0);
                return descending ? priorityB.CompareTo(priorityA) : priorityA.CompareTo(priorityB);
            });
            return sortedContainers;
        }
        catch
        {
            return new List<IContainer>();
        }
    }

    /// <summary>
    /// ��������Ƿ���ע��
    /// </summary>
    /// <param name="containerId">����ID</param>
    /// <returns>�Ƿ���ע��</returns>
    public bool IsContainerRegistered(string containerId)
    {
        try
        {
            return !string.IsNullOrEmpty(containerId) && _containers.ContainsKey(containerId);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// ��ȡ��ע������������
    /// </summary>
    public int ContainerCount => _containers.Count;

    #endregion

    #region ����

    /// <summary>
    /// �����������ȼ�
    /// </summary>
    /// <param name="containerId">����ID</param>
    /// <param name="priority">���ȼ���ֵ</param>
    /// <returns>�����Ƿ�ɹ�</returns>
    public bool SetContainerPriority(string containerId, int priority)
    {
        try
        {
            if (!IsContainerRegistered(containerId))
                return false;

            _containerPriorities[containerId] = priority;
            OnContainerPriorityChanged?.Invoke(containerId, priority);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// ��ȡ�������ȼ�
    /// </summary>
    /// <param name="containerId">����ID</param>
    /// <returns>�������ȼ���δ�ҵ�����0</returns>
    public int GetContainerPriority(string containerId)
    {
        try
        {
            return _containerPriorities.GetValueOrDefault(containerId, 0);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// ������������
    /// </summary>
    /// <param name="containerId">����ID</param>
    /// <param name="category">��������</param>
    /// <returns>�����Ƿ�ɹ�</returns>
    public bool SetContainerCategory(string containerId, string category)
    {
        try
        {
            if (!IsContainerRegistered(containerId))
                return false;

            string oldCategory = _containerCategories.GetValueOrDefault(containerId, "Default");
            _containerCategories[containerId] = category ?? "Default";
            OnContainerCategoryChanged?.Invoke(containerId, oldCategory, category);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// ��ȡ��������
    /// </summary>
    /// <param name="containerId">����ID</param>
    /// <returns>�������࣬δ�ҵ�����"Default"</returns>
    public string GetContainerCategory(string containerId)
    {
        try
        {
            return _containerCategories.GetValueOrDefault(containerId, "Default");
        }
        catch
        {
            return "Default";
        }
    }


    #endregion

    #region ȫ������

    /// <summary>
    /// �����Ʒ�Ƿ�����ȫ������
    /// </summary>
    /// <param name="item">Ҫ������Ʒ</param>
    /// <returns>�Ƿ���������ȫ������</returns>
    public bool ValidateGlobalItemConditions(IItem item)
    {
        try
        {
            if (item == null) return false;
            if (!_enableGlobalConditions)
                return true;

            foreach (var condition in _globalItemConditions)
            {
                if (!condition.IsCondition(item))
                    return false;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// ���ȫ����Ʒ����
    /// </summary>
    /// <param name="condition">��Ʒ����</param>
    public void AddGlobalItemCondition(IItemCondition condition)
    {
        try
        {
            if (condition != null && !_globalItemConditions.Contains(condition))
            {
                _globalItemConditions.Add(condition);

                // ���ȫ�����������ã���ӵ���������
                if (_enableGlobalConditions)
                {
                    foreach (var container in _containers.Values)
                    {
                        if (!container.ContainerCondition.Contains(condition))
                        {
                            container.ContainerCondition.Add(condition);
                        }
                    }
                }

                OnGlobalConditionAdded?.Invoke(condition);
            }
        }
        catch
        {
            // ��Ĭ�����쳣
        }
    }

    /// <summary>
    /// �Ƴ�ȫ����Ʒ����
    /// </summary>
    /// <param name="condition">��Ʒ����</param>
    /// <returns>�Ƴ��Ƿ�ɹ�</returns>
    public bool RemoveGlobalItemCondition(IItemCondition condition)
    {
        try
        {
            if (condition == null) return false;

            bool removed = _globalItemConditions.Remove(condition);
            if (removed)
            {
                // �������������Ƴ�������
                foreach (var container in _containers.Values)
                {
                    container.ContainerCondition.Remove(condition);
                }

                OnGlobalConditionRemoved?.Invoke(condition);
            }
            return removed;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// �����Ƿ�����ȫ������
    /// </summary>
    /// <param name="enable">�Ƿ�����</param>
    public void SetGlobalConditionsEnabled(bool enable)
    {
        try
        {
            if (_enableGlobalConditions == enable) return;

            _enableGlobalConditions = enable;

            if (enable)
            {
                foreach (var container in _containers.Values)
                {
                    ApplyGlobalConditionsToContainer(container);
                }
            }
            else
            {
                foreach (var container in _containers.Values)
                {
                    RemoveGlobalConditionsFromContainer(container);
                }
            }
        }
        catch
        {
        }
    }

    /// <summary>
    /// ��ȡ�Ƿ�����ȫ������
    /// </summary>
    public bool IsGlobalConditionsEnabled => _enableGlobalConditions;
    /// <summary>
    /// ��ȫ������Ӧ�õ�ָ������
    /// </summary>
    /// <param name="container">Ŀ������</param>
    private void ApplyGlobalConditionsToContainer(IContainer container)
    {
        try
        {
            foreach (var condition in _globalItemConditions)
            {
                if (!container.ContainerCondition.Contains(condition))
                {
                    container.ContainerCondition.Add(condition);
                }
            }
        }
        catch
        {
        }
    }

    /// <summary>
    /// ��ָ�������Ƴ�ȫ������
    /// </summary>
    /// <param name="container">Ŀ������</param>
    private void RemoveGlobalConditionsFromContainer(IContainer container)
    {
        try
        {
            foreach (var condition in _globalItemConditions)
            {
                container.ContainerCondition.Remove(condition);
            }
        }
        catch
        {
        }
    }

    #endregion

    #region �¼�

    /// <summary>
    /// ����ע���¼�
    /// </summary>
    public event System.Action<IContainer> OnContainerRegistered;

    /// <summary>
    /// ����ע���¼�
    /// </summary>
    public event System.Action<IContainer> OnContainerUnregistered;

    /// <summary>
    /// �������ȼ�����¼�
    /// </summary>
    public event System.Action<string, int> OnContainerPriorityChanged;

    /// <summary>
    /// �����������¼�
    /// </summary>
    public event System.Action<string, string, string> OnContainerCategoryChanged;

    /// <summary>
    /// ȫ����������¼�
    /// </summary>
    public event System.Action<IItemCondition> OnGlobalConditionAdded;

    /// <summary>
    /// ȫ�������Ƴ��¼�
    /// </summary>
    public event System.Action<IItemCondition> OnGlobalConditionRemoved;

    /// <summary>
    /// ȫ�ֻ���ˢ���¼�
    /// </summary>
    public event System.Action OnGlobalCacheRefreshed;

    /// <summary>
    /// ȫ�ֻ�����֤�¼�
    /// </summary>
    public event System.Action OnGlobalCacheValidated;

    #endregion

    #region ��������Ʒ����

    /// <summary>
    /// �ƶ���������ṹ
    /// </summary>
    public struct MoveRequest
    {
        public string FromContainerId;
        public int FromSlot;
        public string ToContainerId;
        public int ToSlot;
        public int Count;
        public string ExpectedItemId;

        public MoveRequest(string fromContainerId, int fromSlot, string toContainerId, int toSlot = -1, int count = -1, string expectedItemId = null)
        {
            FromContainerId = fromContainerId;
            FromSlot = fromSlot;
            ToContainerId = toContainerId;
            ToSlot = toSlot;
            Count = count;
            ExpectedItemId = expectedItemId;
        }
    }

    /// <summary>
    /// �ƶ��������
    /// </summary>
    public enum MoveResult
    {
        Success,
        SourceContainerNotFound,
        TargetContainerNotFound,
        SourceSlotEmpty,
        SourceSlotNotFound,
        TargetSlotNotFound,
        ItemNotFound,
        InsufficientQuantity,
        TargetContainerFull,
        ItemConditionNotMet,
        Failed
    }

    /// <summary>
    /// ��������Ʒ�ƶ�
    /// </summary>
    /// <param name="fromContainerId">Դ����ID</param>
    /// <param name="fromSlot">Դ��λ����</param>
    /// <param name="toContainerId">Ŀ������ID</param>
    /// <param name="toSlot">Ŀ���λ������-1��ʾ�Զ�Ѱ��</param>
    /// <returns>�ƶ����</returns>
    public MoveResult MoveItem(string fromContainerId, int fromSlot, string toContainerId, int toSlot = -1)
    {
        try
        {
            var sourceContainer = GetContainer(fromContainerId);
            if (sourceContainer == null)
                return MoveResult.SourceContainerNotFound;

            var targetContainer = GetContainer(toContainerId);
            if (targetContainer == null)
                return MoveResult.TargetContainerNotFound;

            if (fromSlot < 0 || fromSlot >= sourceContainer.Slots.Count)
                return MoveResult.SourceSlotNotFound;

            var sourceSlot = sourceContainer.Slots[fromSlot];
            if (!sourceSlot.IsOccupied || sourceSlot.Item == null)
                return MoveResult.SourceSlotEmpty;

            var item = sourceSlot.Item;
            int itemCount = sourceSlot.ItemCount;

            // ���ȫ������
            if (!ValidateGlobalItemConditions(item))
                return MoveResult.ItemConditionNotMet;

            // ������ӵ�Ŀ������
            var (addResult, addedCount) = targetContainer.AddItems(item, itemCount, toSlot);

            if (addResult == AddItemResult.Success && addedCount > 0)
            {
                // ��Դ�����Ƴ�
                var removeResult = sourceContainer.RemoveItemAtIndex(fromSlot, addedCount, item.ID);

                if (removeResult == RemoveItemResult.Success)
                {
                    OnItemMoved?.Invoke(fromContainerId, fromSlot, toContainerId, item, addedCount);
                    return MoveResult.Success;
                }
            }

            return addResult switch
            {
                AddItemResult.ContainerIsFull => MoveResult.TargetContainerFull,
                AddItemResult.ItemConditionNotMet => MoveResult.ItemConditionNotMet,
                AddItemResult.SlotNotFound => MoveResult.TargetSlotNotFound,
                _ => MoveResult.Failed
            };
        }
        catch
        {
            return MoveResult.Failed;
        }
    }

    /// <summary>
    /// ָ��������Ʒת��
    /// </summary>
    /// <param name="itemId">��ƷID</param>
    /// <param name="count">ת������</param>
    /// <param name="fromContainerId">Դ����ID</param>
    /// <param name="toContainerId">Ŀ������ID</param>
    /// <returns>ת�ƽ����ʵ��ת������</returns>
    public (MoveResult result, int transferredCount) TransferItems(string itemId, int count, string fromContainerId, string toContainerId)
    {
        try
        {
            if(string.IsNullOrEmpty(itemId))
                return (MoveResult.ItemNotFound, 0);

            var sourceContainer = GetContainer(fromContainerId);
            if (sourceContainer == null)
                return (MoveResult.SourceContainerNotFound, 0);

            var targetContainer = GetContainer(toContainerId);
            if (targetContainer == null)
                return (MoveResult.TargetContainerNotFound, 0);

            if (!sourceContainer.HasItem(itemId))
                return (MoveResult.ItemNotFound, 0);

            int availableCount = sourceContainer.GetItemTotalCount(itemId);
            if (availableCount < count)
                return (MoveResult.InsufficientQuantity, 0);

            // ��ȡ��Ʒ����
            IItem item = null;
            foreach (var slot in sourceContainer.Slots)
            {
                if (slot.IsOccupied && slot.Item?.ID == itemId)
                {
                    item = slot.Item;
                    break;
                }
            }

            if (item == null)
                return (MoveResult.ItemNotFound, 0);

            // ���ȫ������
            if (!ValidateGlobalItemConditions(item))
                return (MoveResult.ItemConditionNotMet, 0);

            // ������ӵ�Ŀ������
            var (addResult, addedCount) = targetContainer.AddItems(item, count);

            if (addResult == AddItemResult.Success && addedCount > 0)
            {
                // ��Դ�����Ƴ�
                var removeResult = sourceContainer.RemoveItem(itemId, addedCount);

                if (removeResult == RemoveItemResult.Success)
                {
                    OnItemsTransferred?.Invoke(fromContainerId, toContainerId, itemId, addedCount);
                    return (MoveResult.Success, addedCount);
                }
            }

            return addResult switch
            {
                AddItemResult.ContainerIsFull => (MoveResult.TargetContainerFull, 0),
                AddItemResult.ItemConditionNotMet => (MoveResult.ItemConditionNotMet, 0),
                _ => (MoveResult.Failed, 0)
            };
        }
        catch
        {
            return (MoveResult.Failed, 0);
        }
    }

    /// <summary>
    /// �Զ�Ѱ�����λ��ת����Ʒ
    /// </summary>
    /// <param name="itemId">��ƷID</param>
    /// <param name="fromContainerId">Դ����ID</param>
    /// <param name="toContainerId">Ŀ������ID</param>
    /// <returns>ת�ƽ����ʵ��ת������</returns>
    public (MoveResult result, int transferredCount) AutoMoveItem(string itemId, string fromContainerId, string toContainerId)
    {
        try
        {
            if (string.IsNullOrEmpty(itemId))
                return (MoveResult.ItemNotFound, 0);

            var sourceContainer = GetContainer(fromContainerId);
            if (sourceContainer == null)
                return (MoveResult.SourceContainerNotFound, 0);

            var targetContainer = GetContainer(toContainerId);
            if (targetContainer == null)
                return (MoveResult.TargetContainerNotFound, 0);

            if (!sourceContainer.HasItem(itemId))
                return (MoveResult.ItemNotFound, 0);

            int totalCount = sourceContainer.GetItemTotalCount(itemId);
            return TransferItems(itemId, totalCount, fromContainerId, toContainerId);
        }
        catch
        {
            return (MoveResult.Failed, 0);
        }
    }

    /// <summary>
    /// �����ƶ�����
    /// </summary>
    /// <param name="requests">�ƶ������б�</param>
    /// <returns>ÿ�������ִ�н��</returns>
    public List<(MoveRequest request, MoveResult result, int movedCount)> BatchMoveItems(List<MoveRequest> requests)
    {
        var results = new List<(MoveRequest request, MoveResult result, int movedCount)>();

        try
        {
            if (requests == null)
            {
                return results;
            }

            foreach (var request in requests)
            {
                if (request.Count > 0)
                {
                    // ָ�������ƶ�
                    if (!string.IsNullOrEmpty(request.ExpectedItemId))
                    {
                        var (result, transferredCount) = TransferItems(request.ExpectedItemId, request.Count,
                            request.FromContainerId, request.ToContainerId);
                        results.Add((request, result, transferredCount));
                    }
                    else
                    {
                        // ��Ҫ�Ȼ�ȡ��λ�е���ƷID
                        var sourceContainer = GetContainer(request.FromContainerId);
                        if (sourceContainer != null && request.FromSlot >= 0 && request.FromSlot < sourceContainer.Slots.Count)
                        {
                            var slot = sourceContainer.Slots[request.FromSlot];
                            if (slot.IsOccupied && slot.Item != null)
                            {
                                var (result, transferredCount) = TransferItems(slot.Item.ID, request.Count,
                                    request.FromContainerId, request.ToContainerId);
                                results.Add((request, result, transferredCount));
                            }
                            else
                            {
                                results.Add((request, MoveResult.SourceSlotEmpty, 0));
                            }
                        }
                        else
                        {
                            results.Add((request, MoveResult.SourceContainerNotFound, 0));
                        }
                    }
                }
                else
                {
                    // ������λ�ƶ�
                    var result = MoveItem(request.FromContainerId, request.FromSlot,
                        request.ToContainerId, request.ToSlot);

                    // ��ȡ�ƶ�������
                    int movedCount = 0;
                    if (result == MoveResult.Success)
                    {
                        var sourceContainer = GetContainer(request.FromContainerId);
                        if (sourceContainer != null && request.FromSlot >= 0 && request.FromSlot < sourceContainer.Slots.Count)
                        {
                            var slot = sourceContainer.Slots[request.FromSlot];
                            movedCount = slot.IsOccupied ? slot.ItemCount : 0;
                        }
                    }

                    results.Add((request, result, movedCount));
                }
            }

            OnBatchMoveCompleted?.Invoke(results);
        }
        catch
        {
            while (results.Count < requests.Count)
            {
                results.Add((requests[results.Count], MoveResult.Failed, 0));
            }
        }

        return results;
    }

    /// <summary>
    /// ������Ʒ���������
    /// </summary>
    /// <param name="item">Ҫ�������Ʒ</param>
    /// <param name="totalCount">������</param>
    /// <param name="targetContainerIds">Ŀ������ID�б�</param>
    /// <returns>������������ID�ͷ��䵽������</returns>
    public Dictionary<string, int> DistributeItems(IItem item, int totalCount, List<string> targetContainerIds)
    {
        var results = new Dictionary<string, int>();

        try
        {
            if (item == null || totalCount <= 0 || targetContainerIds?.Count == 0)
                return results;

            // ���ȫ������
            if (!ValidateGlobalItemConditions(item))
                return results;

            int remainingCount = totalCount;
            var sortedContainers = new List<(string id, IContainer container, int priority)>();

            // ׼�������б������ȼ�����
            foreach (string containerId in targetContainerIds)
            {
                var container = GetContainer(containerId);
                if (container != null)
                {
                    int priority = GetContainerPriority(containerId);
                    sortedContainers.Add((containerId, container, priority));
                }
            }

            // �����ȼ���������
            sortedContainers.Sort((a, b) => b.priority.CompareTo(a.priority));

            // �����ȼ�������Ʒ
            foreach (var (containerId, container, _) in sortedContainers)
            {
                if (remainingCount <= 0) break;

                var (addResult, addedCount) = container.AddItems(item, remainingCount);

                if (addResult == AddItemResult.Success && addedCount > 0)
                {
                    results[containerId] = addedCount;
                    remainingCount -= addedCount;
                }
                else if (addResult == AddItemResult.ContainerIsFull && addedCount > 0)
                {
                    // ������ӳɹ�
                    results[containerId] = addedCount;
                    remainingCount -= addedCount;
                }
            }

            OnItemsDistributed?.Invoke(item, totalCount, results, remainingCount);
        }
        catch
        {
        }

        return results;
    }

    #endregion

    #region �����������¼�

    /// <summary>
    /// ��Ʒ�ƶ��¼�
    /// </summary>
    public event System.Action<string, int, string, IItem, int> OnItemMoved;

    /// <summary>
    /// ��Ʒת���¼�
    /// </summary>
    public event System.Action<string, string, string, int> OnItemsTransferred;

    /// <summary>
    /// �����ƶ�����¼�
    /// </summary>
    public event System.Action<List<(MoveRequest request, MoveResult result, int movedCount)>> OnBatchMoveCompleted;

    /// <summary>
    /// ��Ʒ�����¼�
    /// </summary>
    public event System.Action<IItem, int, Dictionary<string, int>, int> OnItemsDistributed;

    #endregion

    #region ȫ����Ʒ����
    /// <summary>
    /// ȫ����Ʒ�������
    /// </summary>
    public struct GlobalItemResult
    {
        public string ContainerId;
        public int SlotIndex;
        public IItem Item;
        public int IndexCount;

        public GlobalItemResult(string containerId, int slotIndex, IItem item, int count)
        {
            ContainerId = containerId;
            SlotIndex = slotIndex;
            Item = item;
            IndexCount = count;
        }
    }

    /// <summary>
    /// ȫ�ֲ�����Ʒ
    /// </summary>
    /// <param name="itemId">��ƷID</param>
    /// <returns>��Ʒ���ڵ�λ���б�</returns>
    public List<GlobalItemResult> FindItemGlobally(string itemId)
    {
        var results = new List<GlobalItemResult>();

        try
        {
            if (string.IsNullOrEmpty(itemId))
                return results;

            foreach (Container container in _containers.Values)
            {
                if (container.HasItem(itemId)) // ���û�����ټ��
                {
                    var slotIndices = container.FindSlotIndices(itemId); // ���û����ȡ��λ����
                    foreach (int slotIndex in slotIndices)
                    {
                        if (slotIndex < container.Slots.Count)
                        {
                            var slot = container.Slots[slotIndex];
                            if (slot.IsOccupied && slot.Item?.ID == itemId)
                            {
                                results.Add(new GlobalItemResult(container.ID, slotIndex, slot.Item, slot.ItemCount));
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            results.Clear();
            foreach (var container in _containers.Values)
            {
                for (int i = 0; i < container.Slots.Count; i++)
                {
                    var slot = container.Slots[i];
                    if (slot.IsOccupied && slot.Item?.ID == itemId)
                    {
                        results.Add(new GlobalItemResult(container.ID, i, slot.Item, slot.ItemCount));
                    }
                }
            }
        }

        return results;
    }

    /// <summary>
    /// ��ȡȫ����Ʒ����
    /// </summary>
    /// <param name="itemId">��ƷID</param>
    /// <returns>ȫ����Ʒ������</returns>
    public int GetGlobalItemCount(string itemId)
    {
        try
        {
            if (string.IsNullOrEmpty(itemId))
                return 0;

            int totalCount = 0;
            foreach (var container in _containers.Values)
            {
                totalCount += container.GetItemTotalCount(itemId);
            }

            return totalCount;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// ���Ұ���ָ����Ʒ������
    /// </summary>
    /// <param name="itemId">��ƷID</param>
    /// <returns>��������Ʒ�������б������</returns>
    public Dictionary<string, int> FindContainersWithItem(string itemId)
    {
        var results = new Dictionary<string, int>();

        try
        {
            if (string.IsNullOrEmpty(itemId))
                return results;

            foreach (var container in _containers.Values)
            {
                if (container.HasItem(itemId))
                {
                    int count = container.GetItemTotalCount(itemId);
                    if (count > 0)
                    {
                        results[container.ID] = count;
                    }
                }
            }
        }
        catch
        {
            
        }

        return results;
    }

    /// <summary>
    /// ������ȫ��������Ʒ
    /// </summary>
    /// <param name="condition">��������</param>
    /// <returns>������������Ʒ�б�</returns>
    public List<GlobalItemResult> SearchItemsByCondition(System.Func<IItem, bool> condition)
    {
        var results = new List<GlobalItemResult>();

        try
        {
            if (condition == null)
                return results;

            foreach (var container in _containers.Values)
            {
                for (int i = 0; i < container.Slots.Count; i++)
                {
                    var slot = container.Slots[i];
                    if (slot.IsOccupied && slot.Item != null && condition(slot.Item))
                    {
                        results.Add(new GlobalItemResult(container.ID, i, slot.Item, slot.ItemCount));
                    }
                }
            }
        }
        catch
        {
            
        }

        return results;
    }

    /// <summary>
    /// ����Ʒ����ȫ������
    /// </summary>
    /// <param name="itemType">��Ʒ����</param>
    /// <returns>ָ�����͵���Ʒ�б�</returns>
    public List<GlobalItemResult> SearchItemsByType(string itemType)
    {
        var results = new List<GlobalItemResult>();

        try
        {
            if (string.IsNullOrEmpty(itemType))
                return results;

            foreach (Container container in _containers.Values)
            {
                // �������������ͻ����ѯ
                var typeItems = container.GetItemsByType(itemType);
                foreach (var (slotIndex, item, count) in typeItems)
                {
                    results.Add(new GlobalItemResult(container.ID, slotIndex, item, count));
                }
            }
        }
        catch
        {
            // �����쳣ʱ���˵���������
            results.Clear();
            results = SearchItemsByCondition(item => item.Type == itemType);
        }

        return results;
    }

    /// <summary>
    /// ����Ʒ����ȫ������
    /// </summary>
    /// <param name="namePattern">����ģʽ</param>
    /// <returns>��������ģʽ����Ʒ�б�</returns>
    public List<GlobalItemResult> SearchItemsByName(string namePattern)
    {
        var results = new List<GlobalItemResult>();

        try
        {
            if (string.IsNullOrEmpty(namePattern))
                return results;

            foreach (Container container in _containers.Values)
            {
                // �������������ƻ����ѯ
                var nameItems = container.GetItemsByName(namePattern);
                foreach (var (slotIndex, item, count) in nameItems)
                {
                    results.Add(new GlobalItemResult(container.ID, slotIndex, item, count));
                }
            }
        }
        catch
        {
            results.Clear();
            results = SearchItemsByCondition(item => item.Name?.Contains(namePattern) == true);
        }

        return results;
    }
    /// <summary>
    /// ������ȫ��������Ʒ
    /// </summary>
    /// <param name="attributeName">��������</param>
    /// <param name="attributeValue">����ֵ</param>
    /// <returns>����������������Ʒ�б�</returns>
    public List<GlobalItemResult> SearchItemsByAttribute(string attributeName, object attributeValue)
    {
        var results = new List<GlobalItemResult>();

        try
        {
            if (string.IsNullOrEmpty(attributeName))
                return results;

            foreach (Container container in _containers.Values)
            {
                // �������������Ի����ѯ
                var attributeItems = container.GetItemsByAttribute(attributeName, attributeValue);
                foreach (var (slotIndex, item, count) in attributeItems)
                {
                    results.Add(new GlobalItemResult(container.ID, slotIndex, item, count));
                }
            }
        }
        catch
        {
            results.Clear();
            results = SearchItemsByCondition(item =>
                item.Attributes != null &&
                item.Attributes.TryGetValue(attributeName, out var value) &&
                (attributeValue == null || value?.Equals(attributeValue) == true));
        }

        return results;
    }
    #endregion

    #region ȫ�ֻ���
    /// <summary>
    /// ˢ��ȫ�ֻ���
    /// </summary>
    public void RefreshGlobalCache()
    {
        try
        {
            foreach (var container in _containers.Values)
            {
                if (container is Container containerImpl)
                {
                    containerImpl.RebuildCaches();
                }
            }

            OnGlobalCacheRefreshed?.Invoke();
        }
        catch
        {
            
        }
    }

    /// <summary>
    /// ��֤ȫ�ֻ���
    /// </summary>
    public void ValidateGlobalCache()
    {
        try
        {
            foreach (var container in _containers.Values)
            {
                if (container is Container containerImpl)
                {
                    containerImpl.ValidateCaches();
                }
            }

            OnGlobalCacheValidated?.Invoke();
        }
        catch
        {
            
        }
    }
    #endregion
}