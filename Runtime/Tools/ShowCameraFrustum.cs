#if UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

[CustomEditor(typeof(ShowCameraFrustum))]
public class CameraHelperScriptEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // base.OnInspectorGUI();
        ShowCameraFrustum showCameraFrustum = (ShowCameraFrustum) target;

        showCameraFrustum.showInRuntime = EditorGUILayout.Toggle("Show In Runtime", showCameraFrustum.showInRuntime);
        showCameraFrustum.drawFrustum = EditorGUILayout.Toggle("Draw Frustum", showCameraFrustum.drawFrustum);

        EditorGUI.indentLevel++;
        showCameraFrustum.meshAlpha = EditorGUILayout.Slider("Mesh Alpha", showCameraFrustum.meshAlpha, 0, 1);
        showCameraFrustum.nearFarPlane = EditorGUILayout.Toggle("Near Far Plane", showCameraFrustum.nearFarPlane);
        EditorGUI.indentLevel--;

        showCameraFrustum.drawCamDirMesh = EditorGUILayout.Toggle("Draw Cam Dir Mesh", showCameraFrustum.drawCamDirMesh);
        EditorGUI.indentLevel++;
        showCameraFrustum.camDirMeshWidth = EditorGUILayout.Slider("Cam Dir Mesh Width", showCameraFrustum.camDirMeshWidth, 0.01f, 0.03f);
        EditorGUI.indentLevel--;

        //绘制debug信息
        Camera targetCamera = showCameraFrustum.targetCamera;
        if (targetCamera.transform.localScale != Vector3.one)
            EditorGUILayout.HelpBox("相机的缩放不是1，可能会导致视锥体显示不正确。", MessageType.Warning);

        // Vector3[] cameraFrustumPoss = showCameraFrustum.CaculateCameraFrustumLocalPositions(targetCamera.orthographic,targetCamera.orthographicSize,targetCamera.aspect);
        // for (int i = 0; i < cameraFrustumPoss.Length; i++)
        // {
        //     GUILayout.Label($"cameraFrustumPoss[{i}] = {cameraFrustumPoss[i]}");
        //     cameraFrustumPoss[i] = targetCamera.transform.TransformPoint(cameraFrustumPoss[i]);
        //     GUILayout.Label($"cameraFrustumPoss[{i}] = {cameraFrustumPoss[i]}");
        // }

        //save changes
        if (GUI.changed)
        {
            EditorUtility.SetDirty(showCameraFrustum);
            showCameraFrustum.meshDirty = true;
        }
    }
}


//该脚本使得即使不选择相机对象，也时刻显示相机的视锥体，以作为其他工作的辅助。
//该脚本仅为编辑器工具，Build后无实际作用。
[RequireComponent(typeof(Camera))]
[ExecuteInEditMode]
public class ShowCameraFrustum : MonoBehaviour
{
    public bool showInRuntime = false;

    [Range(0,1)]
    public float meshAlpha = .7f;
    //是否绘制相机视锥体网格
    public bool drawFrustum = true;
    public bool nearFarPlane = false; //是否绘制近远平面， 避免挡住game视图相机
    //是否绘制表示相机方向的网格
    public bool drawCamDirMesh = false;
    [Range(0,1)]
    public float camDirMeshWidth = 0.01f;


    private Material materialWithoutCull;
    private Material materialWithCull;


    private Camera _targetCamera;
    public Camera targetCamera
    {
        get
        {
            if (_targetCamera == null)
                _targetCamera = GetComponent<Camera>();
            return _targetCamera;
        }
    }
    
    public bool meshDirty { get; set; } = true;
    private Mesh frustumMesh;
    private Mesh camDirMesh;

    private void Start()
    {
        if (!Application.isEditor)//该脚本仅为编辑器工具，Build后无实际作用。
        {
            Destroy(this);
        }
    }

    private void Update()
    {
        //show frustum
        if (base.enabled && (showInRuntime || !Application.isPlaying))
        {
            DrawMesh();
        }
    }

