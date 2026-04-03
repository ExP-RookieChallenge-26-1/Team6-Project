using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 실제 인게임 로직을 담당하는 클래스.
/// 
/// 역할:
/// 1. 셀 열기
/// 2. 지뢰를 눌렀는지 판정
/// 3. 빈칸(주변 지뢰 수가 0인 칸) 연쇄 오픈
/// 
/// 즉, 게임 규칙을 처리하는 부분이다.
/// </summary>
public class InGameLogic : MonoBehaviour
{
    /// <summary>
    /// 맵/셀/가림막 데이터에 접근하기 위한 참조.
    /// CellMaker가 생성 완료 후 Init으로 넘겨준다.
    /// </summary>
    private CellMaker cellMaker;

    /// <summary>
    /// CellMaker 참조를 저장하는 초기화 함수.
    /// </summary>
    /// <param name="cellMaker">셀과 가림막을 관리하는 클래스</param>
    public void Init(CellMaker cellMaker)
    {
        this.cellMaker = cellMaker;
    }

    /// <summary>
    /// 특정 좌표의 셀을 여는 함수.
    /// 
    /// 처리 순서:
    /// 1. 예외 체크
    /// 2. 이미 열린 칸인지 확인
    /// 3. 지뢰인지 확인
    /// 4. 일반 칸이면 가림막 제거
    /// 5. 주변 지뢰 수가 0이면 빈칸 연쇄 오픈
    /// </summary>
    /// <param name="x">열 x 좌표</param>
    /// <param name="y">열 y 좌표</param>
    public void OpenCell(int x, int y)
    {
        // 맵 범위 밖 좌표면 무시
        if (!cellMaker.IsInRange(x, y))
            return;

        // 이미 열린 칸이면 다시 열 필요 없음
        if (cellMaker.IsOpened(x, y))
            return;

        // 지뢰면 가림막을 제거하고 게임 오버 처리
        if (cellMaker.IsMine(x, y))
        {
            cellMaker.RemoveCover(x, y);
            Debug.Log("게임 오버");
            return;
        }

        // 일반 칸이면 현재 칸의 가림막 제거
        cellMaker.RemoveCover(x, y);

        // 주변 지뢰 개수가 0이면
        // 주변 빈 영역도 자동으로 열어준다.
        if (cellMaker.GetAroundMineCount(x, y) == 0)
        {
            OpenEmptyArea(x, y);
        }
    }

    /// <summary>
    /// 빈칸 영역을 연쇄적으로 여는 함수.
    /// 
    /// Queue를 사용한 BFS(너비 우선 탐색) 방식이다.
    /// 시작 칸이 0이면 주변 칸을 열고,
    /// 주변 칸도 0이면 다시 그 주변을 계속 여는 구조다.
    /// </summary>
    /// <param name="startX">시작 x 좌표</param>
    /// <param name="startY">시작 y 좌표</param>
    private void OpenEmptyArea(int startX, int startY)
    {
        // 앞으로 검사할 좌표들을 저장하는 큐
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        // 시작 좌표를 큐에 넣고 탐색 시작
        queue.Enqueue(new Vector2Int(startX, startY));

        // 큐가 빌 때까지 반복
        while (queue.Count > 0)
        {
            // 현재 확장 기준이 되는 좌표 꺼내기
            Vector2Int current = queue.Dequeue();

            // 현재 칸 주변 8칸 검사
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                for (int offsetY = -1; offsetY <= 1; offsetY++)
                {
                    int nextX = current.x + offsetX;
                    int nextY = current.y + offsetY;

                    // 맵 범위를 벗어나면 무시
                    if (!cellMaker.IsInRange(nextX, nextY))
                        continue;

                    // 이미 열린 칸이면 무시
                    if (cellMaker.IsOpened(nextX, nextY))
                        continue;

                    // 지뢰는 자동 오픈 대상이 아니므로 무시
                    if (cellMaker.IsMine(nextX, nextY))
                        continue;

                    // 일반 칸이면 가림막 제거
                    cellMaker.RemoveCover(nextX, nextY);

                    // 주변 지뢰 수가 0이면
                    // 이 칸을 기준으로 다시 주변을 확장해야 하므로 큐에 추가
                    if (cellMaker.GetAroundMineCount(nextX, nextY) == 0)
                    {
                        queue.Enqueue(new Vector2Int(nextX, nextY));
                    }
                }
            }
        }
    }
}