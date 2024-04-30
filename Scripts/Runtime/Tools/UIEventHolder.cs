using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PluginHub.Runtime
{

    /// <summary>
    /// 这个类是一种ui按钮事件添加方法，强调用事件类来挂按钮，unity推出的方法是每个按钮来挂事件，其实都要挂，麻烦程度差不多
    /// </summary>
//使用范例
//public class UIMain : MonoBehaviour
//{
//    Button button;
//    void Start()
//    {
//        button = transform.Find("Button").GetComponent<Button>();
//        UIEventHolder.Get(button.gameObject).onClick = OnButtonClick;//添加事件
//    }

//    private void OnButtonClick(GameObject go)
//    {
//        //在这里监听按钮的点击事件
//        if (go == button.gameObject)
//        {
//            Debug.Log("DoSomeThings");
//        }
//    }
//}
//雨松MoMo的方法，也类似余佳以前的方法
//让UGUI事件集中到一起，不再手动在检视面板添加事件了，统一在代码中添加
    public class UIEventHolder : EventTrigger
    {
        public delegate void VoidDelegate(GameObject go);

        public VoidDelegate onDown;
        public VoidDelegate onUp;
        public VoidDelegate onClick;
        public VoidDelegate onEnter;
        public VoidDelegate onExit;
        public VoidDelegate onSelect;
        public VoidDelegate onUpdateSelect;
        public VoidDelegate onBeginDragEvent;
        public VoidDelegate onDragEvent;

        public VoidDelegate onEndDragEvent;

        //这个要选择性的使用，这个让ScrollRect上的按钮可以用UIEventHolder类添加事件，而不影响点击按钮时候的ScrollRect滑动。但是对于Slider来说，就会和ScrollRect同时滑动，这是不对的。
        private GameObject scrollrectGO;

        void Start()
        {
            ScrollRect sr = transform.GetComponentInParent<ScrollRect>();
            if (sr) scrollrectGO = sr.gameObject;
        }

        public static UIEventHolder Get(GameObject go)
        {
            UIEventHolder listener = go.GetComponent<UIEventHolder>();
            if (listener == null) listener = go.AddComponent<UIEventHolder>();
            return listener;
        }

        public override void OnPointerClick(PointerEventData eventData)
        {
            if (onClick != null) onClick(gameObject);
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            if (onDown != null) onDown(gameObject);
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            if (onEnter != null) onEnter(gameObject);
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            if (onExit != null) onExit(gameObject);
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            if (onUp != null) onUp(gameObject);
        }

        public override void OnSelect(BaseEventData eventData)
        {
            if (onSelect != null) onSelect(gameObject);
        }

        public override void OnUpdateSelected(BaseEventData eventData)
        {
            if (onUpdateSelect != null) onUpdateSelect(gameObject);
        }

        public override void OnBeginDrag(PointerEventData eventData)
        {
            if (onBeginDragEvent != null) onBeginDragEvent(gameObject);
            if (scrollrectGO) ExecuteEvents.Execute(scrollrectGO, eventData, ExecuteEvents.beginDragHandler);
        }

        public override void OnDrag(PointerEventData eventData)
        {
            if (onDragEvent != null) onDragEvent(gameObject);
            if (scrollrectGO) ExecuteEvents.Execute(scrollrectGO, eventData, ExecuteEvents.dragHandler);
        }

        public override void OnEndDrag(PointerEventData eventData)
        {
            if (onEndDragEvent != null) onEndDragEvent(gameObject);
            if (scrollrectGO) ExecuteEvents.Execute(scrollrectGO, eventData, ExecuteEvents.endDragHandler);
        }


    }
}