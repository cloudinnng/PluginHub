using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PluginHub.Editor
{
    public static class PHAssetContextMenu
    {
        #region Material
        [MenuItem("CONTEXT/Material/PH 使用 [Shader名称] 命名Material资产")]
        public static void RenameMatUseShaderName(MenuCommand command)
        {
            Material material = (Material)command.context;
            if (material != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(material);
                // Debug.Log(assetPath);
                string nameUsage = material.shader.name.Replace("/", "_");
                nameUsage = nameUsage.Replace(" ", "");
                // string nameUsage = Path.GetFileNameWithoutExtension(material.shader.name);
                string newName = $"M_{nameUsage}.mat";
                // Debug.Log(newName);
                string result = AssetDatabase.RenameAsset(assetPath, newName);
                if (!string.IsNullOrWhiteSpace(result))
                {
                    Debug.LogWarning(result);
                }
            }
        }

        [MenuItem("CONTEXT/Material/PH 使用 [MainTex名称] 命名Material资产")]
        public static void RenameMatUseMainTexName(MenuCommand command)
        {
            Material material = (Material)command.context;
            if (material != null)
            {
                if (material.mainTexture == null)
                {
                    Debug.LogWarning("mainTexture is null");
                    return;
                }


                string assetPath = AssetDatabase.GetAssetPath(material);
                // Debug.Log(assetPath);
                //string nameUsage = material.shader.name.Replace("/", "_");
                string nameUsage = Path.GetFileNameWithoutExtension(material.mainTexture.name);
                string newName = $"M_{nameUsage}.mat";
                // Debug.Log(newName);
                string result = AssetDatabase.RenameAsset(assetPath, newName);
                if (!string.IsNullOrWhiteSpace(result))
                {
                    Debug.LogWarning(result);
                }
            }
        }

        #region 智能纹理赋值
        //智能纹理赋值将材质文件同目录下的纹理文件赋值到材质属性上，无需手动拖拽
        //目前不一定所有管线支持，因为shader中的属性名不一样，遇到之后会继续完善
        //自定义Shader的支持，需要在shader中按以下规则命名属性
        //（_[纹理类型全程]Tex）,例如：_MainTex,_NormalTex,_MetallicTex,_EmissionTex,_HeightTex,

        //这里定义各种纹理的别名，用于匹配，优先级从高到低。
        //如果纹理文件名中包含这些别名，就会被赋值到对应的纹理属性上
        private static string[] albedoAlias = new string[] { "MainTex", "Albedo", "BaseColorMap", "BaseColor", "Color", "Diffuse", "col", "Diff" };
        private static string[] metallicAlias = new string[] { "Metalness", "MetallicMap", "Metallic", "Metal" };
        private static string[] emissionAlias = new string[] { "EmissionMap", "Emission", "emiss" };
        private static string[] normalAlias = new string[] { "NormalMap", "Normal", "BumpMap", "_n" };//有时BumpMap就是法线贴图
        private static string[] heightAlias = new string[] { "HeightMap", "Height", "high", "displacement", "_h", "disp" };
        private static string[] occlusionAlias = new string[] { "OcclusionMap", "Occlusion", "AO" };

        private static int NameContainAlias(string name, string[] aliasArray)
        {
            name = name.ToLower();
            for (int i = 0; i < aliasArray.Length; i++)
            {
                if (name.Contains(aliasArray[i].ToLower()))
                {
                    Debug.Log($"{name} 匹配到别名：{aliasArray[i]},优先级{i}");
                    return i;
                }
            }
            return -1;
        }

        [MenuItem("CONTEXT/Material/PH 智能纹理赋值")]
        public static void CM_MaterialSmartTexturesAssign(MenuCommand command)
        {
            Material material = (Material)command.context;
            if (material == null) return;
            //find textures
            Texture albedo = null, metallic = null, emission = null, normal = null, height = null, occlusion = null;
            int aliasID_albedo = 9999, aliasID_metallic = 9999, aliasID_emission = 9999, aliasID_normal = 9999,
                aliasID_height = 9999, aliasID_occlusion = 9999;

            //eg: Assets/03.Art/Textures/MetalSteelBrushed001/New Material.mat
            string matPath = AssetDatabase.GetAssetPath(material);
            //eg: Assets\03.Art\Textures\MetalSteelBrushed001
            matPath = Path.GetDirectoryName(matPath);
            // Debug.Log(matPath);
            string[] texGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { matPath });
            texGuids = texGuids.Reverse().ToArray(); //查询结果，当前文件夹的纹理会在最后面，所以这里进行反转。当前文件夹的纹理优先
            foreach (string texGuid in texGuids)
            {
                //eg: Assets/03.Art/Textures/MetalSteelBrushed001/MetalSteelBrushed001_Sphere.png
                string texPath = AssetDatabase.GUIDToAssetPath(texGuid);
                string fileName = Path.GetFileNameWithoutExtension(texPath).ToLower();

                int aliasIndex = -1;

                aliasIndex = NameContainAlias(fileName, albedoAlias);
                if (aliasIndex != -1 && aliasIndex < aliasID_albedo)
                {
                    aliasID_albedo = aliasIndex;
                    albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                }
                aliasIndex = NameContainAlias(fileName, metallicAlias);
                if (aliasIndex != -1 && aliasIndex < aliasID_metallic)
                {
                    aliasID_metallic = aliasIndex;
                    metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                }
                aliasIndex = NameContainAlias(fileName, emissionAlias);
                if (aliasIndex != -1 && aliasIndex < aliasID_emission)
                {
                    aliasID_emission = aliasIndex;
                    emission = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                }
                aliasIndex = NameContainAlias(fileName, normalAlias);
                if (aliasIndex != -1 && aliasIndex < aliasID_normal)
                {
                    aliasID_normal = aliasIndex;
                    normal = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                }
                aliasIndex = NameContainAlias(fileName, heightAlias);
                if (aliasIndex != -1 && aliasIndex < aliasID_height)
                {
                    aliasID_height = aliasIndex;
                    height = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                }
                aliasIndex = NameContainAlias(fileName, occlusionAlias);
                if (aliasIndex != -1 && aliasIndex < aliasID_occlusion)
                {
                    aliasID_occlusion = aliasIndex;
                    occlusion = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                }
            }

            //assign textures -------------------------

            //  set albedo map
            if (albedo != null)
            {
                bool albedoOk = false;
                if (material.HasProperty("_Color"))
                {
                    float alpha = material.color.a;
                    material.color = new Color(1, 1, 1, alpha);
                }
                //check if buildin r p and standard shader
                if (material.shader.name.Contains("Standard"))
                {
                    material.SetTexture("_MainTex", albedo);
                    albedoOk = true;
                }
                //compatible custom shader
                if (material.HasProperty("_albedo"))
                {
                    material.SetTexture("_albedo", albedo);
                    albedoOk = true;
                }
                if (material.HasProperty("_MainTex"))
                {
                    material.SetTexture("_MainTex", albedo);
                    albedoOk = true;
                }
                //URP里叫_BaseMap
                if (material.HasProperty("_BaseMap"))
                {
                    material.SetTexture("_BaseMap", albedo);
                    albedoOk = true;
                }
                if(!albedoOk)
                    Debug.LogError($"Albedo纹理找到，但是没有找到对应的属性，{material.name}");
            }

            //  set emission map
            if (emission != null)
            {
                bool emissionOk = false;

                //check if buildin r p and standard shader
                if (material.shader.name.Contains("Standard"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", Color.white);
                    material.SetTexture("_EmissionMap", emission);
                    material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.AnyEmissive;
                    emissionOk = true;
                }
                if(!emissionOk)
                    Debug.LogError($"Emission纹理找到，但是没有找到对应的属性，{material.name}");
            }

            //  set metallic map
            if (metallic != null)
            {
                bool metallicOk = false;
                //check if buildin rp and standard shader
                if (material.HasProperty("_MetallicGlossMap"))
                {
                    material.EnableKeyword("_METALLICGLOSSMAP");
                    material.SetTexture("_MetallicGlossMap", metallic);
                    metallicOk = true;
                }

                if (material.HasProperty("_MetallicTex"))
                {
                    material.SetTexture("_MetallicTex", metallic);
                    metallicOk = true;
                }

                if(!metallicOk)
                    Debug.LogError($"Metallic纹理找到，但是没有找到对应的属性，{material.name}");
            }

            if (normal != null)
            {
                bool normalOk = false;

                //  reimport normal texture as normal map
                string path = AssetDatabase.GetAssetPath(normal);
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
                if (importer.textureType != TextureImporterType.NormalMap)
                {
                    importer.textureType = TextureImporterType.NormalMap;
                    importer.SaveAndReimport();
                }

                //check if buildin rp and standard shader
                if (material.HasProperty("_BumpMap"))
                {
                    material.EnableKeyword("_NORMALMAP");
                    material.SetTexture("_BumpMap", normal);
                    normalOk = true;
                }
                //compatible custom shader
                if (material.HasProperty("_NormalTex"))
                {
                    material.SetTexture("_NormalTex", normal);
                    normalOk = true;
                }
                if (material.HasProperty("_NormalTex"))
                {
                    material.SetTexture("_NormalTex", normal);
                    normalOk = true;
                }

                if(!normalOk)
                    Debug.LogError($"Normal纹理找到，但是没有找到对应的属性，{material.name}");
            }

            //  set height map
            if (height != null)
            {
                bool heightOk = false;
                //check if buildin rp and standard shader
                if (material.HasProperty("_ParallaxMap"))
                {
                    material.EnableKeyword("_PARALLAXMAP");
                    material.SetTexture("_ParallaxMap", height);
                    heightOk = true;
                }
                if (material.HasProperty("_HeightMap"))
                {
                    material.SetTexture("_HeightMap", height);
                    heightOk = true;
                }
                if(!heightOk)
                    Debug.LogError($"Height纹理找到，但是没有找到对应的属性，{material.name}");
            }

            //  set occlusion map
            if (occlusion != null)
            {
                bool occlusionOk = false;
                //check if buildin rp and standard shader
                if (material.shader.name.Contains("Standard"))
                {
                    material.SetTexture("_OcclusionMap", occlusion);
                    occlusionOk = true;
                }
                if(!occlusionOk)
                    Debug.LogError($"Occlusion纹理找到，但是没有找到对应的属性，{material.name}");
            }
        }
        #endregion
        #endregion

        #region Shader
        [MenuItem("CONTEXT/Shader/PH 使用 [Shader名称] 命名shader文件")]
        public static void ShaderContextMenuRenameShaderFile(MenuCommand command)
        {
            Shader shader = (Shader)command.context;
            if (shader != null)
            {
                string assetPath = AssetDatabase.GetAssetPath(shader);
                //最后的名字
                string nameUsage = shader.name.Replace("/", "_");
                // string nameUsage = Path.GetFileNameWithoutExtension(shader.name);
                string newName = $"S_{nameUsage}.shader";
                string result = AssetDatabase.RenameAsset(assetPath, newName);
                if (!string.IsNullOrWhiteSpace(result))
                {
                    Debug.LogError(result);
                }
            }
        }
        #endregion


    }
}