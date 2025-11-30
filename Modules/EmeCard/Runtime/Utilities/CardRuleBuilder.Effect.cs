using System;
using System.Collections.Generic;

namespace EasyPack.EmeCardSystem
{
    /// <summary>
    ///     CardRuleBuilder - 效果执行部分
    /// </summary>
    public sealed partial class CardRuleBuilder
    {
        #region 效果执行 - 便捷语法糖

        /// <summary>移除匹配结果中指定标签的卡牌</summary>
        public CardRuleBuilder DoRemoveByTag(string tag, int? take = null) => DoRemove(SelectionRoot.MatchRoot,
            TargetScope.Matched, CardFilterMode.ByTag, tag, take);

        /// <summary>移除匹配结果中指定ID的卡牌</summary>
        public CardRuleBuilder DoRemoveById(string id, int? take = null) => DoRemove(SelectionRoot.MatchRoot,
            TargetScope.Matched, CardFilterMode.ById, id, take);

        /// <summary>移除容器子卡中指定标签的卡牌</summary>
        public CardRuleBuilder DoRemoveMatchRootChildByTag(string tag, int? take = null) => DoRemove(SelectionRoot.MatchRoot,
            TargetScope.Children, CardFilterMode.ByTag, tag, take);

        /// <summary>移除容器子卡中指定ID的卡牌</summary>
        public CardRuleBuilder DoRemoveMatchRootChildById(string id, int? take = null) => DoRemove(SelectionRoot.MatchRoot,
            TargetScope.Children, CardFilterMode.ById, id, take);

        /// <summary>给匹配结果添加标签</summary>
        public CardRuleBuilder DoAddTagToMatched(string tag) =>
            DoAddTag(tag);

        /// <summary>给源卡牌自身添加标签</summary>
        public CardRuleBuilder DoAddTagToSource(string tag)
        {
            return DoInvoke((ctx, matched) => ctx.Source.AddTag(tag));
        }

        /// <summary>给容器卡牌自身添加标签</summary>
        public CardRuleBuilder DoAddTagToMatchRoot(string tag)
        {
            return DoInvoke((ctx, matched) => ctx.MatchRoot.AddTag(tag));
        }

        /// <summary>给容器子卡中指定标签的卡牌添加新标签</summary>
        public CardRuleBuilder DoAddTagToMatchRootChildByTag(string targetTag, string newTag, int? take = null) =>
            DoAddTag(newTag, SelectionRoot.MatchRoot, TargetScope.Children, CardFilterMode.ByTag, targetTag,
                take);

        /// <summary>给容器子卡中指定ID的卡牌添加标签</summary>
        public CardRuleBuilder DoAddTagToMatchRootChildById(string targetId, string newTag, int? take = null) => DoAddTag(newTag,
            SelectionRoot.MatchRoot, TargetScope.Children, CardFilterMode.ById, targetId, take);

        /// <summary>给源卡牌自身移除标签</summary>
        public CardRuleBuilder DoRemoveTagFromSource(string tag)
        {
            return DoInvoke((ctx, matched) => ctx.Source.RemoveTag(tag));
        }

        /// <summary>给容器卡牌自身移除标签</summary>
        public CardRuleBuilder DoRemoveTagFromMatchRoot(string tag)
        {
            return DoInvoke((ctx, matched) => ctx.MatchRoot.RemoveTag(tag));
        }

        /// <summary>修改匹配结果中指定标签的卡牌的属性</summary>
        public CardRuleBuilder DoModifyTag(
            string tag,
            string propertyName,
            float value,
            ModifyPropertyEffect.Mode mode = ModifyPropertyEffect.Mode.AddToBase,
            Func<CardRuleContext,float> valueFunc=null,
            int? take = null)=>DoModify(propertyName,value, mode, SelectionRoot.MatchRoot, TargetScope.Matched,
                CardFilterMode.ByTag, tag,valueFunc, take);

        /// <summary>修改匹配结果的属性</summary>
        public CardRuleBuilder DoModifyMatched(
            string propertyName,
            float value,
            ModifyPropertyEffect.Mode mode = ModifyPropertyEffect.Mode.AddToBase) =>
            DoModify(propertyName,value, mode);

        /// <summary>批量触发匹配卡牌的自定义事件</summary>
        public CardRuleBuilder DoBatchCustom(string eventId, Func<CardRuleContext, object> data = null,
                                             bool haveSource = false)
        {
            return DoInvoke((ctx, matched) =>
            {
                object newData = data == null ? ctx.Event.DataObject : data.Invoke(ctx);
                if (haveSource) ctx.Source.RaiseEvent(eventId, newData);

                foreach (Card card in matched)
                {
                    card.RaiseEvent(eventId, newData);
                }
            });
        }

        #endregion

        #region 效果执行 - EffectRoot 便捷语法糖

        /// <summary>给效果根卡牌自身添加标签</summary>
        public CardRuleBuilder DoAddTagToEffectRoot(string tag)
        {
            return DoInvoke((ctx, matched) => ctx.EffectRoot?.AddTag(tag));
        }

        /// <summary>给效果根卡牌自身移除标签</summary>
        public CardRuleBuilder DoRemoveTagFromEffectRoot(string tag)
        {
            return DoInvoke((ctx, matched) => ctx.EffectRoot?.RemoveTag(tag));
        }

