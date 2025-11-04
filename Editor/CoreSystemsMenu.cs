#if UNITY_EDITOR
using UnityEditor;

namespace EasyPack.Editor
{
    /// <summary>
    /// 核心系统菜单
    /// </summary>
    public static class CoreSystemsMenu
    {
        [MenuItem("EasyPack/CoreSystems/游戏属性(GameProperty)/管理器窗口")]
        public static void OpenGamePropertyManagerWindow()
        {
            var window = EditorWindow.GetWindow<GamePropertyManagerWindow>("GameProperty Manager");
            window.Show();
        }
    }
}
#endif