    //计算相机视锥8个关键点坐标，坐标是本地坐标。
    public Vector3[] CaculateCameraFrustumLocalPositions(bool orthographic,float size,float aspect)
    {
        //计算相机视锥8个关键点坐标
        Vector3[] cameraFrustumPoss = new Vector3[8];
        if (orthographic)//正交相机
        {
            float orthographicSize = size;
            float orthographicHalfWidth = orthographicSize * aspect;

            cameraFrustumPoss[0] = new Vector3(-orthographicHalfWidth, orthographicSize, targetCamera.nearClipPlane);
            cameraFrustumPoss[1] = new Vector3(orthographicHalfWidth, orthographicSize, targetCamera.nearClipPlane);
            cameraFrustumPoss[2] = new Vector3(orthographicHalfWidth, -orthographicSize, targetCamera.nearClipPlane);
            cameraFrustumPoss[3] = new Vector3(-orthographicHalfWidth, -orthographicSize, targetCamera.nearClipPlane);

            cameraFrustumPoss[4] = new Vector3(-orthographicHalfWidth, orthographicSize, targetCamera.farClipPlane);
            cameraFrustumPoss[5] = new Vector3(orthographicHalfWidth, orthographicSize, targetCamera.farClipPlane);
            cameraFrustumPoss[6] = new Vector3(orthographicHalfWidth, -orthographicSize, targetCamera.farClipPlane);
            cameraFrustumPoss[7] = new Vector3(-orthographicHalfWidth, -orthographicSize, targetCamera.farClipPlane);
        }
        else//透视相机
        {
            float nearPlaneHalfHeight = targetCamera.nearClipPlane * Mathf.Tan(targetCamera.fieldOfView / 2f * Mathf.Deg2Rad);
            float nearPlaneHalfWidth = nearPlaneHalfHeight * aspect;
            float farPlaneHalfHeight = targetCamera.farClipPlane * Mathf.Tan(targetCamera.fieldOfView / 2f * Mathf.Deg2Rad);
            float farPlaneHalfWidth = farPlaneHalfHeight * aspect;

            cameraFrustumPoss[0] = new Vector3(-nearPlaneHalfWidth, nearPlaneHalfHeight, targetCamera.nearClipPlane);
            cameraFrustumPoss[1] = new Vector3(nearPlaneHalfWidth, nearPlaneHalfHeight, targetCamera.nearClipPlane);
            cameraFrustumPoss[2] = new Vector3(nearPlaneHalfWidth, -nearPlaneHalfHeight, targetCamera.nearClipPlane);
            cameraFrustumPoss[3] = new Vector3(-nearPlaneHalfWidth, -nearPlaneHalfHeight, targetCamera.nearClipPlane);

            cameraFrustumPoss[4] = new Vector3(-farPlaneHalfWidth, farPlaneHalfHeight, targetCamera.farClipPlane);
            cameraFrustumPoss[5] = new Vector3(farPlaneHalfWidth, farPlaneHalfHeight, targetCamera.farClipPlane);
            cameraFrustumPoss[6] = new Vector3(farPlaneHalfWidth, -farPlaneHalfHeight, targetCamera.farClipPlane);
            cameraFrustumPoss[7] = new Vector3(-farPlaneHalfWidth, -farPlaneHalfHeight, targetCamera.farClipPlane);
        }
        return cameraFrustumPoss;
    }

    //绘制相机的 视锥体网格 以及 视线网格
    private void DrawMesh()
    {
        if (meshDirty)//如果mesh不存在或者mesh需要更新
        {
            meshDirty = false;

            PrepareMesh();
        }
        Matrix4x4 matrix4X4 = Matrix4x4.TRS(targetCamera.transform.position, targetCamera.transform.rotation, Vector3.one);
        //绘制相机视锥体网格
        if (drawFrustum)
        {
            materialWithoutCull.SetFloat("_Alpha", meshAlpha);
            Graphics.DrawMesh(frustumMesh, matrix4X4, materialWithoutCull, 0);
        }
        //绘制相机视线网格
        if (drawCamDirMesh)
        {
            materialWithCull.SetFloat("_Alpha", meshAlpha);
            Graphics.DrawMesh(camDirMesh, matrix4X4, materialWithCull, 0);
        }
    }

