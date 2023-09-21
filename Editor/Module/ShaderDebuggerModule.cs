using System.Collections;
using System.Collections.Generic;
using PluginHub;
using UnityEditor;
using UnityEngine;

namespace PluginHub.Module
{
    //shader 调试器模块
    //取色 用于调试shader
    public class ShaderDebuggerModule : PluginHubModuleBase
    {
        enum PickMode
        {
            None, //不进行捕获颜色
            SceneView, //捕获场景视图
            GameView_InDevelop, //捕获游戏视图
        }

        private PickMode _pickMode = PickMode.None;

        private Camera _sceneCamera; //场景相机
        private Camera _gameCamera; //游戏相机
        private Texture2D screenshotTex;
        private Color _colorPicked; //拾取到的颜色

        //左上角是0，0
        private Vector2 mousePositionInSceneView;
        private Vector2 mousePositionInGameView;

        private GUIStyle lableStyle;

        private GUIContent toggleContent =
            new GUIContent("Enable Pick SceneView", "关闭该开关，以使用吸管在任意位置取色，开启该开关，鼠标在场景视图中取色");


        protected override void DrawModuleDebug()
        {
            base.DrawModuleDebug();

            _sceneCamera = EditorGUILayout.ObjectField("Scene Camera", _sceneCamera, typeof(Camera), true) as Camera;
            _gameCamera = EditorGUILayout.ObjectField("Game Camera", _gameCamera, typeof(Camera), true) as Camera;

            Vector2 sceneCameraScreenSize = new Vector2(_sceneCamera.pixelWidth, _sceneCamera.pixelHeight);
            GUILayout.Label($"Scene Camera Resolution : {sceneCameraScreenSize.x}x{sceneCameraScreenSize.y}");
            GUILayout.Label($"Mouse Position In SceneView : {mousePositionInSceneView.x},{mousePositionInSceneView.y}");

            mousePositionInGameView = Input.mousePosition;
            GUILayout.Label(
                $"Mouse Position In GameView: {mousePositionInGameView.x:F0},{mousePositionInGameView.y:F0}");

            // texture = EditorGUILayout.ObjectField(texture, typeof(Texture2D), true,GUILayout.Width(_gameCamera.aspect * 100f),GUILayout.Height(100)) as Texture2D;
            // GUILayout.Label(Time.frameCount.ToString());

            GUILayout.Space(50);
        }

        protected override void DrawGuiContent()
        {
            //init
            if (lableStyle == null)
            {
                GUISkin skin = GameObject.Instantiate<GUISkin>(GUI.skin);
                lableStyle = skin.label;
                lableStyle.alignment = TextAnchor.MiddleLeft;
            }

            //get component
            if (_sceneCamera == null)
            {
                if (SceneView.lastActiveSceneView != null)
                    _sceneCamera = SceneView.lastActiveSceneView.camera;
            }

            if (_sceneCamera == null) return;

            if (_gameCamera == null)
                _gameCamera = Camera.main;
            if (_gameCamera == null) return;


            Vector2 sceneCameraScreenSize = new Vector2(_sceneCamera.pixelWidth, _sceneCamera.pixelHeight);
            Vector2 gameCameraScreenSize = new Vector2(Screen.width, Screen.height);

            if (_pickMode == PickMode.SceneView)
            {
                MakeTextureAvailable(sceneCameraScreenSize);

                //do re screen shot
                var rtTmp = RenderTexture.active;
                _sceneCamera.Render();
                RenderTexture.active = _sceneCamera.targetTexture;
                screenshotTex.ReadPixels(new Rect(0, 0, (int)sceneCameraScreenSize.x, (int)sceneCameraScreenSize.y), 0,
                    0);
                screenshotTex.Apply();
                RenderTexture.active = rtTmp;

                PickColorFromTexture(mousePositionInSceneView);
            }
            else if (_pickMode == PickMode.GameView_InDevelop)
            {
                //_gameCamera.Render();//会把窗口变成相机纹理的样子  有问题
                MakeTextureAvailable(gameCameraScreenSize);
                screenshotTex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
                screenshotTex.Apply();
                PickColorFromTexture(mousePositionInGameView);
            }


            _pickMode = (PickMode)EditorGUILayout.EnumPopup("Pick Mode", _pickMode);

            if (_pickMode == PickMode.SceneView && !PluginHubWindow.alwaysRefreshGUI)
            {
                //
                EditorGUILayout.HelpBox(
                    "If you want to pick color in sceneview at real-time use this feature, please set alwaysRefreshGUI to true in PluginHubWindow top",
                    MessageType.Warning);
            }



            GUILayout.BeginVertical("Box");
            {
                GUILayout.Label("Color Infos :");
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Label("Color");
                    GUILayout.FlexibleSpace();
                    _colorPicked = EditorGUILayout.ColorField(_colorPicked, GUILayout.Width(100), GUILayout.Height(80));
                }
                GUILayout.EndHorizontal();

                float width = PluginHubWindow.Window.position.width / 3f - 20;
                lableStyle.fixedWidth = width;

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Button("R", lableStyle);
                    GUILayout.Button("G", lableStyle);
                    GUILayout.Button("B", lableStyle);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Button(_colorPicked.r.ToString("F3"), lableStyle);
                    GUILayout.Button(_colorPicked.g.ToString("F3"), lableStyle);
                    GUILayout.Button(_colorPicked.b.ToString("F3"), lableStyle);
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Button((_colorPicked.r * 255f).ToString(), lableStyle);
                    GUILayout.Button((_colorPicked.g * 255f).ToString(), lableStyle);
                    GUILayout.Button((_colorPicked.b * 255f).ToString(), lableStyle);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();


        }

        private void MakeTextureAvailable(Vector2 resolution)
        {
            //check size
            if (screenshotTex != null)
            {
                if (screenshotTex.width != resolution.x || screenshotTex.height != resolution.y)
                {
                    GameObject.DestroyImmediate(screenshotTex);
                    screenshotTex = null;
                }
            }

            if (screenshotTex == null)
                screenshotTex = new Texture2D((int)resolution.x, (int)resolution.y, TextureFormat.RGB24, false);
        }

        private void PickColorFromTexture(Vector2 position)
        {
            //取色
            //clamp  避免越界
            position.x = Mathf.Clamp(position.x, 0, (int)screenshotTex.width);
            position.y = Mathf.Clamp(position.y, 0, (int)screenshotTex.height);
            _colorPicked = screenshotTex.GetPixel((int)position.x, (int)position.y);
        }

        //画场景GUi
        public override bool OnSceneGUI(SceneView sceneView)
        {
            if (_sceneCamera == null) return false;

            //计算并储存鼠标在场景视图中的坐标
            mousePositionInSceneView = Event.current.mousePosition;

            //画一个十字
            if (_pickMode == PickMode.SceneView)
            {
                Handles.BeginGUI();
                // int crossHairSize = 50;
                // GUI.Label(new Rect(mousePositionInSceneView.x-crossHairSize/2f ,mousePositionInSceneView.y-crossHairSize/2f, crossHairSize,crossHairSize), PHFunc.Icon("CrossIcon",""));
                GUI.Label(new Rect(mousePositionInSceneView.x, mousePositionInSceneView.y, 230, 50),
                    "ShaderDebugger pick scene view color");
                Handles.EndGUI();
            }

            //invert y
            mousePositionInSceneView.y = _sceneCamera.pixelHeight - mousePositionInSceneView.y;
            mousePositionInSceneView.x = (int)mousePositionInSceneView.x;
            mousePositionInSceneView.y = (int)mousePositionInSceneView.y;

            return true;
        }

    }
}
