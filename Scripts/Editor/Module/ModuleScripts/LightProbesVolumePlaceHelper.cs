using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PluginHub.Editor
{
    [CustomEditor(typeof(LightProbesVolumePlaceHelper)), CanEditMultipleObjects]
    //编辑器GUI
    public class LightProbesVolumePlaceHelperEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            Color oldColor = GUI.color;

            GUI.color = Color.green;
            GUILayout.Label("GREEN: Valid point waiting to be placed");
            GUI.color = Color.yellow;
            GUILayout.Label("YELLOW: already have a LightProbes in LightProbesGroup");
            GUI.color = Color.red;
            GUILayout.Label("RED: invalid point");
            GUI.color = Color.cyan;
            GUILayout.Label("CYAN: RayTestOrigin");


            GUI.color = oldColor;

            var volume = (LightProbesVolumePlaceHelper)target;

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Create LightProbes"))
                {
                    volume.ExePlace();
                }

                if (GUILayout.Button("Remove LightProbes"))
                {
                    LightProbeGroup lightProbeGroup = volume.GetComponent<LightProbeGroup>();
                    if (lightProbeGroup != null)
                    {
                        lightProbeGroup.probePositions = new Vector3[] { };
                    }
                }
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Disable Other Gizmos"))
            {
                LightProbesVolumePlaceHelper[] helpers = FindObjectsByType<LightProbesVolumePlaceHelper>(FindObjectsSortMode.None);
                foreach (var helper in helpers)
                {
                    helper.drawPreview = false;
                }

                (target as LightProbesVolumePlaceHelper).drawPreview = true;
            }

            GUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("Disable All Gizmos"))
                {
                    LightProbesVolumePlaceHelper[] helpers = FindObjectsByType<LightProbesVolumePlaceHelper>(FindObjectsSortMode.None);
                    foreach (var helper in helpers)
                    {
                        helper.drawPreview = false;
                    }
                }

                if (GUILayout.Button("Enable All Gizmos"))
                {
                    LightProbesVolumePlaceHelper[] helpers = FindObjectsByType<LightProbesVolumePlaceHelper>(FindObjectsSortMode.None);
                    foreach (var helper in helpers)
                    {
                        helper.drawPreview = true;
                    }
                }
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Re Create all"))
            {
                LightProbesVolumePlaceHelper[] lightProbesVolumePlaceHelpers =
                    FindObjectsByType<LightProbesVolumePlaceHelper>(FindObjectsSortMode.None);
                for (int i = 0; i < lightProbesVolumePlaceHelpers.Length; i++)
                {
                    lightProbesVolumePlaceHelpers[i].ExePlace();
                }
            }
        }
    }


    [ExecuteInEditMode]
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(LightProbeGroup))]
    //光探头体积放置助手,用于辅助摆放长方体形状的光探头
    //长方体顶端有一个射线检测点，为正常工作，请保证该点不要在几何体内部
    public class LightProbesVolumePlaceHelper : MonoBehaviour
    {
        [Range(0.1f, 5)] public float horizontalSpacing = 2.0f; //横向间距
        [Range(0.1f, 5)] public float verticalSpacing = 2.0f; //纵向间距
        [Range(0.01f, 5)] public float offsetFromFloor = 0.5f;
        public Vector3 rayTestOriginOffset = Vector3.zero; //射线检测原点偏移量
        public bool drawPreview = true;
        [Range(0f, 2)] public float gizmoSizeMultiplier = 1;

        //component
        private BoxCollider boxCollider;
        private LightProbeGroup lightProbeGroup;

        //draw gizmo
        private Vector3 rayTestOrigin;
        private Vector3[] validPositions;
        private Vector3[] invalidPositions;

        //tmp data
        private Vector3 lastPosition;
        private Vector3 lastCenter;
        private Vector3 lastSize;


        private void MakeComponent()
        {
            if (boxCollider == null)
            {
                boxCollider = GetComponent<BoxCollider>();
                boxCollider.isTrigger = true;
                boxCollider.size = new Vector3(5, 2, 5);
            }

            if (lightProbeGroup == null)
            {
                lightProbeGroup = GetComponent<LightProbeGroup>();
                // lightProbeGroup.probePositions = new Vector3[] { };//清空所有lightprobe
            }
        }

        private void OnValidate()
        {
            GetPositions(ref validPositions, ref invalidPositions);
            //dont scale this obj
            transform.localScale = Vector3.one;
        }

        private void Update()
        {
            MakeComponent();

            //detect change
            if (lastPosition != transform.position ||
                lastCenter != boxCollider.center ||
                lastSize != boxCollider.size)
            {
                OnValidate();
                lastPosition = transform.position;
                lastCenter = boxCollider.center;
                lastSize = boxCollider.size;
            }
        }

        //执行放置
        public void ExePlace()
        {
            Vector3[] validVertPositions = validPositions;

            //将位置灌进去
            Vector3[] ProbePos = new Vector3[validVertPositions.Length];
            for (int i = 0; i < validVertPositions.Length; i++)
            {
                ProbePos[i] = gameObject.transform.InverseTransformPoint(validVertPositions[i]);
            }

            lightProbeGroup.probePositions = ProbePos;

            //Finish
            Debug.Log("Finished placing " + ProbePos.Length + " probes for " + gameObject.name);
        }

        //获取即将放置光探头的世界坐标位置
        private void GetPositions(ref Vector3[] validPositions, ref Vector3[] invalidPositions)
        {
            boxCollider = gameObject.GetComponent<BoxCollider>();
            //Make sure collider is a trigger
            boxCollider.isTrigger = true;

            //avoid division by 0
            horizontalSpacing = Mathf.Max(horizontalSpacing, 0.01f);
            verticalSpacing = Mathf.Max(verticalSpacing, 0.01f);

            //Calculate Start Points at the top of the collider
            //startPosition是在碰撞器顶部的一些点，从上面开始摆放
            Vector3[] startPositions =
                StartPoints(boxCollider.size, boxCollider.center, boxCollider.transform, horizontalSpacing);

            float minY = boxCollider.bounds.min.y;
            float maxY = boxCollider.bounds.max.y;

            float sizeY = boxCollider.size.y;
            int ycount = Mathf.FloorToInt((sizeY - offsetFromFloor) / verticalSpacing) + 1;

            List<Vector3> VertPositions = new List<Vector3>();

            //层数
            for (int i = 0; i < ycount; i++)
            {
                foreach (Vector3 position in startPositions)
                {
                    Vector3 pos = position + Vector3.up * (verticalSpacing * i) - Vector3.up * sizeY +
                                  Vector3.up * offsetFromFloor;
                    VertPositions.Add(pos);
                }
            }

            List<Vector3> validVertPositions = new List<Vector3>();
            List<Vector3> invalidVertPositions = new List<Vector3>();

            //Inside Geometry test : take an arbitrary position in space and trace from that position to the probe position and back from the probe position to the arbitrary position. If the number of hits is different for both raycasts the probe is considered to be inside an object.
            //When using Draw Debug the arbitrary position is the Green cross in the air.
            //内部几何测试：在空间中取任意位置并从该位置跟踪到探针位置，然后从探针位置返回到任意位置。如果两个射线投射的命中数不同，则认为探测器位于对象内部。
            //使用Draw Debug时，任意位置是空中的绿色十字。
            int j = 0;
            //这就是随意位置，从这个点做来回射线检测来判断候选点是否在几何体内部。该点位置默认处于碰撞器顶端，也可以添加一个偏移量。
            //碰撞器的中心偏移 + 碰撞器的高度/2 + 偏移量
            Vector3 worldVec = transform.TransformVector(boxCollider.center +
                                                         new Vector3(0, boxCollider.size.y / 2f, 0) +
                                                         rayTestOriginOffset);
            rayTestOrigin = gameObject.transform.position + worldVec;
            foreach (Vector3 positionCandidate in VertPositions)
            {
                //positionCandidate 候选位置
                EditorUtility.DisplayProgressBar("Checking probes inside geometry",
                    j.ToString() + "/" + VertPositions.Count, (float)j / (float)VertPositions.Count);

                //正反射线
                Ray forwardRay = new Ray(rayTestOrigin, Vector3.Normalize(positionCandidate - rayTestOrigin));
                Ray backwardRay = new Ray(positionCandidate, Vector3.Normalize(rayTestOrigin - positionCandidate));
                RaycastHit[] hitsForward;
                RaycastHit[] hitsBackward;
                hitsForward = Physics.RaycastAll(forwardRay, Vector3.Distance(positionCandidate, rayTestOrigin), -1,
                    QueryTriggerInteraction.Ignore);
                hitsBackward = Physics.RaycastAll(backwardRay, Vector3.Distance(positionCandidate, rayTestOrigin), -1,
                    QueryTriggerInteraction.Ignore);
                if (hitsForward.Length == hitsBackward.Length)
                    validVertPositions.Add(positionCandidate);
                else
                {
                    invalidVertPositions.Add(positionCandidate);
                }

                j++;
            }

            EditorUtility.ClearProgressBar();

            validPositions = validVertPositions.ToArray();
            invalidPositions = invalidVertPositions.ToArray();
        }


        Vector3[] StartPoints(Vector3 size, Vector3 offset, Transform transform, float horizontalSpacing)
        {
            // Calculate count and start offset
            int xCount = Mathf.FloorToInt(size.x / horizontalSpacing) + 1;
            int zCount = Mathf.FloorToInt(size.z / horizontalSpacing) + 1;
            // Debug.Log(xCount + " "+ zCount);
            float startxoffset = (size.x - (xCount - 1) * horizontalSpacing) / 2;
            float startzoffset = (size.z - (zCount - 1) * horizontalSpacing) / 2;
            // Debug.Log(startxoffset + " "+ startzoffset);

            //if lightprobe count fits exactly in bounds, I know the probes at the maximum bounds will be rejected, so add offset
            // 如果 lightprobe 计数完全符合界限，我知道最大界限的探针将被拒绝，所以添加偏移量
            // if (startxoffset == 0)
            //     startxoffset = horizontalSpacing / 2;
            // if (startzoffset == 0)
            //     startzoffset = horizontalSpacing / 2;

            Vector3[] vertPositions = new Vector3[xCount * zCount];

            int vertexnumber = 0;

            for (int i = 0; i < xCount; i++)
            {
                for (int j = 0; j < zCount; j++)
                {
                    Vector3 position = new Vector3
                    {
                        y = size.y / 2,
                        x = startxoffset + (i * horizontalSpacing) - (size.x / 2),
                        z = startzoffset + (j * horizontalSpacing) - (size.z / 2)
                    };

                    vertPositions[vertexnumber] = transform.TransformPoint(position + offset);

                    vertexnumber++;
                }
            }

            return vertPositions;
        }

        private void OnDrawGizmos()
        {
            if (!drawPreview) return;

            float r = 0.2f * gizmoSizeMultiplier;

            Color oldColor = Gizmos.color;
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(rayTestOrigin, r);

            //画出遮挡的射线
            Gizmos.color = Color.red;
            for (int i = 0; i < invalidPositions.Length; i++)
            {
                Gizmos.DrawLine(rayTestOrigin, invalidPositions[i]);
            }

            //绘制即将放置的预览位置  （有效点）
            Gizmos.color = Color.green;
            for (int i = 0; i < validPositions.Length; i++)
            {
                Gizmos.DrawSphere(validPositions[i], r);
            }

            //画无效点
            Gizmos.color = Color.red;
            for (int i = 0; i < invalidPositions.Length; i++)
            {
                Gizmos.DrawSphere(invalidPositions[i], r);
            }

            //画已经放置的点
            Gizmos.color = Color.yellow;
            for (int i = 0; i < lightProbeGroup.probePositions.Length; i++)
            {
                Vector3 worldPos = lightProbeGroup.probePositions[i];
                worldPos = transform.TransformPoint(worldPos);
                Gizmos.DrawSphere(worldPos, r);
            }

            Gizmos.color = oldColor;
        }

    }
}