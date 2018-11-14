using UnityEngine;

namespace Azee.NavGrid
{
    public class NavGrid : MonoBehaviour
    {
        [Header("Nav Grid Config")] [SerializeField]
        private int NavUnitSize = 10;

        [SerializeField] private int NavGridSizeX = 10;
        [SerializeField] private int NavGridSizeY = 10;
        [SerializeField] private int NavGridSizeZ = 10;

        [Header("Debug Options")] public bool ShowNavGrid = true;
        public bool ShowNavUnits = true;

        // Start is called before the first frame update
        void Start()
        {
        }

        void OnDrawGizmos()
        {
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = rotationMatrix;

            if (ShowNavGrid)
            {
                Gizmos.color = Color.white;

                Vector3 gridCenter = Vector3.zero;
                gridCenter += Vector3.right * NavGridSizeX;
                gridCenter += Vector3.forward * NavGridSizeZ;
                gridCenter += Vector3.up * NavGridSizeY;
                gridCenter *= NavUnitSize;
                gridCenter /= 2f;

                Gizmos.DrawWireCube(gridCenter, gridCenter * 2f);
            }

            if (ShowNavUnits)
            {
                Gizmos.color = Color.green;

                for (int i = 0; i < NavGridSizeX; i++)
                {
                    for (int j = 0; j < NavGridSizeY; j++)
                    {
                        for (int k = 0; k < NavGridSizeZ; k++)
                        {
                            Vector3 unitLocalCenter = new Vector3(NavUnitSize, NavUnitSize, NavUnitSize) / 2f;

                            Vector3 unitCenter = Vector3.zero;
                            unitCenter += Vector3.right * i;
                            unitCenter += Vector3.up * j;
                            unitCenter += Vector3.forward * k;
                            unitCenter *= NavUnitSize;
                            unitCenter += unitLocalCenter;

                            Gizmos.DrawWireCube(unitCenter, unitLocalCenter * 2f);
                        }
                    }
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
        }
    }
}