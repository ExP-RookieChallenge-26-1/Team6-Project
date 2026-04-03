using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 가림막의 상태.
/// 
/// Normal : 일반 상태
/// Flag   : 깃발 표시 상태
/// Memo   : 메모(물음표) 상태
/// </summary>
public enum CoverState
{
    Normal,
    Flag,
    Memo
}

/// <summary>
/// 각 셀 위를 덮는 가림막 오브젝트.
/// 
/// 역할:
/// 1. 클릭 입력 받기
/// 2. 좌클릭 시 셀 열기
/// 3. 우클릭 시 상태 변경
/// 4. 상태에 따라 스프라이트 갱신
/// 
/// 실제 셀 오픈 규칙은 InGameLogic에서 처리하고,
/// 이 클래스는 "입력 처리 + 표시 변경" 쪽에 가깝다.
/// </summary>
public class Cover : MonoBehaviour, IPointerClickHandler
{
    /// <summary>
    /// 자신이 담당하는 셀의 x 좌표
    /// </summary>
    public int x;

    /// <summary>
    /// 자신이 담당하는 셀의 y 좌표
    /// </summary>
    public int y;

    /// <summary>
    /// 현재 가림막 상태
    /// </summary>
    public CoverState state = CoverState.Normal;

    [Header("Sprite")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite flagSprite;
    [SerializeField] private Sprite memoSprite;

    /// <summary>
    /// 상위 관리자(CellMaker) 참조
    /// </summary>
    private CellMaker cellMaker;

    /// <summary>
    /// 이 가림막이 직접 열렸는지 여부.
    /// 
    /// 현재 구조에선 실제 열린 상태 판정은
    /// CellMaker의 Covers[x, y] == null 쪽이 더 핵심이다.
    /// 그래도 현재 코드는 이 플래그도 함께 사용하고 있다.
    /// </summary>
    private bool isOpened = false;

    /// <summary>
    /// 가림막 초기화
    /// </summary>
    /// <param name="x">맵 x 좌표</param>
    /// <param name="y">맵 y 좌표</param>
    /// <param name="cellMaker">상위 관리자</param>
    public void Init(int x, int y, CellMaker cellMaker)
    {
        this.x = x;
        this.y = y;
        this.cellMaker = cellMaker;
        state = CoverState.Normal;
        Refresh();
    }

    /// <summary>
    /// EventSystem을 통해 클릭 이벤트를 받는 함수.
    /// 
    /// 좌클릭:
    /// - Normal 상태일 때만 셀 열기 가능
    /// 
    /// 우클릭:
    /// - Normal -> Flag -> Memo -> Normal 순으로 상태 변경
    /// </summary>
    /// <param name="eventData">클릭 정보</param>
    public void OnPointerClick(PointerEventData eventData)
    {
        // 이미 열렸다면 더 이상 처리하지 않음
        if (isOpened)
            return;

        // 좌클릭 처리
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            // Flag나 Memo 상태에서는 실수로 열리지 않도록 막음
            if (state != CoverState.Normal)
                return;

            isOpened = true;

            // 실제 오픈 처리는 CellMaker -> InGameLogic 쪽으로 넘긴다.
            cellMaker.OpenCell(x, y, gameObject);
        }

        // 우클릭 처리
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            // 상태를 순환시킨다.
            switch (state)
            {
                case CoverState.Normal:
                    state = CoverState.Flag;
                    break;

                case CoverState.Flag:
                    state = CoverState.Memo;
                    break;

                case CoverState.Memo:
                    state = CoverState.Normal;
                    break;
            }

            // 상태가 바뀌었으니 외형 갱신
            Refresh();
        }
    }

    /// <summary>
    /// 현재 상태(state)에 맞는 스프라이트를 적용하는 함수.
    /// </summary>
    private void Refresh()
    {
        switch (state)
        {
            case CoverState.Normal:
                spriteRenderer.sprite = normalSprite;
                break;

            case CoverState.Flag:
                spriteRenderer.sprite = flagSprite;
                break;

            case CoverState.Memo:
                spriteRenderer.sprite = memoSprite;
                break;
        }
    }
}