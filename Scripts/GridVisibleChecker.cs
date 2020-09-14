using SuperGrid2D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LiteNetLibManager.SuperGrid2D
{
    public class GridVisibleChecker : LiteNetLibBehaviour
    {
        public int range = 10;
        public float updateInterval = 1.0f;
        private float updateCountDown;
        private bool previouslyHasPlayer;

        public override void OnSetup()
        {
            base.OnSetup();
            UpdatePosition();
        }

        private void Start()
        {
            updateCountDown = updateInterval;
        }

        private void Update()
        {
            if (!IsServer)
                return;

            updateCountDown -= Time.unscaledDeltaTime;

            if (updateCountDown <= 0f)
            {
                updateCountDown = updateInterval;
                // Request identity to rebuild subscribers
                Identity.RebuildSubscribers(false);
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
            // Updating position in grid for behavours that has player only
            if (previouslyHasPlayer != ConnectionId >= 0)
            {
                previouslyHasPlayer = ConnectionId >= 0;
                if (previouslyHasPlayer)
                    GridManager.Grid.Add(ObjectId, Identity, new Point(GetPosition()));
                else
                    GridManager.Grid.Remove(ObjectId);
            }
            if (previouslyHasPlayer)
            {
                GridManager.Grid.Update(ObjectId, new Point(GetPosition()));
            }
        }

        private void OnDestroy()
        {
            if (GridManager.Grid != null && previouslyHasPlayer)
                GridManager.Grid.Remove(ObjectId);
        }

        public override bool ShouldAddSubscriber(LiteNetLibPlayer subscriber)
        {
            if (subscriber == null)
                return false;

            if (subscriber.ConnectionId == ConnectionId)
                return true;

            Vector3 pos;
            foreach (LiteNetLibIdentity spawnedObject in subscriber.GetSpawnedObjects())
            {
                pos = spawnedObject.transform.position;
                if ((pos - transform.position).sqrMagnitude < range * range)
                    return true;
            }
            return false;
        }

        public override bool OnRebuildSubscribers(HashSet<LiteNetLibPlayer> subscribers, bool initialize)
        {
            foreach (LiteNetLibIdentity entry in GridManager.Grid.Contact(new Circle(GetPosition(), range)))
            {
                if (entry != null && entry.Player != null)
                    subscribers.Add(entry.Player);
            }
            return true;
        }

        public override void OnServerSubscribingAdded()
        {
            base.OnServerSubscribingAdded();
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                return;
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; ++i)
            {
                renderers[i].forceRenderingOff = false;
            }
        }

        public override void OnServerSubscribingRemoved()
        {
            base.OnServerSubscribingRemoved();
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                return;
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; ++i)
            {
                renderers[i].forceRenderingOff = true;
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
