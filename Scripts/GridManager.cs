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
    public class GridManager : BaseInterestManager
    {
        public enum EGenerateGridMode
        {
            Renderer,
            Collider3D,
            Collider2D,
            Terrain,
        };

        public enum EAxisMode
        {
            XZ,
            XY
        }

        public bool generateGridByRenderers = true;
        public bool generateGridByCollider3D = false;
        public bool generateGridByCollider2D = false;
        public bool generateGridByTerrain = true;
        public EAxisMode axisMode = EAxisMode.XZ;
        public bool includeInactiveComponents = true;
        public float cellSize = 100f;
        [Tooltip("Update every ? seconds")]
        public float updateInterval = 1.0f;
        public static EAxisMode AxisMode { get; private set; }

        private StaticGrid2D<uint> grid = null;
        private float updateCountDown = 0f;

        protected override void Awake()
        {
            base.Awake();
            Manager.Assets.onInitialize.AddListener(InitGrid);
        }

        private void OnDestroy()
        {
            Manager.Assets.onInitialize.RemoveListener(InitGrid);
            grid = null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (grid == null) return;
            Vector3 topLeft = GetTopLeft(grid);
            Vector3 cellSize = GetCellSize(grid);
            Vector3 halfCellSize = cellSize * 0.5f;
            Gizmos.color = Color.green;
            for (int y = 0; y < grid.Rows; ++y)
            {
                for (int x = 0; x < grid.Rows; ++x)
                {
                    Vector3 offsets = GetOffsets(grid, x, y);
                    Gizmos.DrawWireCube(topLeft + halfCellSize + offsets, cellSize);
                }
            }
        }
#endif

        public Vector3 GetTopLeft(IGridDimensions2D grid)
        {
            switch (AxisMode)
            {
                case EAxisMode.XZ:
                    return new Vector3(grid.TopLeft.x, 0, grid.TopLeft.y);
                case EAxisMode.XY:
                    return new Vector3(grid.TopLeft.x, grid.TopLeft.y, 0);
            }
            return Vector3.zero;
        }

        public Vector3 GetCellSize(IGridDimensions2D grid)
        {
            switch (AxisMode)
            {
                case EAxisMode.XZ:
                    return new Vector3(grid.CellSize.x, 0, grid.CellSize.y);
                case EAxisMode.XY:
                    return new Vector3(grid.CellSize.x, grid.CellSize.y, 0);
            }
            return Vector3.zero;
        }

        public Vector3 GetOffsets(IGridDimensions2D grid, int x, int y)
        {
            switch (AxisMode)
            {
                case EAxisMode.XZ:
                    return new Vector3(grid.CellSize.x * x, 0, grid.CellSize.y * y);
                case EAxisMode.XY:
                    return new Vector3(grid.CellSize.x * x, grid.CellSize.y * y, 0);
            }
            return Vector3.zero;
        }

        public Vector2 GetPosition(LiteNetLibIdentity identity)
        {
            // find players within range
            switch (AxisMode)
            {
                case EAxisMode.XZ:
                    return new Vector2(identity.transform.position.x, identity.transform.position.z);
                case EAxisMode.XY:
                    return new Vector2(identity.transform.position.x, identity.transform.position.y);
            }
            return Vector2.zero;
        }

        private void InitGrid()
        {
            // Collect components
            GameObject[] rootGameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
            List<Renderer> tempRenderers = new List<Renderer>();
            List<Collider> tempColliders3D = new List<Collider>();
            List<Collider2D> tempColliders2D = new List<Collider2D>();
            List<TerrainCollider> terrainColliders = new List<TerrainCollider>();
            for (int i = 0; i < rootGameObjects.Length; ++i)
            {
                if (generateGridByRenderers)
                    tempRenderers.AddRange(rootGameObjects[i].GetComponentsInChildren<Renderer>(includeInactiveComponents));
                if (generateGridByCollider3D)
                    tempColliders3D.AddRange(rootGameObjects[i].GetComponentsInChildren<Collider>(includeInactiveComponents));
                if (generateGridByCollider2D)
                    tempColliders2D.AddRange(rootGameObjects[i].GetComponentsInChildren<Collider2D>(includeInactiveComponents));
                if (generateGridByTerrain)
                    terrainColliders.AddRange(rootGameObjects[i].GetComponentsInChildren<TerrainCollider>(includeInactiveComponents));
            }
            // Make bounds
            bool setBoundsOnce = false;
            Bounds bounds = default;
            if (generateGridByRenderers)
            {
                foreach (Renderer comp in tempRenderers)
                {
                    if (!setBoundsOnce)
                        bounds = comp.bounds;
                    else
                        bounds.Encapsulate(comp.bounds);
                    setBoundsOnce = true;
                }
            }
            if (generateGridByCollider3D)
            {
                foreach (Collider comp in tempColliders3D)
                {
                    if (!setBoundsOnce)
                        bounds = comp.bounds;
                    else
                        bounds.Encapsulate(comp.bounds);
                    setBoundsOnce = true;
                }
            }
            if (generateGridByCollider2D)
            {
                foreach (Collider2D comp in tempColliders2D)
                {
                    if (!setBoundsOnce)
                        bounds = comp.bounds;
                    else
                        bounds.Encapsulate(comp.bounds);
                    setBoundsOnce = true;
                }
            }
            if (generateGridByTerrain)
            {
                foreach (TerrainCollider comp in terrainColliders)
                {
                    if (!setBoundsOnce)
                        bounds = comp.terrainData.bounds;
                    else
                        bounds.Encapsulate(comp.terrainData.bounds);
                    setBoundsOnce = true;
                }
            }
            // Generate grid
            switch (axisMode)
            {
                case EAxisMode.XZ:
                    grid = new StaticGrid2D<uint>(
                        new Vector2(bounds.min.x, bounds.min.z),
                        bounds.size.x, bounds.size.z, cellSize);
                    break;
                case EAxisMode.XY:
                    grid = new StaticGrid2D<uint>(
                        new Vector2(bounds.min.x, bounds.min.y),
                        bounds.size.x, bounds.size.y, cellSize);
                    break;
            }
            AxisMode = axisMode;
        }

        private void Update()
        {
            if (!IsServer || grid == null)
            {
                // Update at server only
                return;
            }
            updateCountDown -= Time.unscaledDeltaTime;
            if (updateCountDown <= 0)
            {
                updateCountDown = updateInterval;
                grid.Clear();
                foreach (LiteNetLibIdentity spawnedObject in Manager.Assets.GetSpawnedObjects())
                {
                    if (spawnedObject != null)
                        grid.Add(spawnedObject.ObjectId, new Circle(GetPosition(spawnedObject), GetVisibleRange(spawnedObject)));
                }
                HashSet<uint> subscribings = new HashSet<uint>();
                foreach (LiteNetLibPlayer player in Manager.GetPlayers())
                {
                    if (!player.IsReady)
                    {
                        // Don't subscribe if player not ready
                        continue;
                    }
                    foreach (LiteNetLibIdentity playerObject in player.GetSpawnedObjects())
                    {
                        // Update subscribing list, it will unsubscribe objects which is not in this list
                        subscribings.Clear();
                        LiteNetLibIdentity contactedObject;
                        foreach (uint contactedObjectId in grid.Contact(new Point(GetPosition(playerObject))))
                        {
                            if (Manager.Assets.TryGetSpawnedObject(contactedObjectId, out contactedObject) &&
                                ShouldSubscribe(playerObject, contactedObject, false))
                                subscribings.Add(contactedObjectId);
                        }
                        playerObject.UpdateSubscribings(subscribings);
                    }
                }
            }
        }
    }
}
