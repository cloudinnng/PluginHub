using TMPro;
using UnityEngine.UI;
using UnityEngine;

namespace PluginHub.Runtime
{
    /// <summary>
    /// 该组件对UGUI文本组件进行优化，解决一些问题：
    /// </summary>
    public class UITextOptim : MonoBehaviour
    {
        [Tooltip("文本组件有时候打空格会自动进行换行，原因是空格分为两种空格，unity内部自动替换成换行空格。启用enableSpaceReplace选项以重新替换回来")]
        public bool enableSpaceReplace = true;

        [Tooltip("\n在文本组件中不会起到换行作用，unity会替换成\\n。启用enableNewlineReplace选项以重新替换回来。之后即可使用\n控制文本组件换行")]
        public bool enableNewlineReplace = true;

        private Text txt; //Text文本组件
        private TextMeshProUGUI txtPro; //TextMeshProUGUI文本组件

        private const string NonBreakingSpace = "\u00A0"; //不换行空格的Unicode编码

        private void Awake()
        {
            txt = GetComponent<Text>();
            txtPro = GetComponent<TextMeshProUGUI>();

            OnTextChange();

            if (txt != null)
                txt.RegisterDirtyLayoutCallback(OnTextChange);
            if (txtPro != null)
                txtPro.RegisterDirtyVerticesCallback(OnTextChange);
        }

        [ContextMenu("execute")]
        private void OnTextChange()
        {
            if (txt == null)
                txt = GetComponent<Text>();
            if (txtPro == null)
                txtPro = GetComponent<TextMeshProUGUI>();


            if (enableSpaceReplace && txt != null && txt.text.Contains(" "))
            {
                txt.text = txt.text.Replace(" ", NonBreakingSpace);
            }

            if (enableNewlineReplace && txt != null && txt.text.Contains("\\n"))
            {
                txt.text = txt.text.Replace("\\n", "\n");
            }

            if (enableSpaceReplace && txtPro != null && txtPro.text.Contains(" "))
            {
                txtPro.text = txtPro.text.Replace(" ", NonBreakingSpace);
            }

            if (enableNewlineReplace && txtPro != null && txtPro.text.Contains("\\n"))
            {
                txtPro.text = txtPro.text.Replace("\\n", "\n");
            }
        }
    }
}