    private void PrepareMesh()
    {
        //如果材质不存在，创建材质
        if (materialWithoutCull == null || materialWithCull == null)
        {
            Shader shader =ShaderUtil.CreateShaderAsset(shaderCode_WithoutCull.Replace("\\#", "#"));
            shader.hideFlags = HideFlags.HideAndDontSave;
            materialWithoutCull = new Material(shader);
            materialWithoutCull.hideFlags = HideFlags.HideAndDontSave;
            Shader shaderCull =ShaderUtil.CreateShaderAsset(shaderCode_Cull.Replace("\\#", "#"));
            shaderCull.hideFlags = HideFlags.HideAndDontSave;
            materialWithCull = new Material(shaderCull);
            materialWithCull.hideFlags = HideFlags.HideAndDontSave;
        }
        //如果mesh不存在，创建mesh
        if (frustumMesh == null)
        {
            frustumMesh = new Mesh();
            frustumMesh.MarkDynamic();
        }
        Vector3[] cameraFrustumPoss = CaculateCameraFrustumLocalPositions(targetCamera.orthographic,targetCamera.orthographicSize,targetCamera.aspect);
        frustumMesh.vertices = new Vector3[]
        {
            cameraFrustumPoss[0],cameraFrustumPoss[1],cameraFrustumPoss[2],cameraFrustumPoss[3],//0123
            cameraFrustumPoss[1],cameraFrustumPoss[5],cameraFrustumPoss[6],cameraFrustumPoss[2],//4567
            cameraFrustumPoss[4],cameraFrustumPoss[0],cameraFrustumPoss[3],cameraFrustumPoss[7],//891011
            cameraFrustumPoss[4],cameraFrustumPoss[5],cameraFrustumPoss[1],cameraFrustumPoss[0],//12131415
            cameraFrustumPoss[3],cameraFrustumPoss[2],cameraFrustumPoss[6],cameraFrustumPoss[7],//16171819
            cameraFrustumPoss[5],cameraFrustumPoss[4],cameraFrustumPoss[7],cameraFrustumPoss[6],//20212223
        };
        if (nearFarPlane)
        {
            frustumMesh.triangles = new int[]
            {
                0,1,2,2,3,0,
                4,5,6,6,7,4,
                8,9,10,10,11,8,
                12,13,14,14,15,12,
                16,17,18,18,19,16,
                20,21,22,22,23,20,
            };
        }
        else
        {
            frustumMesh.triangles = new int[]
            {
                4,5,6,6,7,4,
                8,9,10,10,11,8,
                12,13,14,14,15,12,
                16,17,18,18,19,16,
            };
        }
        frustumMesh.RecalculateNormals();
        frustumMesh.RecalculateBounds();
        frustumMesh.RecalculateTangents();

        //如果mesh不存在，创建mesh
        if (camDirMesh == null)
        {
            camDirMesh = new Mesh();
            camDirMesh.MarkDynamic();
        }

        cameraFrustumPoss = CaculateCameraFrustumLocalPositions(true, camDirMeshWidth, 1);
        camDirMesh.vertices = new Vector3[]
        {
            cameraFrustumPoss[0],cameraFrustumPoss[1],cameraFrustumPoss[2],cameraFrustumPoss[3],//0123
            cameraFrustumPoss[1],cameraFrustumPoss[5],cameraFrustumPoss[6],cameraFrustumPoss[2],//4567
            cameraFrustumPoss[4],cameraFrustumPoss[0],cameraFrustumPoss[3],cameraFrustumPoss[7],//891011
            cameraFrustumPoss[4],cameraFrustumPoss[5],cameraFrustumPoss[1],cameraFrustumPoss[0],//12131415
            cameraFrustumPoss[3],cameraFrustumPoss[2],cameraFrustumPoss[6],cameraFrustumPoss[7],//16171819
            cameraFrustumPoss[5],cameraFrustumPoss[4],cameraFrustumPoss[7],cameraFrustumPoss[6],//20212223
        };
        camDirMesh.triangles = new int[]
        {
            4,5,6,6,7,4,
            8,9,10,10,11,8,
            12,13,14,14,15,12,
            16,17,18,18,19,16,
        };
        camDirMesh.RecalculateNormals();
        camDirMesh.RecalculateBounds();
        camDirMesh.RecalculateTangents();
    }

