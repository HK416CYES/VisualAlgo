using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VisualAlgo.Managers.UI
{
    public class LinkHandler : MonoBehaviour, IPointerClickHandler
    {
        private TextMeshProUGUI textMeshProUGUI;

        private void Start()
        {
            textMeshProUGUI = GetComponent<TextMeshProUGUI>();

            // 设置文本内容
            //string fullText = "为了更好地保障您的个人权益，在使用前，请您务必审慎的阅读和理解我们的<color=blue><link=https://www.longtugame.com/t2/261/6320.html>《用户协议》</link></color>、<color=blue><link=https://www.longtugame.com/t2/261/6319.html>《隐私协议》</link></color>和<color=blue><link=https://www.longtugame.com/t2/261/6321.html>《儿童隐私保护指引》</link></color>。如您已详细阅读并同意此协议，请点击“同意”开始游戏。如您拒绝，将无法进入游戏。";
            //textMeshProUGUI.text = fullText;
            //textMeshProUGUI.ForceMeshUpdate(); // 确保文本更新

            // 注册链接点击事件的回调函数
            textMeshProUGUI.richText = true;
            textMeshProUGUI.raycastTarget = true;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // 检查点击的区域是否是文本链接
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(textMeshProUGUI, Input.mousePosition, null);
            if (linkIndex != -1)
            {
                TMP_LinkInfo linkInfo = textMeshProUGUI.textInfo.linkInfo[linkIndex];
                string url = linkInfo.GetLinkID();
                Application.OpenURL(url);
            }
        }
    }
}

