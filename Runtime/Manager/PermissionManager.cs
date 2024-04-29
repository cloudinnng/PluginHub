using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Serialization;

//定义App需要的权限
//对于广告应用，尽量做到如果用户不给网络权限，就不让游玩
public class PermissionManager : SceneSingleton<PermissionManager>
{
    [Flags]
    public enum PermissionType
    {
        None = 0,
        Internet = 1 << 0,
        Camera = 1 << 1,
        Microphone = 1 << 2,
        Location = 1 << 3,
    }

    public PermissionType RequestPermission = PermissionType.Internet;


    public static Action<PermissionType, bool> OnPermission;

    IEnumerator Start()
    {
        //请求权限
        if (RequestPermission.HasFlag(PermissionType.Internet))
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                ToastManager.Instance.Show("无网络权限", 3, true);
                ToastManager.Instance.Show("部分功能可能无法使用", 3, true);
                OnPermission?.Invoke(PermissionType.Internet, false);
            }
            else
            {
                OnPermission?.Invoke(PermissionType.Internet, true);
            }
        }

        if (RequestPermission.HasFlag(PermissionType.Camera))
        {
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Permission.RequestUserPermission(Permission.Camera);
                while (!Permission.HasUserAuthorizedPermission(Permission.Camera))
                {
                    yield return null;
                }

                OnPermission?.Invoke(PermissionType.Camera, true);
            }
            else
            {
                OnPermission?.Invoke(PermissionType.Camera, true);
            }
        }

        if (RequestPermission.HasFlag(PermissionType.Microphone))
        {
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Permission.RequestUserPermission(Permission.Microphone);
                while (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
                {
                    yield return null;
                }

                OnPermission?.Invoke(PermissionType.Microphone, true);
            }
            else
            {
                OnPermission?.Invoke(PermissionType.Microphone, true);
            }
        }
        if (RequestPermission.HasFlag(PermissionType.Location))
        {
            if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            {
                Permission.RequestUserPermission(Permission.FineLocation);
                while (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
                {
                    yield return null;
                }

                OnPermission?.Invoke(PermissionType.Location, true);
            }
            else
            {
                OnPermission?.Invoke(PermissionType.Location, true);
            }
        }

        yield break;
    }
}