using System;
using System.Reflection;
using System.Text.RegularExpressions;
using PluginHub.Runtime;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PluginHub.Editor
{
    public static class InspectorComponentContextMenu
    {
        #region BoxCollider
        [MenuItem("CONTEXT/BoxCollider/PH_自动为BoxCollider计算Size")]
        public static void ComputeBoxColliderSize(MenuCommand command)
        {
            //自动检查所有子物体中的网格渲染器计算size
            BoxCollider boxCollider = (BoxCollider)command.context;
            float minX = 999, maxX = -999, minY = 999, maxY = -999, minZ = 999, maxZ = -999;
            MeshFilter[] meshFilters = boxCollider.GetComponentsInChildren<MeshFilter>();
            for (int i = 0; i < meshFilters.Length; i++)
            {
                MeshFilter meshFilter = meshFilters[i];
                foreach (var aVertex in meshFilter.sharedMesh.vertices)
                {
                    Vector3 worldPos = meshFilter.transform.TransformPoint(aVertex);

                    if (worldPos.x < minX)
                        minX = worldPos.x;
                    if (worldPos.x > maxX)
                        maxX = worldPos.x;
                    if (worldPos.y < minY)
                        minY = worldPos.y;
                    if (worldPos.y > maxY)
                        maxY = worldPos.y;
                    if (worldPos.z < minZ)
                        minZ = worldPos.z;
                    if (worldPos.z > maxZ)
                        maxZ = worldPos.z;
                }
            }

            //计算碰撞器的添加位置
            Vector3 worldSize = new Vector3(maxX - minX, maxY - minY, maxZ - minZ);
            Vector3 worldSizePos = worldSize / 2f;
            // DebugEx.DrawAllCameraPoint(worldSizePos,.01f,99,Color.red);
            // DebugEx.DebugArrow(Vector3.zero,worldSizePos,Color.red,99);
            Matrix4x4 matrix4X4 = boxCollider.transform.worldToLocalMatrix; //拿到零件的转换矩阵
            matrix4X4.SetTRS(Vector3.zero, matrix4X4.rotation, matrix4X4.lossyScale); //舍弃矩阵的位移
            Vector3 localSizePos = matrix4X4.MultiplyPoint(worldSizePos); //计算本地碰撞体size
            Vector3 localSize = localSizePos * 2;
            // DebugEx.DrawAllCameraPoint(localSizePos,.01f,99,Color.green);
            // DebugEx.DebugArrow(Vector3.zero,localSizePos,Color.green,99);

            //boxCollider.size要传局部坐标系的size,加一个绝对值 避免为负
            boxCollider.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));

            //计算center
            boxCollider.center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, (minZ + maxZ) / 2f);
            boxCollider.center = boxCollider.transform.worldToLocalMatrix.MultiplyPoint(boxCollider.center);
        }
        #endregion

        #region Transform
        //Transform 组件的右键菜单
        [MenuItem("CONTEXT/Transform/PH_拷贝Scene相机的位置和旋转")]
        public static void CopySceneCameraTransform(MenuCommand command)
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                Transform transform = sceneView.camera.transform;
                //systemCopyBuffer
                GUIUtility.systemCopyBuffer = $"position:{transform.position},rotation:{transform.eulerAngles}";
            }
        }
        #endregion

        #region Component
        [MenuItem("CONTEXT/Component/PH_使用组件名命名游戏对象")]
        public static void CustomContextMenuRename(MenuCommand command)
        {
            Component component = (Component)command.context;
            GameObject gameObject = component.gameObject;
            Undo.RecordObject(gameObject, "rename obj");
            string componentAllName = component.GetType().ToString();
            //Debug.Log(componentAllName);
            gameObject.name = componentAllName.Substring(componentAllName.LastIndexOf(".") + 1);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); //标记场景脏
        }

        //换句话说就是，找到这个组件的mono脚本位置
        [MenuItem("CONTEXT/Component/PH_Ping这个Mono组件")]
        public static void CustomContextComponentPing(MenuCommand command)
        {
            Component component = (Component)command.context;
            Debug.Log(command.context);
            //get script asset
            MonoScript script = MonoScript.FromMonoBehaviour((MonoBehaviour)command.context);
            //ping this script
            EditorGUIUtility.PingObject(script);
        }

        [MenuItem("CONTEXT/Component/PH_复制组件名")]
        public static void CopyComponentName(MenuCommand command)
        {
            Component component = (Component)command.context;
            GameObject gameObject = component.gameObject;
            string componentAllName = component.GetType().ToString();
            //Debug.Log(componentAllName);
            EditorGUIUtility.systemCopyBuffer = componentAllName.Substring(componentAllName.LastIndexOf(".") + 1);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); //标记场景脏
        }

        [MenuItem("CONTEXT/Component/PH_智能赋值(TODO)")]
        public static void SmartAssign(MenuCommand command)
        {
            Debug.Log("Method TODO");
            Component component = (Component)command.context;

            Type type = component.GetType();
            Debug.Log(type);
            FieldInfo[] fieldInfos = type.GetFields();

            MethodInfo[] methodInfos = type.GetMethods();

            for (int i = 0; i < fieldInfos.Length; i++)
            {
                Debug.Log(fieldInfos[i].Name);
            }

            // for (int i = 0; i < methodInfos.Length; i++)
            // {
            //     Debug.Log(methodInfos[i].Name);
            // }


            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); //标记场景脏
        }
        #endregion

        #region TextMeshPro
        [MenuItem("CONTEXT/Button/PH_使用按钮文本命名游戏对象")]
        public static void CustomContextMenuRenameBtn(MenuCommand command)
        {
            Component component = (Component)command.context;
            GameObject gameObject = component.gameObject;
            string text = "";
            Text btnText = gameObject.GetComponentInChildren<Text>();
            if (btnText != null)
                text = btnText.text;
            TextMeshProUGUI textMeshProUGUI = gameObject.GetComponentInChildren<TextMeshProUGUI>();
            if (textMeshProUGUI != null)
                text = textMeshProUGUI.text;
            if (!string.IsNullOrWhiteSpace(text))
            {
                Undo.RecordObject(gameObject, "rename obj");
                gameObject.name = $"{text.Trim()} Btn";
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); //标记场景脏
            }
            else
            {
                Debug.LogWarning("该按钮无文本");
            }
        }

        [MenuItem("CONTEXT/Toggle/PH_使用开关文本命名游戏对象")]
        public static void CustomContextMenuRenameToggle(MenuCommand command)
        {
            Component component = (Toggle)command.context;
            GameObject gameObject = component.gameObject;
            string text = "";
            Text toggleText = gameObject.GetComponentInChildren<Text>();
            if (toggleText != null)
                text = toggleText.text;
            TextMeshProUGUI textMeshProUGUI = gameObject.GetComponentInChildren<TextMeshProUGUI>();
            if (textMeshProUGUI != null)
                text = textMeshProUGUI.text;

            if (!string.IsNullOrWhiteSpace(text))
            {
                Undo.RecordObject(gameObject, "rename obj");
                gameObject.name = $"{text} Toggle";
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); //标记场景脏
            }
            else
            {
                Debug.LogWarning("该开关无文本");
            }
        }

        [MenuItem("CONTEXT/Image/PH_使用Sprite名命名游戏对象", false)]
        public static void CustomContextMenuRenameImageUseSpriteName(MenuCommand command)
        {
            Image component = (Image)command.context;
            GameObject gameObject = component.gameObject;
            Undo.RecordObject(gameObject, "rename obj");
            //Debug.Log(componentAllName);
            gameObject.name = component.sprite.name;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); //标记场景脏
        }

        [MenuItem("CONTEXT/Image/PH_使用Sprite名命名游戏对象", true)] //验证函数，返回假会导致该按钮灰色不能点击。
        public static bool CustomContextMenuRenameImageUseSpriteNameValidata(MenuCommand command)
        {
            Image component = (Image)command.context;
            return component.sprite != null;
        }

        [MenuItem("CONTEXT/SpriteRenderer/PH_使用Sprite名命名游戏对象", false)]
        public static void CustomContextMenuRenameSpriteRendererUseSpriteName(MenuCommand command)
        {
            SpriteRenderer component = (SpriteRenderer)command.context;
            GameObject gameObject = component.gameObject;
            Undo.RecordObject(gameObject, "rename obj");
            //Debug.Log(componentAllName);
            gameObject.name = component.sprite.name;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); //标记场景脏
        }

        [MenuItem("CONTEXT/SpriteRenderer/PH_使用Sprite名命名游戏对象", true)] //验证函数，返回假会导致该按钮灰色不能点击。
        public static bool CustomContextMenuRenameSpriteRendererUseSpriteNameValidata(MenuCommand command)
        {
            SpriteRenderer component = (SpriteRenderer)command.context;
            return component.sprite != null;
        }

        [MenuItem("CONTEXT/Text/PH_替换文本：所有英文添加透明标签")]
        public static void CustomUGUITextMenuAddAlphaTag(MenuCommand command)
        {
            Text textComponent = (Text)command.context;

            //Regex _regex = new Regex("[^\u4e00-\u9fa5]+");//正则，匹配所有非汉字字符串
            Regex _regex = new Regex("[a-zA-Z]+"); //正则，匹配所有英文单词

            MatchCollection matchCollection = _regex.Matches(textComponent.text);
            string newText = textComponent.text;
            foreach (var matchResult in matchCollection)
            {
                string matchResultStr = matchResult.ToString();
                newText = newText.ReplaceFirst(matchResultStr, $"<color=#ff000000>{matchResultStr}</color>");
            }

            Undo.RecordObject(textComponent, "Change Text");
            //修改文本
            textComponent.text = newText;
        }

        [MenuItem("CONTEXT/TextMeshProUGUI/PH_使用TextMeshProUGUI文本命名游戏对象")]
        public static void changeText(MenuCommand command)
        {
            TextMeshProUGUI textComponent = (TextMeshProUGUI)command.context;

            Undo.RecordObject(textComponent, "Change Text");
            textComponent.gameObject.name = $"{textComponent.text} Label";
        }


        [MenuItem("CONTEXT/Text/PH_替换为TextMeshProUGUI")]
        public static void CustomUGUITextMenu(MenuCommand command)
        {
            Text oldText = (Text)command.context;
            GameObject gameObject = oldText.gameObject;
            Undo.DestroyObjectImmediate(oldText); //删除UGUI Text
            TextMeshProUGUI textMeshProUGUI = Undo.AddComponent<TextMeshProUGUI>(gameObject); //添加TMP Text
            textMeshProUGUI.color = oldText.color;
            textMeshProUGUI.text = oldText.text;
            textMeshProUGUI.fontSize = oldText.fontSize;
            textMeshProUGUI.font = Resources.Load<TMP_FontAsset>("msyh-SDF-CN-3500");
            // switch (oldText.alignment)
            // {
            //     case TextAnchor.UpperLeft:
            //         textMeshProUGUI.alignment = TextAlignmentOptions.TopLeft;
            //         break;
            //     case TextAnchor.UpperCenter:
            //         textMeshProUGUI.alignment = TextAlignmentOptions.TopLeft;
            //         break;
            // }
        }

        [MenuItem("CONTEXT/TextMeshProUGUI/PH_替换为UGUIText")]
        public static void CustomTextMeshProUGUIMenu(MenuCommand command)
        {
            TextMeshProUGUI oldText = (TextMeshProUGUI)command.context;
            GameObject gameObject = oldText.gameObject;
            Undo.DestroyObjectImmediate(oldText); //删除
            Text uguiText = Undo.AddComponent<Text>(gameObject); //添加Text
            uguiText.color = oldText.color;
            uguiText.text = oldText.text;
            uguiText.fontSize = (int)oldText.fontSize;
        }
        #endregion

        #region TerrainCollider
        //地形上的TerrainCollider组件命名
        [MenuItem("CONTEXT/TerrainCollider/PH_使用terrainData名称命名游戏对象")]
        public static void RenameObjectTerrainCollider(MenuCommand command)
        {
            TerrainCollider terrainCollider = (TerrainCollider)command.context;
            GameObject gameObject = terrainCollider.gameObject;
            Undo.RecordObject(gameObject, "rename obj");
            gameObject.name = terrainCollider.terrainData.name;
        }
        #endregion

        #region MeshRenderer
        [MenuItem("CONTEXT/MeshRenderer/PH_使用Mesh名称命名材质资产")]
        public static void RenameMatUseMeshName(MenuCommand command)
        {
            MeshRenderer meshRenderer = (MeshRenderer)command.context;
            MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
            GameObject gameObject = meshRenderer.gameObject;
            Undo.RecordObject(gameObject, "rename obj");
            //获取mesh的材质
            Material[] materials = meshRenderer.sharedMaterials;

            if (materials != null && materials.Length > 0)
            {
                //单个材质
                if (materials.Length == 1)
                {
                    Material material = materials[0];
                    if (material != null)
                    {
                        string assetPath = AssetDatabase.GetAssetPath(material);
                        string newName = $"M_{meshFilter.name}.mat";
                        string result = AssetDatabase.RenameAsset(assetPath, newName);
                        if (!string.IsNullOrWhiteSpace(result))
                        {
                            Debug.LogError(result);
                        }
                    }
                }
                else
                {
                    //多个材质
                    for (int i = 0; i < materials.Length; i++)
                    {
                        Material material = materials[i];
                        if (material != null)
                        {
                            string assetPath = AssetDatabase.GetAssetPath(material);
                            string newName = $"M_{meshFilter.name}_{i}.mat";
                            string result = AssetDatabase.RenameAsset(assetPath, newName);
                            if (!string.IsNullOrWhiteSpace(result))
                            {
                                Debug.LogError(result);
                            }
                        }
                    }
                }
            }
            else
            {
            }
        }
        #endregion

        #region MeshFilter
        [MenuItem("CONTEXT/MeshFilter/PH_复制Mesh路径")]
        public static void CopyMeshPath(MenuCommand command)
        {
            MeshFilter meshFilter = (MeshFilter)command.context;
            string assetPath = AssetDatabase.GetAssetPath(meshFilter.sharedMesh);
            EditorGUIUtility.systemCopyBuffer = assetPath;
        }
        #endregion

    }
}