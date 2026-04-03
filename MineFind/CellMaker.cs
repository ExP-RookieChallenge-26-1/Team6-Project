using UnityEngine;

/// <summary>
/// 맵 데이터를 기반으로 실제 셀/가림막 오브젝트를 생성하는 클래스.
/// 
/// 역할:
/// 1. MapMakor에게 맵 생성 요청
/// 2. Cell 프리팹 생성
/// 3. Cover 프리팹 생성
/// 4. 생성한 객체를 배열에 저장
/// 5. InGameLogic 초기화
/// 
/// 즉, "화면에 보이는 오브젝트 생성 및 관리"를 담당한다.
/// </summary>
public class CellMaker : MonoBehaviour
{
    /// <summary>
    /// 맵 데이터 생성 클래스
    /// </summary>
    public MapMaker mapMaker;

    /// <summary>
    /// 인게임 로직 클래스
    /// </summary>
    public InGameLogic inGameLogic;

    [SerializeField] private Vector2Int mapSize = new Vector2Int(10, 10);
    [SerializeField] private int mineCount = 10;

    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private Transform cellParent;

    [SerializeField] private GameObject coverPrefab;
    [SerializeField] private Transform coverParent;

    /// <summary>
    /// 현재 맵 크기를 외부에서 읽을 수 있도록 제공
    /// </summary>
    public Vector2Int MapSize => mapSize;

    /// <summary>
    /// 생성된 셀 오브젝트들을 저장하는 배열
    /// </summary>
    public Cell[,] Cells;

    /// <summary>
    /// 생성된 가림막 오브젝트들을 저장하는 배열
    /// 
    /// Covers[x, y]가 null이면
    /// 해당 칸은 이미 열린 상태라고 볼 수 있다.
    /// </summary>
    public Cover[,] Covers;

    /// <summary>
    /// 시작 시 맵 생성 및 셀/가림막 생성
    /// </summary>
    private void GameStart()
    {
         mapMaker = GetComponent<MapMaker>();
        // 먼저 맵 데이터를 생성한다.
        mapMaker.MakeMap(mapSize, mineCount);

        // 셀과 가림막을 저장할 2차원 배열 생성
        Cells = new Cell[mapSize.x, mapSize.y];
        Covers = new Cover[mapSize.x, mapSize.y];

        // 맵 전체를 순회하며 셀과 가림막 생성
        for (int x = 0; x < mapSize.x; x++)
        {
            for (int y = 0; y < mapSize.y; y++)
            {
                // 맵을 화면 중앙 기준으로 배치하기 위한 좌표 계산
                // 0,0부터 시작하는 맵 좌표를
                // 실제 월드 좌표로 옮기는 과정이다.
                Vector3 pos = new Vector3(x - mapSize.x / 2 + 0.5f, y - mapSize.y / 2 + 0.5f, 0);

                // 셀 생성
                GameObject cellObj = Instantiate(cellPrefab, pos, Quaternion.identity, cellParent);
                Cell cell = cellObj.GetComponent<Cell>();
                Cells[x, y] = cell;

                // 셀에 표시할 텍스트 설정
                // 지뢰면 "M"
                // 일반 칸이면 주변 지뢰 수를 표시
                if (mapMaker.Map[x, y] == Type.Mine)
                {
                    cell.SetText("M");
                }
                else
                {
                    int aroundMineCount = GetAroundMineCount(x, y);

                    if (aroundMineCount == 0)
                        cell.SetText("");
                    else
                        cell.SetText(aroundMineCount.ToString());
                }

                // 가림막 생성
                GameObject coverObj = Instantiate(coverPrefab, pos, Quaternion.identity, coverParent);

                Cover cover = coverObj.GetComponent<Cover>();
                cover.Init(x, y, this);
                Covers[x, y] = cover;
            }
        }

        // 모든 생성이 끝난 후 게임 로직에 자기 자신을 넘긴다.
        inGameLogic.Init(this);
    }

    /// <summary>
    /// 특정 칸 주변 8방향에 지뢰가 몇 개 있는지 계산하는 함수.
    /// </summary>
    /// <param name="centerX">중심 x 좌표</param>
    /// <param name="centerY">중심 y 좌표</param>
    /// <returns>주변 지뢰 개수</returns>
    public int GetAroundMineCount(int centerX, int centerY)
    {
        int count = 0;

        // 중심 칸 주변 3x3 범위를 탐색
        for (int x = centerX - 1; x <= centerX + 1; x++)
        {
            for (int y = centerY - 1; y <= centerY + 1; y++)
            {
                // 자기 자신은 제외
                if (x == centerX && y == centerY)
                    continue;

                // 범위 밖 좌표는 제외
                if (x < 0 || y < 0 || x >= mapSize.x || y >= mapSize.y)
                    continue;

                // 지뢰라면 카운트 증가
                if (mapMaker.Map[x, y] == Type.Mine)
                    count++;
            }
        }

        return count;
    }

    /// <summary>
    /// 특정 좌표를 열어달라는 요청을 받으면
    /// 실제 처리는 InGameLogic에게 넘긴다.
    /// </summary>
    /// <param name="x">열 x 좌표</param>
    /// <param name="y">열 y 좌표</param>
    /// <param name="coverObject">클릭된 가림막 오브젝트</param>
    public void OpenCell(int x, int y, GameObject coverObject)
    {
        inGameLogic.OpenCell(x, y);
    }

    /// <summary>
    /// 특정 좌표의 가림막을 제거하는 함수.
    /// 
    /// 가림막이 사라지면 아래에 있던 실제 셀 내용이 보이게 된다.
    /// </summary>
    /// <param name="x">좌표 x</param>
    /// <param name="y">좌표 y</param>
    public void RemoveCover(int x, int y)
    {
        // 이미 제거된 경우 아무것도 하지 않음
        if (Covers[x, y] == null)
            return;

        Destroy(Covers[x, y].gameObject);
        Covers[x, y] = null;
    }

    /// <summary>
    /// 해당 좌표가 이미 열렸는지 확인하는 함수.
    /// 
    /// 현재 구조에서는 Covers[x, y] == null 이면
    /// 열린 상태로 판단한다.
    /// </summary>
    public bool IsOpened(int x, int y)
    {
        return Covers[x, y] == null;
    }

    /// <summary>
    /// 해당 좌표가 지뢰인지 확인
    /// </summary>
    public bool IsMine(int x, int y)
    {
        return mapMaker.Map[x, y] == Type.Mine;
    }

    /// <summary>
    /// 좌표가 맵 범위 안에 들어오는지 확인
    /// </summary>
    public bool IsInRange(int x, int y)
    {
        return x >= 0 && y >= 0 && x < mapSize.x && y < mapSize.y;
    }


}