    private void OnDrawGizmos()
    {
        if (!base.enabled) return;


        //选中了相机，不画视锥，系统会画
        if (Selection.gameObjects != null && Selection.gameObjects.Length > 0 &&
            Selection.gameObjects[0] == targetCamera.gameObject)
            return;
        if(!drawFrustum)
            return;

        Vector3[] cameraFrustumPoss = CaculateCameraFrustumLocalPositions(targetCamera.orthographic,targetCamera.orthographicSize,targetCamera.aspect);
        for (int i = 0; i < cameraFrustumPoss.Length; i++)
            cameraFrustumPoss[i] = targetCamera.transform.TransformPoint(cameraFrustumPoss[i]);
        //画视锥关键线条
        // Gizmos.color = new Color(0.5f, 0.5f, 0.5f, .5f);
        // Gizmos.color = Color.white;
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(cameraFrustumPoss[0],cameraFrustumPoss[1]);
        Gizmos.DrawLine(cameraFrustumPoss[1],cameraFrustumPoss[2]);
        Gizmos.DrawLine(cameraFrustumPoss[2],cameraFrustumPoss[3]);
        Gizmos.DrawLine(cameraFrustumPoss[3],cameraFrustumPoss[0]);
        Gizmos.DrawLine(cameraFrustumPoss[4],cameraFrustumPoss[5]);
        Gizmos.DrawLine(cameraFrustumPoss[5],cameraFrustumPoss[6]);
        Gizmos.DrawLine(cameraFrustumPoss[6],cameraFrustumPoss[7]);
        Gizmos.DrawLine(cameraFrustumPoss[7],cameraFrustumPoss[4]);
        Gizmos.DrawLine(cameraFrustumPoss[0],cameraFrustumPoss[4]);
        Gizmos.DrawLine(cameraFrustumPoss[1],cameraFrustumPoss[5]);
        Gizmos.DrawLine(cameraFrustumPoss[2],cameraFrustumPoss[6]);
        Gizmos.DrawLine(cameraFrustumPoss[3],cameraFrustumPoss[7]);
        Gizmos.color = Color.white;

    }

    #region Shader代码
    private string shaderCode_WithoutCull = @"
//黑色半透明和一个调节透明度的滑动条，经过测试，此shader支持内置和URP
Shader ""Hidden/TransparentBlackWithSlider""
{
    Properties
    {
        //透明度
        _Alpha(""Alpha"", Range( 0 , 1)) = 0.5
    }
    SubShader
    {
        Tags { ""RenderType"" = ""Transparent""  ""Queue"" = ""Transparent"" }
        LOD 100
        Cull off
        zWrite off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            \#include ""UnityCG.cginc""

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            float _Alpha;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(0,0,0,_Alpha);
            }
            ENDCG
        }
    }
}
";

    private string shaderCode_Cull = @"
//黑色半透明和一个调节透明度的滑动条，经过测试，此shader支持内置和URP
Shader ""Hidden/TransparentBlackWithSlider""
{
    Properties
    {
        //透明度
        _Alpha(""Alpha"", Range( 0 , 1)) = 0.5
    }
    SubShader
    {
        Tags { ""RenderType"" = ""Transparent""  ""Queue"" = ""Transparent"" }
        LOD 100
        zWrite off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            \#include ""UnityCG.cginc""

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            float _Alpha;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(0,0,0,_Alpha);
            }
            ENDCG
        }
    }
}
";

    #endregion
}

#endif
