using System.Collections.Generic;
using EasyPack;
using UnityEngine;
using UnityEngine.UI;

namespace EasyPack.GamePropertySystem.Example.EatGame
{
    /// <summary>
    /// EatGame主控制器
    /// </summary>
    public class EatGameController : EasyPackController
    {
        [Header("UI References")]
        [SerializeField] private Text statusText;
        [SerializeField] private Text strangeScoreText;
        [SerializeField] private Text strangeScoreRuleText;
        [SerializeField] private Text dayText;
        [SerializeField] private Button[] foodButtons;
        [SerializeField] private Text[] foodTexts;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Text gameOverText;
        [SerializeField] private GameObject confirmPanel;
        [SerializeField] private Text confirmText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button nextDayButton;
        [SerializeField] private Button restartButton;

        private PlayerAttributes playerAttributes;
        private FoodGenerator foodGenerator;
        private List<Food> currentFoods;
        private int currentDay = 1;
        private Food selectedFood;
        private bool isWaitingForNextDay = false;

        private void Start()
        {
            InitializeGame();
            StartNewDay();
        }

        private void InitializeGame()
        {
            playerAttributes = new PlayerAttributes();
            foodGenerator = new FoodGenerator();

            // 监听属性变化
            playerAttributes.Satiety.OnValueChanged += OnAttributeChanged;
            playerAttributes.Health.OnValueChanged += OnAttributeChanged;
            playerAttributes.Sanity.OnValueChanged += OnAttributeChanged;
            playerAttributes.StrangeScore.OnValueChanged += OnAttributeChanged;

            // 注册按钮点击事件
            for (int i = 0; i < foodButtons.Length; i++)
            {
                int index = i;  // 闭包捕获
                foodButtons[i].onClick.AddListener(() => OnFoodSelected(index));
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(ConfirmEat);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(CancelSelection);
            }

            if (nextDayButton != null)
            {
                nextDayButton.onClick.AddListener(NextDay);
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(RestartGame);
            }

            Debug.Log("=== EatGame 初始化完成 ===");
            Debug.Log($"初始属性状态:\n{playerAttributes.GetStatusDescription()}");

            UpdateUI();
        }

        private void StartNewDay()
        {
            if (isWaitingForNextDay) return;

            // 生成当日食物
            currentFoods = foodGenerator.GenerateDailyFoods();

            Debug.Log($"\n=== 第 {currentDay} 天开始 ===");
            Debug.Log($"今日可选食物:");
            for (int i = 0; i < currentFoods.Count; i++)
            {
                Debug.Log($"[{i + 1}] {foodGenerator.GetFoodEffectDescription(currentFoods[i])}");
            }

            // 更新UI
            dayText.text = $"第 {currentDay} 天";
            for (int i = 0; i < foodButtons.Length && i < currentFoods.Count; i++)
            {
                foodButtons[i].gameObject.SetActive(true);
                foodTexts[i].text = foodGenerator.GetFoodEffectDescription(currentFoods[i]);
            }

            // 隐藏确认面板
            if (confirmPanel != null) confirmPanel.SetActive(false);
            if (nextDayButton != null) nextDayButton.gameObject.SetActive(false);

            UpdateUI();
        }

        public void OnFoodSelected(int foodIndex)
        {
            if (foodIndex < 0 || foodIndex >= currentFoods.Count) return;

            selectedFood = currentFoods[foodIndex];

            Debug.Log($"\n=== 选择食物: {selectedFood.Name} ===");
            Debug.Log($"食用前属性:\n{playerAttributes.GetStatusDescription()}");

            // 显示即时效果
            if (selectedFood.ImmediateEffects.Count > 0)
            {
                Debug.Log("即时效果:");
                foreach (var effect in selectedFood.ImmediateEffects)
                {
                    string attrName = GetAttributeDisplayName(effect.Key);
                    string sign = effect.Value >= 0 ? "+" : "";
                    Debug.Log($"  {attrName}: {sign}{effect.Value}");
                }
            }

            // 显示确认面板
            if (confirmPanel != null)
            {
                confirmPanel.SetActive(true);
                confirmText.text = $"确认食用: {selectedFood.Name}\n\n{foodGenerator.GetFoodEffectDescription(selectedFood)}";
            }

            // 禁用食物按钮
            foreach (var button in foodButtons)
            {
                button.interactable = false;
            }
        }

        public void ConfirmEat()
        {
            if (selectedFood == null) return;

            // 应用食物效果
            selectedFood.ApplyImmediateEffects(playerAttributes);
            selectedFood.ApplySustainedEffects(playerAttributes);

            Debug.Log($"食用后属性:\n{playerAttributes.GetStatusDescription()}");

            // 显示持续效果
            if (selectedFood.SustainedEffects.Count > 0)
            {
                Debug.Log("添加持续效果:");
                foreach (var effect in selectedFood.SustainedEffects)
                {
                    string attrName = GetAttributeDisplayName(effect.Key);
                    string sign = effect.Value.value >= 0 ? "+" : "";
                    Debug.Log($"  {attrName}: {sign}{effect.Value.value} (持续{effect.Value.duration}天, 类型:{effect.Value.type})");

                    // 详细显示modifier应用逻辑
                    Debug.Log($"    Modifier详情: 类型={effect.Value.type}, 值={effect.Value.value}, 持续时间={effect.Value.duration}天");
                    if (effect.Value.type == ModifierType.Mul)
                    {
                        float baseValue = GetBaseValueForAttribute(effect.Key, playerAttributes);
                        float expectedResult = baseValue * effect.Value.value;
                        Debug.Log($"    Mul计算: {baseValue} × {effect.Value.value} = {expectedResult}");
                    }
                }
            }

            // 隐藏确认面板，显示下一天按钮
            if (confirmPanel != null) confirmPanel.SetActive(false);
            if (nextDayButton != null) nextDayButton.gameObject.SetActive(true);

            isWaitingForNextDay = true;
            UpdateUI();
        }

