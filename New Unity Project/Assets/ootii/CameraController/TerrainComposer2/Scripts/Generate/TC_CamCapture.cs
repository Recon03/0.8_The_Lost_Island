using UnityEngine;
using System.Collections;

namespace TerrainComposer2
{
    [ExecuteInEditMode]
    public class TC_CamCapture : MonoBehaviour
    {
        public Camera cam;
        public int collisionMask;
        [System.NonSerialized] public Terrain terrain;
        Transform t;
        public CollisionDirection collisionDirection;

        void Start()
        {
            t = transform;
            cam = GetComponent<Camera>();
            cam.aspect = 1;
        }
        
        public void Capture(int collisionMask, CollisionDirection collisionDirection, int outputId)
        {
            if (TC_Area2D.current.currentTerrainArea == null) return;
            // Debug.Log("Capture");
            this.collisionMask = collisionMask;
            terrain = TC_Area2D.current.currentTerrain;
            // this.collisionDirection = collisionDirection;
            cam.cullingMask = collisionMask;

            SetCamera(collisionDirection, outputId);

            cam.Render();
        }

        public void SetCamera(CollisionDirection collisionDirection, int outputId)
        {
            if (t == null) Start();

            if (collisionDirection == CollisionDirection.Up)
            {
                t.position = new Vector3(TC_Area2D.current.bounds.center.x, -1, TC_Area2D.current.bounds.center.z);
                t.rotation = Quaternion.Euler(-90, 0, 0);
            }
            else
            {
                t.position = new Vector3(TC_Area2D.current.bounds.center.x, TC_Area2D.current.bounds.center.y + 1, TC_Area2D.current.bounds.center.z);
                t.rotation = Quaternion.Euler(90, 0, 0);
            }
                
            float orthographicSize = TC_Area2D.current.bounds.extents.x;

            if (outputId == TC.heightOutput) orthographicSize += TC_Area2D.current.resExpandBorderSize;

            cam.orthographicSize = orthographicSize;

            cam.nearClipPlane = 0;
            cam.farClipPlane = TC_Area2D.current.currentTerrainArea.terrainSize.y + 1;

            // Debug.Log(t.position);

            // Vector3 size = area.currentTerrain.terrainData.size;
            // t.position = new Vector3(area.area.center.x, -1, area.area.center.y);
        }
    }
}