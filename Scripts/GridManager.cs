using SuperGrid2D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

namespace LiteNetLibManager.SuperGrid2D
{
    /// <summary>
    /// Attach this component to the same game object with LiteNetLibGameManager
    /// it will create dynamic grid when online scene loaded
    /// </summary>
    public class GridManager : BaseInterestManager
    {
        public enum EAxisMode
        {
            XZ,
            XY
        }

        private struct CellObject
        {
            public uint objectId;
            public Circle shape;
        }

        public EAxisMode axisMode = EAxisMode.XZ;
        public bool includeInactiveComponents = true;
        public float cellSize = 100f;
        [Tooltip("Update every ? seconds")]
        public float updateInterval = 1.0f;
        public static EAxisMode AxisMode { get; private set; }

        private float _lastUpdateTime = float.MinValue;
        private List<CellObject> _cellObjects = new List<CellObject>(1024);

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

        private void Update()
        {
            if (!IsServer)
            {
                // Update at server only
                return;
            }

            float currentTime = Time.unscaledTime;
            if (currentTime - _lastUpdateTime < updateInterval)
                return;
            _lastUpdateTime = currentTime;

            Profiler.BeginSample("GridManager - Update");
            _cellObjects.Clear();
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;

            Vector2 tempPosition;
            foreach (LiteNetLibIdentity spawnedObject in Manager.Assets.GetSpawnedObjects())
            {
                if (spawnedObject == null)
                    continue;
                tempPosition = GetPosition(spawnedObject);
                _cellObjects.Add(new CellObject()
                {
                    objectId = spawnedObject.ObjectId,
                    shape = new Circle(tempPosition, GetVisibleRange(spawnedObject)),
                });
                if (tempPosition.x < minX)
                    minX = tempPosition.x;
                if (tempPosition.y < minY)
                    minY = tempPosition.y;
                if (tempPosition.x > maxX)
                    maxX = tempPosition.x;
                if (tempPosition.y > maxY)
                    maxY = tempPosition.y;
            }

            float width = maxX - minX;
            float height = maxY - minY;
            if (width > 0 && height > 0)
            {
                StaticGrid2D<uint> grid = new StaticGrid2D<uint>(new Vector2(minX, minY), width, height, cellSize);
                for (int i = 0; i < _cellObjects.Count; ++i)
                {
                    grid.Add(_cellObjects[i].objectId, _cellObjects[i].shape);
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
            Profiler.EndSample();
        }
    }
}