        public void CancelSelection()
        {
            Debug.Log("取消选择食物");

            selectedFood = null;

            // 隐藏确认面板
            if (confirmPanel != null)
            {
                confirmPanel.SetActive(false);
            }

            // 重新启用食物按钮
            foreach (var button in foodButtons)
            {
                button.interactable = true;
            }

            UpdateUI();
        }

        public void NextDay()
        {
            // 处理每日结算
            Debug.Log("\n--- 每日属性结算 ---");
            Debug.Log($"结算前每日变化: 饱食度{playerAttributes.SatietyChangePerDay.GetValue():+0.0;-0.0;0}, 生命值{playerAttributes.HealthChangePerDay.GetValue():+0.0;-0.0;0}, SAN{playerAttributes.SanityChangePerDay.GetValue():+0.0;-0.0;0}");

            // 显示活跃修饰符详情
            Debug.Log($"当前活跃修饰符数量: {playerAttributes.modifierManager.GetActiveModifierCount()}");

            playerAttributes.ProcessDailyChanges();

            Debug.Log($"结算后属性:\n{playerAttributes.GetStatusDescription()}");

            // 检查游戏结束
            if (playerAttributes.IsGameOver())
            {
                string reason = "";
                if (playerAttributes.Health.GetValue() <= 0) reason = "生命值";
                else if (playerAttributes.Satiety.GetValue() <= 0) reason = "饱食度";
                else if (playerAttributes.Sanity.GetValue() <= 0) reason = "SAN值";

                Debug.Log($"❌ 游戏结束！{reason}降至0");
                GameOver();
                return;
            }

            // 下一天
            currentDay++;
            isWaitingForNextDay = false;
            Debug.Log($"✅ 第 {currentDay - 1} 天完成，进入下一天\n");

            // 重新启用所有食物按钮
            foreach (var button in foodButtons)
            {
                button.interactable = true;
            }

            StartNewDay();
        }

        private void OnAttributeChanged(float oldValue, float newValue)
        {
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (statusText != null)
            {
                statusText.text = playerAttributes.GetStatusDescription();
            }

            if (strangeScoreText != null)
            {
                strangeScoreText.text = $"奇怪评分: {playerAttributes.StrangeScore.GetValue():F1}/100";
            }
        }

        private void GameOver()
        {
            string reason = "";
            if (playerAttributes.Health.GetValue() <= 0) reason = "生命值";
            else if (playerAttributes.Satiety.GetValue() <= 0) reason = "饱食度";
            else if (playerAttributes.Sanity.GetValue() <= 0) reason = "SAN值";

            Debug.Log($"\n🏁 游戏结束！{reason}降至0");
            Debug.Log($"生存天数: {currentDay - 1}");
            Debug.Log($"最终属性状态:\n{playerAttributes.GetStatusDescription()}");

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
                gameOverText.text = $"游戏结束！\n{reason}降至0\n生存了 {currentDay - 1} 天\n\n最终状态:\n{playerAttributes.GetStatusDescription()}";
            }

            // 显示重来按钮
            if (restartButton != null)
            {
                restartButton.gameObject.SetActive(true);
            }

            // 禁用食物按钮
            foreach (var button in foodButtons)
            {
                button.gameObject.SetActive(false);
            }
        }

        public void RestartGame()
        {
            currentDay = 1;
            selectedFood = null;
            isWaitingForNextDay = false;
            playerAttributes = new PlayerAttributes();

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            if (confirmPanel != null)
            {
                confirmPanel.SetActive(false);
            }

            if (nextDayButton != null)
            {
                nextDayButton.gameObject.SetActive(false);
            }

            if (restartButton != null)
            {
                restartButton.gameObject.SetActive(false);
            }

            // 重新启用所有食物按钮
            foreach (var button in foodButtons)
            {
                button.interactable = true;
                button.gameObject.SetActive(true);
            }

            InitializeGame();
            StartNewDay();
        }

        private string GetAttributeDisplayName(string attrKey)
        {
            switch (attrKey)
            {
                case "Satiety": return "饱食度";
                case "Health": return "生命值";
                case "Sanity": return "SAN值";
                case "SatietyChangePerDay": return "每日饱食度变化";
                case "HealthChangePerDay": return "每日生命值变化";
                case "SanityChangePerDay": return "每日SAN变化";
                default: return attrKey;
            }
        }

        private float GetBaseValueForAttribute(string attrKey, PlayerAttributes player)
        {
            switch (attrKey)
            {
                case "SatietyChangePerDay": return player.SatietyChangePerDay.GetBaseValue();
                case "HealthChangePerDay": return player.HealthChangePerDay.GetBaseValue();
                case "SanityChangePerDay": return player.SanityChangePerDay.GetBaseValue();
                default: return 0f;
            }
        }
    }
}