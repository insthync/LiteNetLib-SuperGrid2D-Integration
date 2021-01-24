using SuperGrid2D;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LiteNetLibManager.SuperGrid2D
{
    public class GridVisibleChecker : BaseLiteNetLibVisibleChecker
    {
        public int range = 10;
        public float updateInterval = 1.0f;
        private readonly HashSet<uint> subscribings = new HashSet<uint>();
        private float updateCountDown;
        private bool isSpawned;

        private void Start()
        {
            updateCountDown = updateInterval;
        }

        private void Update()
        {
            if (!IsServer || ConnectionId < 0)
                return;

            updateCountDown -= Time.unscaledDeltaTime;

            if (updateCountDown <= 0f)
            {
                updateCountDown = updateInterval;
                FindObjectsToSubscribe();
                UpdateSubscribings(subscribings);
            }
        }

        private void LateUpdate()
        {
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            if (!IsServer)
                return;
            // Updating position in grid for behaviours that has player only
            if (isSpawned != Identity.IsSpawned)
            {
                isSpawned = Identity.IsSpawned;
                if (isSpawned)
                    GridManager.Grid.Add(ObjectId, Identity, new Point(GetPosition()));
                else
                    GridManager.Grid.Remove(ObjectId);
            }
            if (isSpawned)
            {
                GridManager.Grid.Update(ObjectId, new Point(GetPosition()));
            }
        }

        private void OnDestroy()
        {
            if (GridManager.Grid != null && isSpawned)
                GridManager.Grid.Remove(ObjectId);
        }

        public override HashSet<uint> GetInitializeSubscribings()
        {
            UpdatePosition();
            FindObjectsToSubscribe();
            return subscribings;
        }

        private void FindObjectsToSubscribe()
        {
            subscribings.Clear();
            foreach (LiteNetLibIdentity entry in GridManager.Grid.Contact(new Circle(GetPosition(), range)))
            {
                subscribings.Add(entry.ObjectId);
            }
        }

        public Vector2 GetPosition()
        {
            // find players within range
            switch (GridManager.AxisMode)
            {
                case GridManager.EAxisMode.XZ:
                    return new Vector2(transform.position.x, transform.position.z);
                case GridManager.EAxisMode.XY:
                    return new Vector2(transform.position.x, transform.position.y);
            }
            return Vector2.zero;
        }
    }
}
