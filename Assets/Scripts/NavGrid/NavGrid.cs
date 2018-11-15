using UnityEngine;

namespace Azee.PathFinding3D
{
    [ExecuteInEditMode]
    public class NavGrid : MonoBehaviour
    {
        #region InspectorFields

        [Header("Nav Grid Config")] [SerializeField]
        private int _navUnitSize = 10;

        [SerializeField] private int _navGridSizeX = 10;
        [SerializeField] private int _navGridSizeY = 10;
        [SerializeField] private int _navGridSizeZ = 10;

        [SerializeField] private bool _allowGridRotation = false;

        [Header("Debug Options")] public bool ShowNavGrid = true;
        public bool ShowNavUnits = true;
        public bool ShowNavigableUnits = true;
        public bool ShowNonNavigableUnits = true;

        #endregion

        #region NonInspectorFields

        private NavUnit[,,] _navUnits;

        #endregion


        #region Unity API

        void Awake()
        {
            InitIfNeeded();
        }

        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
            InitIfNeeded();
            CheckGridRotation();
            UpdateNavUnits();
        }

        void OnDrawGizmos()
        {
            InitIfNeeded();
            DrawNavGizmos();
        }

        #endregion


        #region Implementation

        void InitIfNeeded()
        {
            _navGridSizeX = Mathf.Abs(_navGridSizeX);
            _navGridSizeY = Mathf.Abs(_navGridSizeY);
            _navGridSizeZ = Mathf.Abs(_navGridSizeZ);

            if (_navUnits == null
                || _navUnits.GetLength(0) != _navGridSizeX
                || _navUnits.GetLength(1) != _navGridSizeY
                || _navUnits.GetLength(2) != _navGridSizeZ)
            {
                ResetNavUnits();
            }
        }

        void CheckGridRotation()
        {
            if (!_allowGridRotation && transform.rotation.eulerAngles != Vector3.zero)
            {
                transform.rotation = Quaternion.Euler(Vector3.zero);
            }
        }

        void ResetNavUnits()
        {
            _navUnits = new NavUnit[_navGridSizeX, _navGridSizeY, _navGridSizeZ];
            for (int i = 0; i < _navUnits.GetLength(0); i++)
            {
                for (int j = 0; j < _navUnits.GetLength(1); j++)
                {
                    for (int k = 0; k < _navUnits.GetLength(2); k++)
                    {
                        _navUnits[i, j, k] = new NavUnit(i, j, k, this);
                    }
                }
            }
        }

        void UpdateNavUnits()
        {
            for (int i = 0; i < _navUnits.GetLength(0); i++)
            {
                for (int j = 0; j < _navUnits.GetLength(1); j++)
                {
                    for (int k = 0; k < _navUnits.GetLength(2); k++)
                    {
                        _navUnits[i, j, k].Update();
                    }
                }
            }
        }

        void DrawNavGizmos()
        {
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = rotationMatrix;

            if (ShowNavGrid)
            {
                Gizmos.color = Color.white;

                Vector3 gridCenter = Vector3.zero;
                gridCenter += Vector3.right * _navGridSizeX;
                gridCenter += Vector3.forward * _navGridSizeZ;
                gridCenter += Vector3.up * _navGridSizeY;
                gridCenter *= _navUnitSize;
                gridCenter /= 2f;

                Gizmos.DrawWireCube(gridCenter, gridCenter * 2f);
            }

            if (ShowNavUnits && _navUnits != null)
            {
                for (int i = 0; i < _navUnits.GetLength(0); i++)
                {
                    for (int j = 0; j < _navUnits.GetLength(1); j++)
                    {
                        for (int k = 0; k < _navUnits.GetLength(2); k++)
                        {
                            NavUnit navUnit = _navUnits[i, j, k];
                            Bounds relativeBounds = navUnit.GetRelativeBounds();

                            if (navUnit.IsNavigable() && ShowNavigableUnits)
                            {
                                Gizmos.color = Color.green;
                                Gizmos.DrawWireCube(relativeBounds.center, relativeBounds.size);
                            }
                            else if (!navUnit.IsNavigable() && ShowNonNavigableUnits)
                            {
                                Gizmos.color = Color.red;
                                Gizmos.DrawWireCube(relativeBounds.center, relativeBounds.size);
                            }
                        }
                    }
                }
            }
        }

        #endregion


        #region Interface

        public int GetNavUnitSize()
        {
            return _navUnitSize;
        }

        #endregion
    }
}