        /// <summary>移除效果根子卡中指定标签的卡牌</summary>
        public CardRuleBuilder DoRemoveEffectRootChildByTag(string tag, int? take = null)
        {
            return DoInvoke((ctx, matched) =>
            {
                if (ctx.EffectRoot?.Children == null) return;
                int count = 0;
                var toRemove = new List<Card>();
                foreach (Card child in ctx.EffectRoot.Children)
                {
                    if (child.HasTag(tag))
                    {
                        toRemove.Add(child);
                        count++;
                        if (take.HasValue && count >= take.Value) break;
                    }
                }
                foreach (Card card in toRemove)
                {
                    ctx.EffectRoot.RemoveChild(card);
                }
            });
        }

        /// <summary>移除效果根子卡中指定ID的卡牌</summary>
        public CardRuleBuilder DoRemoveEffectRootChildById(string id, int? take = null)
        {
            return DoInvoke((ctx, matched) =>
            {
                if (ctx.EffectRoot?.Children == null) return;
                int count = 0;
                var toRemove = new List<Card>();
                foreach (Card child in ctx.EffectRoot.Children)
                {
                    if (string.Equals(child.Id, id, StringComparison.Ordinal))
                    {
                        toRemove.Add(child);
                        count++;
                        if (take.HasValue && count >= take.Value) break;
                    }
                }
                foreach (Card card in toRemove)
                {
                    ctx.EffectRoot.RemoveChild(card);
                }
            });
        }

        /// <summary>给效果根子卡中指定标签的卡牌添加新标签</summary>
        public CardRuleBuilder DoAddTagToEffectRootChildByTag(string targetTag, string newTag, int? take = null)
        {
            return DoInvoke((ctx, matched) =>
            {
                if (ctx.EffectRoot?.Children == null) return;
                int count = 0;
                foreach (Card child in ctx.EffectRoot.Children)
                {
                    if (child.HasTag(targetTag))
                    {
                        child.AddTag(newTag);
                        count++;
                        if (take.HasValue && count >= take.Value) break;
                    }
                }
            });
        }

        /// <summary>给效果根子卡中指定ID的卡牌添加标签</summary>
        public CardRuleBuilder DoAddTagToEffectRootChildById(string targetId, string newTag, int? take = null)
        {
            return DoInvoke((ctx, matched) =>
            {
                if (ctx.EffectRoot?.Children == null) return;
                int count = 0;
                foreach (Card child in ctx.EffectRoot.Children)
                {
                    if (string.Equals(child.Id, targetId, StringComparison.Ordinal))
                    {
                        child.AddTag(newTag);
                        count++;
                        if (take.HasValue && count >= take.Value) break;
                    }
                }
            });
        }

        /// <summary>修改效果根子卡中指定标签卡牌的属性（仅支持 AddToBase/SetBase 模式）</summary>
        public CardRuleBuilder DoModifyEffectRootChildByTag(
            string tag,
            string propertyName,
            float value,
            ModifyPropertyEffect.Mode mode = ModifyPropertyEffect.Mode.AddToBase,
            int? take = null)
        {
            return DoInvoke((ctx, matched) =>
            {
                if (ctx.EffectRoot?.Children == null) return;
                int count = 0;
                foreach (Card child in ctx.EffectRoot.Children)
                {
                    if (child.HasTag(tag))
                    {
                        var prop = child.GetProperty(propertyName);
                        if (prop != null)
                        {
                            switch (mode)
                            {
                                case ModifyPropertyEffect.Mode.AddToBase:
                                    prop.SetBaseValue(prop.GetBaseValue() + value);
                                    break;
                                case ModifyPropertyEffect.Mode.SetBase:
                                    prop.SetBaseValue(value);
                                    break;
                                // AddModifier/RemoveModifier 需要 IModifier，请使用 DoModify 完整版本
                            }
                        }
                        count++;
                        if (take.HasValue && count >= take.Value) break;
                    }
                }
            });
        }

        /// <summary>在效果根下创建卡牌作为子卡</summary>
        public CardRuleBuilder DoCreateInEffectRoot(string cardId, int count = 1)
        {
            return DoInvoke((ctx, matched) =>
            {
                if (ctx.EffectRoot == null || ctx.Engine == null) return;
                for (int i = 0; i < count; i++)
                {
                    Card newCard = ctx.Engine.CreateCard(cardId);
                    if (newCard != null)
                    {
                        ctx.EffectRoot.AddChild(newCard);
                    }
                }
            });
        }

        /// <summary>在效果根下创建多种卡牌作为子卡</summary>
        public CardRuleBuilder DoCreateInEffectRoot(params (string cardId, int count)[] cards)
        {
            return DoInvoke((ctx, matched) =>
            {
                if (ctx.EffectRoot == null || ctx.Engine == null) return;
                foreach (var (cardId, count) in cards)
                {
                    for (int i = 0; i < count; i++)
                    {
                        Card newCard = ctx.Engine.CreateCard(cardId);
                        if (newCard != null)
                        {
                            ctx.EffectRoot.AddChild(newCard);
                        }
                    }
                }
            });
        }

        #endregion
    }
}
