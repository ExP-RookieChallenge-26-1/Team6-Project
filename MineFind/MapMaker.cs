using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 맵 한 칸의 타입을 나타내는 enum.
/// Mine이면 지뢰 칸, None이면 일반 빈 칸이다.
/// </summary>
public enum Type
{
    Mine,
    None,
}

/// <summary>
/// 실제 지뢰 맵 데이터를 생성하는 클래스.
/// 
/// 이 클래스의 역할:
/// 1. 2차원 배열(Map) 생성
/// 2. 모든 칸을 None으로 초기화
/// 3. 랜덤하게 Mine 배치
/// 
/// 즉, "게임 데이터 생성"을 담당한다.
/// 셀 오브젝트를 생성하거나 클릭 처리하는 역할은 하지 않는다.
/// </summary>
public class MapMaker : MonoBehaviour
{
    /// <summary>
    /// 실제 맵 데이터.
    /// [x, y] 형태로 접근한다.
    /// </summary>
    public Type[,] Map;

    /// <summary>
    /// 특정 타입(type)을 amount 개수만큼 랜덤 배치하는 함수.
    /// 현재는 Mine을 랜덤하게 심는 용도로 사용된다.
    /// </summary>
    /// <param name="type">배치할 타입</param>
    /// <param name="amount">배치할 개수</param>
    /// <param name="size">맵 크기</param>
    private void SetTypeRandom(Type type, int amount, Vector2Int size)
    {
        // 현재 비어 있는(None) 칸들의 좌표를 모아둘 리스트
        List<Vector2Int> target = new List<Vector2Int>();

        // 맵 전체를 돌면서 None인 칸만 target에 넣는다.
        // 즉, 현재 배치 가능한 후보 칸들을 모으는 과정이다.
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                if (Map[x, y] == Type.None)
                {
                    target.Add(new Vector2Int(x, y));
                }
            }
        }

        int mineCount = 0;

        // 원하는 개수만큼 반복하면서 랜덤한 칸에 type 배치
        while (mineCount < amount)
        {
            // 추가 할 공간이 없으면 더이상 추가하지 않는다.
            if (target.Count <= 0)
                break;

            // 후보 칸 중 랜덤 인덱스 선택
            int index = UnityEngine.Random.Range(0, target.Count);

            // 선택된 좌표에 type 배치
            Map[target[index].x, target[index].y] = type;

            mineCount++;

            // 이미 사용한 좌표는 다시 선택되면 안 되므로 제거
            target.RemoveAt(index);
        }

        // 배치 결과 로그 출력
        Debug.Log(type.ToString() + " 배치 완료 : " + mineCount + "개 배치");
    }

    /// <summary>
    /// 맵을 새로 생성하는 함수.
    /// 
    /// 순서:
    /// 1. 배열 생성
    /// 2. 모든 칸 None으로 초기화
    /// 3. 지뢰 랜덤 배치
    /// </summary>
    /// <param name="size">맵 크기</param>
    /// <param name="mineCount">지뢰 개수</param>
    public void MakeMap(Vector2Int size, int mineCount)
    {
        // size.x * size.y 크기의 2차원 배열 생성
        Map = new Type[size.x, size.y];

        // 모든 칸을 None으로 초기화
        // enum 기본값에 의존하지 않고 명시적으로 넣어주는 방식이다.
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Map[x, y] = Type.None;
            }
        }

        // 랜덤하게 Mine 배치
        SetTypeRandom(Type.Mine, mineCount, size);
    }
}