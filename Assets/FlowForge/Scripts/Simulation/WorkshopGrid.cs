using UnityEngine;

namespace FlowForge.Simulation
{
    /// <summary>
    /// Generates a lightweight isometric-friendly workshop grid from primitive meshes.
    /// Art can replace these placeholders without changing gameplay logic.
    /// </summary>
    public class WorkshopGrid : MonoBehaviour
    {
        [SerializeField] private int width = 8;
        [SerializeField] private int height = 6;
        [SerializeField] private float cellSize = 1.4f;
        [SerializeField] private Color baseColor = new Color(0.78f, 0.82f, 0.86f);
        [SerializeField] private Color alternateColor = new Color(0.86f, 0.88f, 0.9f);

        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;

        public void Build(int newWidth, int newHeight, Color floorColor)
        {
            width = newWidth;
            height = newHeight;
            baseColor = floorColor;
            ClearChildren();

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tile.name = $"Grid Cell {x},{y}";
                    tile.transform.SetParent(transform, false);
                    tile.transform.localPosition = GridToWorld(x, y);
                    tile.transform.localScale = new Vector3(cellSize * 0.96f, 0.08f, cellSize * 0.96f);

                    var renderer = tile.GetComponent<Renderer>();
                    renderer.material = new Material(Shader.Find("Standard"));
                    renderer.material.color = (x + y) % 2 == 0 ? baseColor : alternateColor;
                }
            }
        }

        public Vector3 GridToWorld(int x, int y)
        {
            return new Vector3(x * cellSize, 0f, y * cellSize);
        }

        private void ClearChildren()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }
    }
}
