
using TMPro;
using UnityEngine;

/// <summary>
/// 실제 바닥 셀 오브젝트.
/// 
/// 현재 역할은 단순하다.
/// - TMP_Text에 문자열을 표시하는 역할
/// 
/// 지뢰면 "M",
/// 일반칸이면 주변 지뢰 개수를 표시한다.
/// </summary>
public class Cell : MonoBehaviour
{
    [SerializeField] private TMP_Text text;

    /// <summary>
    /// 셀에 표시할 텍스트를 설정하는 함수
    /// </summary>
    /// <param name="value">표시할 문자열</param>
    public void SetText(string value)
    {
        text.text = value;
    }
}
