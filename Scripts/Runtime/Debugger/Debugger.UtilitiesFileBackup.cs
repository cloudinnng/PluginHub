using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace PluginHub.Runtime
{
    // 使用飞书的免费空间上传和下载文件的相关方法
    public partial class Debugger
    {
        #region JsonModel

        private class ListFilesJsonModel
        {
            public int code;
            public Data data;
            public string msg;

            [Serializable]
            public class Data
            {
                public List<File> files;
                public bool has_more;

                [Serializable]
                public class File
                {
                    public string name;
                    public string parent_token;
                    public string token;
                    public string type;
                    public string created_time;
                    public string modified_time;
                    public string owner_id;
                    public string url;
                }
            }
        }


        // {"code":0 ...}
        private class CodeOnlyJsonModel
        {
            public int code;
        }

        // 获取 TenantAccessToken 时的返回值
        // {"code":0,"expire":7200,"msg":"ok","tenant_access_token":"t-g10471akNZX2TJMBPOIO7YBQFHVVSWG2HX5YE33D"}
        private class TenantAccessTokenJsonModel
        {
            public int code;
            public int expire;
            public string msg;
            public string tenant_access_token;
        }
        #endregion

        // 需要在 Debugger.ini 中配置以下信息
        // [AppInfo]
        // appid =
        // appSecret =
        // rootFolderToken =
        private static class FeiShuFileBackup
        {
            // ini file config
            private static string appid;
            private static string appSecret;
            public static string rootFolderToken{ get; private set; }

            // token 缓存
            private static string tenantAccessToken;
            private static float tenantAccessTokenLastGetTime;
            private static int tenantAccessTokenExpire;

            static FeiShuFileBackup()
            {
                INIParser ini = new INIParser();
                // 读取配置文件
                ini.Open(Application.streamingAssetsPath + "/Debugger.ini");
                appid = ini.ReadValue(SectionName: "AppInfo", Key: "appid", DefaultValue: "");
                appSecret = ini.ReadValue(SectionName: "AppInfo", Key: "appSecret", DefaultValue: "");
                rootFolderToken = ini.ReadValue(SectionName: "AppInfo", Key: "rootFolderToken", DefaultValue: "");
                ini.Close();

                // 有值才初始化成功
                if (IsInitSuccess)
                    Debug.Log($"FeiShu appid: {appid}, appSecret: {appSecret}, rootFolderToken: {rootFolderToken}");
                else
                    Debug.LogError("FeiShu appid, appSecret, rootFolderToken is empty");
            }

            public static bool IsInitSuccess =>!string.IsNullOrEmpty(appid) && !string.IsNullOrEmpty(appSecret) && !string.IsNullOrEmpty(rootFolderToken);

            public static bool NeedRefreshToken()
            {
                return string.IsNullOrEmpty(tenantAccessToken) ||
                       Time.realtimeSinceStartup - tenantAccessTokenLastGetTime > tenantAccessTokenExpire;
            }
            public static IEnumerator GetTenantAccessToken()
            {
                if (!NeedRefreshToken())
                    yield break;

                string url = "https://open.feishu.cn/open-apis/auth/v3/tenant_access_token/internal";
                //准备请求体
                string jsonStr = "{\"app_id\":\"" + appid + "\",\"app_secret\":\"" + appSecret + "\"}";

                using (UnityWebRequest webRequest = new UnityWebRequest(url,"POST"))
                {
                    //设置请求头
                    webRequest.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
                    webRequest.downloadHandler = (DownloadHandler) new DownloadHandlerBuffer();
                    webRequest.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonStr));

                    //timeout
                    webRequest.timeout = 10;
                    yield return webRequest.SendWebRequest();//发送请求

                    if (webRequest.result == UnityWebRequest.Result.Success)
                    {
                        Debug.Log(webRequest.downloadHandler.text);
                        TenantAccessTokenJsonModel model = JsonUtility.FromJson<TenantAccessTokenJsonModel>(webRequest.downloadHandler.text);
                        if (model.code == 0)
                        {
                            tenantAccessToken = model.tenant_access_token;
                            tenantAccessTokenExpire = model.expire;
                            tenantAccessTokenLastGetTime = Time.realtimeSinceStartup;
                        }
                    }
                    else//发生错误
                    {
                        Debug.Log(webRequest.downloadHandler.text);
                        Debug.LogError(webRequest.error);
                    }
                    webRequest.Dispose();
                }
            }

            // 文档：https://open.feishu.cn/document/server-docs/docs/drive-v1/upload/upload_all?appId=cli_a24263f3b2f8d00d
            public static IEnumerator UploadFile(string absFilePath, string folderToken)
            {
                Debug.Log($"UploadFile:{absFilePath}");

                yield return GetTenantAccessToken();

                string url = "https://open.feishu.cn/open-apis/drive/v1/files/upload_all";

                using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
                {
                    // 设置请求头
                    request.SetRequestHeader("Authorization", "Bearer " + tenantAccessToken);
                    byte[] boundary = UnityWebRequest.GenerateBoundary();
                    string contentTypeString = "multipart/form-data; boundary=" + System.Text.Encoding.UTF8.GetString(boundary);
                    request.SetRequestHeader("Content-Type", contentTypeString);

                    // 设置请求体
                    List<IMultipartFormSection> form = new List<IMultipartFormSection>();
                    form.Add(new MultipartFormDataSection("file_name", Path.GetFileName(absFilePath)));
                    form.Add(new MultipartFormDataSection("parent_type", "explorer"));
                    form.Add(new MultipartFormDataSection("parent_node", folderToken));
                    form.Add(new MultipartFormDataSection("size", new FileInfo(absFilePath).Length.ToString().Trim()));
                    form.Add(new MultipartFormFileSection("file", File.ReadAllBytes(absFilePath), Path.GetFileName(absFilePath), "application/octet-stream"));

                    request.uploadHandler = new UploadHandlerRaw(UnityWebRequest.SerializeFormSections(form, boundary));
                    request.downloadHandler = new DownloadHandlerBuffer();

                    yield return request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        CodeOnlyJsonModel model = JsonUtility.FromJson<CodeOnlyJsonModel>(request.downloadHandler.text);
                        if (model.code == 0)
                            Debug.Log("UploadFile Success");
                        else
                        {
                            Debug.LogError("UploadFile Fail");
                            Debug.Log(request.downloadHandler.text);
                        }
                    }
                    else
                    {
                        Debug.LogError(request.error);
                        Debug.Log(request.downloadHandler.text);
                    }
                }
            }

            // https://open.feishu.cn/document/server-docs/docs/drive-v1/download/download
            // FeiShuFileBakcup.DownloadFile("SS4zbPEvvodjDWx7bKEck1r3nYf", "C:/Users/xxx/Desktop/xxx.jpg");
            public static IEnumerator DownloadFile(string fileToken, string savePath)
            {
                yield return GetTenantAccessToken();

                string url = $"https://open.feishu.cn/open-apis/drive/v1/files/{fileToken}/download";

                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    // 设置请求头
                    request.SetRequestHeader("Authorization", "Bearer " + tenantAccessToken);

                    yield return request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        //save to persistentDataPath
                        Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                        File.WriteAllBytes(savePath, request.downloadHandler.data);
                        Debug.Log($"File Downloaded: {savePath}");
                    }
                    else
                    {
                        Debug.LogError(request.error);
                    }
                }
            }

            // 列出有哪些文件
            // 文档：https://open.feishu.cn/document/server-docs/docs/drive-v1/folder/list
            public static IEnumerator ListFiles(string folderToken, Dictionary<string, string> result)
            {
                result.Clear();
                yield return GetTenantAccessToken();

                string url =
                    $"https://open.feishu.cn/open-apis/drive/v1/files?page_size=200&folder_token={folderToken}";

                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    // 设置请求头
                    request.SetRequestHeader("Authorization", "Bearer " + tenantAccessToken);


                    yield return request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        Debug.Log(request.downloadHandler.text);

                        ListFilesJsonModel model =
                            JsonUtility.FromJson<ListFilesJsonModel>(request.downloadHandler.text);
                        if (model.code == 0)
                        {
                            foreach (var file in model.data.files)
                            {
                                // Debug.Log($"name:{file.name}, type:{file.type}, url:{file.url}");
                                result.Add(file.token, file.name);
                            }
                        }
                        else
                        {
                            Debug.LogError("ListFiles Fail");
                        }
                    }
                    else
                    {
                        Debug.LogError(request.error);
                    }
                }
            }

            // 删除文件
            // 文档：https://open.feishu.cn/document/server-docs/docs/drive-v1/file/delete
            public static IEnumerator DeleteFile(string fileToken)
            {
                yield return GetTenantAccessToken();

                string url = $"https://open.feishu.cn/open-apis/drive/v1/files/{fileToken}?type=file";

                using (UnityWebRequest request = new UnityWebRequest(url, "DELETE"))
                {
                    // 设置请求头
                    request.SetRequestHeader("Authorization", "Bearer " + tenantAccessToken);

                    //
                    request.downloadHandler = (DownloadHandler) new DownloadHandlerBuffer();

                    yield return request.SendWebRequest();
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        CodeOnlyJsonModel model = JsonUtility.FromJson<CodeOnlyJsonModel>(request.downloadHandler.text);
                        if (model.code == 0)
                            Debug.Log("DeleteFile Success");
                        else
                        {
                            Debug.LogError("DeleteFile Fail");
                            Debug.Log(request.downloadHandler.text);
                        }
                    }
                    else
                    {
                        Debug.LogError(request.error);
                        Debug.Log(request.downloadHandler.text);
                    }
                }
            }
        }
    }
}