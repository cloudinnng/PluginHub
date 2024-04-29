﻿using UnityEngine;

//持久化对象，场景中只同时存在一个该对象,若有第二个会自动删除
//经过测试，第二次重新加载包含该对象的场景时，该对象能够正确删除。其子对象上的脚本不会执行Start函数
//该对象不会随着场景的切换而删除
//该组件位于执行顺序第一位
[DefaultExecutionOrder(int.MinValue)]
public class PersistentObject : MonoBehaviour
{
    private static bool _hasInstance = false;
    public static PersistentObject Instance { get; private set; }
    
    
    private void Awake()
    {
        if (_hasInstance)
        {
            //多单例情况下，删除多余的单例
            Debug.LogWarning("多个PersistentObject实例，删除多余的实例");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _hasInstance = true;
    }
}
