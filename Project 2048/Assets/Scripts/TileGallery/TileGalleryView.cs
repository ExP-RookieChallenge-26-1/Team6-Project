using Project2048.Prototype;
using UnityEngine;
using UnityEngine.UI;

namespace Project2048.TileGallery
{
    /// <summary>
    /// BoardCellView 프리팹을 그대로 인스턴스화해서 2~2048 값을 띄운다.
    /// Inspector에서 cellPrefab에 배틀씬 셀 프리팹을 연결하면 된다.
    /// </summary>
    public class TileGalleryView : MonoBehaviour
    {
        private static readonly int[] TileValues = { 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048 };

        [SerializeField] private BoardCellView cellPrefab;

        // BoardGrid와 동일한 값
        [Header("Board Grid (BattleScene 동일값)")]
        [SerializeField] private Vector2 cellSize = new(140f, 140f);
        [SerializeField] private Vector2 cellSpacing = new(16f, 16f);
        [SerializeField] private int columnCount = 4;
        [SerializeField] private Vector2 anchorNormalized = new(0.5f, 0.4f);
        [SerializeField] private Vector2 gridSizeDelta = new(620f, 620f);

        private void Start()
        {
            if (cellPrefab == null)
            {
                Debug.LogError("[TileGalleryView] cellPrefab이 연결되지 않았습니다.");
                return;
            }

            BuildGrid();
        }

        private void BuildGrid()
        {
            var grid = new GameObject("TileGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            grid.transform.SetParent(transform, false);

            var rect = grid.GetComponent<RectTransform>();
            rect.anchorMin = anchorNormalized;
            rect.anchorMax = anchorNormalized;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = gridSizeDelta;

            var layout = grid.GetComponent<GridLayoutGroup>();
            layout.cellSize = cellSize;
            layout.spacing = cellSpacing;
            layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            layout.startAxis = GridLayoutGroup.Axis.Horizontal;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = columnCount;

            foreach (var value in TileValues)
            {
                var cell = Instantiate(cellPrefab, grid.transform);
                cell.SetValue(value, Color.clear, Color.clear, Color.clear, Color.clear);
            }

            // 나머지 5칸 더미
            for (var i = TileValues.Length; i < 16; i++)
            {
                var dummy = new GameObject("Dummy", typeof(RectTransform), typeof(Image));
                dummy.transform.SetParent(grid.transform, false);
                dummy.GetComponent<Image>().color = Color.clear;
                dummy.GetComponent<Image>().raycastTarget = false;
            }
        }
    }
}
