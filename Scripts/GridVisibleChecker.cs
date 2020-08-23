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

        void Start()
        {
            updateCountDown = updateInterval + Random.value;
        }

        void Update()
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
            float x = 0f;
            float y = 0f;
            // find players within range
            switch (GridManager.AxisMode)
            {
                case GridManager.EAxisMode.XZ:
                    x = transform.position.x;
                    y = transform.position.z;
                    break;
                case GridManager.EAxisMode.XY:
                    x = transform.position.x;
                    y = transform.position.y;
                    break;
            }
            foreach (LiteNetLibIdentity entry in GridManager.Grid.Contact(new Circle(x, y, range)))
            {
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
    }
}
