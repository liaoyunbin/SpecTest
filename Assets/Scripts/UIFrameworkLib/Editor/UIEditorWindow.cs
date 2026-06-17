//using UnityEditor;
//using UnityEngine;

//namespace UIFrameworkLib.Editor
//{
//    /// <summary>
//    /// UI 编辑器调试窗口
//    /// 运行时查看 UI 栈状态、缓存统计、强制操作
//    /// </summary>
//    public class UIEditorWindow : EditorWindow
//    {
//        private Vector2 _scrollPos;
//        private int _selectedIndex;

//        [MenuItem("UIFrameworkLib/UI Debugger", priority = 100)]
//        private static void Open() => GetWindow<UIEditorWindow>("UI Debugger");

//        private void OnGUI()
//        {
//            var mgr = UIManager.Instance;

//            GUILayout.BeginVertical();
//            _scrollPos = GUILayout.BeginScrollView(_scrollPos);

//            DrawSection("=== Active UI ===", () =>
//            {
//                foreach (var ctx in mgr.GetAllActiveContexts())
//                {
//                    var state = ctx.StateMachine.CurrentState;
//                    var layer = ctx.Config.Layer;
//                    var time = Time.time - ctx.OpenTime;
//                    EditorGUILayout.LabelField(
//                        $"[{state}] {ctx.UIKey}  (Layer: {layer}, Open: {time:F1}s)",
//                        EditorStyles.label
//                    );
//                }
//            });

//            DrawSection("=== Normal Stack ===", () =>
//            {
//                for (int i = 0; i < mgr.NormalStack.Count; i++)
//                {
//                    var ctx = mgr.NormalStack[i];
//                    var state = ctx.StateMachine.CurrentState;
//                    EditorGUILayout.LabelField(
//                        $"  [{i}] {ctx.UIKey}  ({state})",
//                        EditorStyles.label
//                    );
//                }
//            });

//            DrawSection("=== Cache ===", () =>
//            {
//                EditorGUILayout.LabelField($"Cached Views: {mgr.CachedViewCount}");
//            });

//            DrawSection("=== Operations ===", () =>
//            {
//                if (GUILayout.Button("Close All UI", GUILayout.Height(30)))
//                {
//                    mgr.CloseAll();
//                    Debug.Log("[UIFrameworkLib] 已强制关闭所有 UI");
//                }

//                if (GUILayout.Button("Clear Cache", GUILayout.Height(30)))
//                {
//                    mgr.ClearCache();
//                    Debug.Log("[UIFrameworkLib] 已清空缓存");
//                }
//            });

//            DrawSection("=== Open UI (Manual) ===", () =>
//            {
//                var keys = mgr.ConfigLoader.GetAllKeys();
//                if (keys.Length > 0)
//                {
//                    _selectedIndex = EditorGUILayout.Popup(_selectedIndex, keys);
//                    if (GUILayout.Button($"Open [{keys[_selectedIndex]}]"))
//                    {
//                        mgr.Open(keys[_selectedIndex]);
//                    }
//                }
//                else
//                {
//                    EditorGUILayout.LabelField("无已加载的 UI 配置");
//                }
//            });

//            GUILayout.EndScrollView();
//            GUILayout.EndVertical();

//            // 自动刷新
//            if (EditorApplication.isPlaying)
//                Repaint();
//        }

//        private static void DrawSection(string title, System.Action content)
//        {
//            EditorGUILayout.Space(4);
//            GUILayout.Label(title, EditorStyles.boldLabel);
//            EditorGUILayout.Space(2);
//            content?.Invoke();
//            EditorGUILayout.Space(4);
//        }
//    }
//}
