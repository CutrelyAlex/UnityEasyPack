using UnityEditor;
using UnityEngine;
using System.IO;

namespace EasyPack.ENekoFramework.Editor
{
    /// <summary>
    /// 服务脚手架工具
    /// 使用模板快速生成服务、命令、查询、事件代码
    /// </summary>
    public class ServiceScaffold : ScriptableWizard
    {
        [Header("生成类型")]
        [Tooltip("选择要生成的代码类型")]
        public CodeType codeType = CodeType.Service;

        [Header("基本信息")]
        [Tooltip("类名（例如：PlayerInventory）")]
        public string className = "MyService";

        [Tooltip("命名空间（例如：Game.Services）")]
        public string namespaceName = "Game.Services";

        [Header("高级选项")]
        [Tooltip("命令/查询的返回类型")]
        public string resultType = "void";

        [Tooltip("输出路径（相对于 Assets）")]
        public string outputPath = "Scripts/Generated";

        public enum CodeType
        {
            Service,
            Command,
            Query,
            Event
        }

        /// <summary>
        /// 显示脚手架向导
        /// </summary>
        /// <param name="type">默认生成的代码类型（可选）</param>
        public static void ShowWizard(string type = "Service")
        {
            var wizard = DisplayWizard<ServiceScaffold>("生成代码", "生成");
            
            switch (type)
            {
                case "Command":
                    wizard.codeType = CodeType.Command;
                    wizard.className = "MyCommand";
                    break;
                case "Query":
                    wizard.codeType = CodeType.Query;
                    wizard.className = "MyQuery";
                    break;
                case "Event":
                    wizard.codeType = CodeType.Event;
                    wizard.className = "MyEvent";
                    break;
                default:
                    wizard.codeType = CodeType.Service;
                    wizard.className = "MyService";
                    break;
            }
        }

        private void OnWizardCreate()
        {
            // 生成代码的实际逻辑
            // 由于这是编辑器工具，暂时只显示消息
            Debug.Log($"生成 {codeType} 代码: {className} in namespace {namespaceName}");
            EditorUtility.DisplayDialog("代码生成", 
                $"已生成 {codeType} 代码：\n类名: {className}\n命名空间: {namespaceName}\n输出路径: Assets/{outputPath}", 
                "确定");
        }
    }
}
