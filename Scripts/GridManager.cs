using SuperGrid2D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LiteNetLibManager.SuperGrid2D
{
    /// <summary>
    /// Attach this component to the same game object with LiteNetLibGameManager
    /// it will create dynamic grid when online scene loaded
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        public enum EGenerateGridMode
        {
            Renderer,
            Collider3D,
            Collider2D
        };

        public enum EAxisMode
        {
            XZ,
            XY
        }

        public EGenerateGridMode generateGridMode = EGenerateGridMode.Renderer;
        public EAxisMode axisMode = EAxisMode.XZ;
        public bool includeInactiveComponents = true;
        public float cellSize = 100f;
        private LiteNetLibAssets assets;
        public static DynamicGrid2D<uint, LiteNetLibIdentity> Grid { get; private set; }
        public static EAxisMode AxisMode { get; private set; }

        private void Awake()
        {
            assets = GetComponent<LiteNetLibAssets>();
            assets.onInitialize.AddListener(InitGrid);
        }

        private void OnDestroy()
        {
            assets.onInitialize.RemoveListener(InitGrid);
            Grid = null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (Grid == null) return;
            Vector3 topLeft = GetTopLeft();
            Vector3 cellSize = GetCellSize();
            Vector3 halfCellSize = cellSize * 0.5f;
            Gizmos.color = Color.green;
            for (int y = 0; y < Grid.Rows; ++y)
            {
                for (int x = 0; x < Grid.Rows; ++x)
                {
                    Vector3 offsets = GetOffsets(x, y);
                    Gizmos.DrawWireCube(topLeft + halfCellSize + offsets, cellSize);
                }
            }
        }
#endif

        public Vector3 GetTopLeft()
        {
            switch (AxisMode)
            {
                case EAxisMode.XZ:
                    return new Vector3(Grid.TopLeft.x, 0, Grid.TopLeft.y);
                case EAxisMode.XY:
                    return new Vector3(Grid.TopLeft.x, Grid.TopLeft.y, 0);
            }
            return Vector3.zero;
        }

        public Vector3 GetCellSize()
        {
            switch (AxisMode)
            {
                case EAxisMode.XZ:
                    return new Vector3(Grid.CellSize.x, 0, Grid.CellSize.y);
                case EAxisMode.XY:
                    return new Vector3(Grid.CellSize.x, Grid.CellSize.y, 0);
            }
            return Vector3.zero;
        }

        public Vector3 GetOffsets(int x, int y)
        {
            switch (AxisMode)
            {
                case EAxisMode.XZ:
                    return new Vector3(Grid.CellSize.x * x, 0, Grid.CellSize.y * y);
                case EAxisMode.XY:
                    return new Vector3(Grid.CellSize.x * x, Grid.CellSize.y * y, 0);
            }
            return Vector3.zero;
        }

        private void InitGrid()
        {
            // Collect components
            GameObject[] rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            List<Renderer> tempRenderers = new List<Renderer>();
            List<Collider> tempColliders3D = new List<Collider>();
            List<Collider2D> tempColliders2D = new List<Collider2D>();
            for (int i = 0; i < rootGameObjects.Length; ++i)
            {
                switch (generateGridMode)
                {
                    case EGenerateGridMode.Renderer:
                        tempRenderers.AddRange(rootGameObjects[i].GetComponentsInChildren<Renderer>(includeInactiveComponents));
                        break;
                    case EGenerateGridMode.Collider3D:
                        tempColliders3D.AddRange(rootGameObjects[i].GetComponentsInChildren<Collider>(includeInactiveComponents));
                        break;
                    case EGenerateGridMode.Collider2D:
                        tempColliders2D.AddRange(rootGameObjects[i].GetComponentsInChildren<Collider2D>(includeInactiveComponents));
                        break;
                }
            }
            // Make bounds
            bool setBoundsOnce = false;
            Bounds bounds = default;
            switch (generateGridMode)
            {
                case EGenerateGridMode.Renderer:
                    foreach (Renderer comp in tempRenderers)
                    {
                        if (!setBoundsOnce)
                            bounds = comp.bounds;
                        else
                            bounds.Encapsulate(comp.bounds);
                        setBoundsOnce = true;
                    }
                    break;
                case EGenerateGridMode.Collider3D:
                    foreach (Collider comp in tempColliders3D)
                    {
                        if (!setBoundsOnce)
                            bounds = comp.bounds;
                        else
                            bounds.Encapsulate(comp.bounds);
                        setBoundsOnce = true;
                    }
                    break;
                case EGenerateGridMode.Collider2D:
                    foreach (Collider2D comp in tempColliders2D)
                    {
                        if (!setBoundsOnce)
                            bounds = comp.bounds;
                        else
                            bounds.Encapsulate(comp.bounds);
                        setBoundsOnce = true;
                    }
                    break;
            }
            // Generate grid
            switch (axisMode)
            {
                case EAxisMode.XZ:
                    Grid = new DynamicGrid2D<uint, LiteNetLibIdentity>(
                        new Vector2(bounds.min.x, bounds.min.z),
                        bounds.size.x, bounds.size.z, cellSize);
                    break;
                case EAxisMode.XY:
                    Grid = new DynamicGrid2D<uint, LiteNetLibIdentity>(
                        new Vector2(bounds.min.x, bounds.min.y),
                        bounds.size.x, bounds.size.y, cellSize);
                    break;
            }
            AxisMode = axisMode;
        }
    }
}
