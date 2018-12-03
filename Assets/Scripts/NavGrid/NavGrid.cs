using System;
using System.Collections.Generic;
using System.IO;
using BasicTools.ButtonInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Azee.PathFinding3D
{
    [ExecuteInEditMode]
    public class NavGrid : MonoBehaviour
    {
        #region Inspector Fields

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

        [Button("Bake Nav Grid", "BakeNavGrid")]
        public bool BtnBakeNavGrid;

        [Button("Reset Baked Data", "RemoveBakedData")]
        public bool BtnRemoveBakedData;

        [SerializeField] [ReadOnly] private string _bakeDataFileName;

        #endregion

        #region Non Inspector Fields

        public static NavGrid Instance { get; private set; }
        private NavUnit[,,] _navUnits = new NavUnit[10, 10, 10];

        #endregion


        #region Unity API

        private void Awake()
        {
            Instance = this;
            ValidateGridSize();
        }

        // Start is called before the first frame update
        private void Start()
        {
            LoadBakedData();
        }

        // Update is called once per frame
        private void Update()
        {
            ValidateGridSize();
            CheckGridRotation();
        }

        private void OnDrawGizmosSelected()
        {
            ValidateGridSize();
            DrawNavGizmos();
        }

        #endregion


        #region Implementation

        void ValidateGridSize()
        {
            _navGridSizeX = Mathf.Abs(_navGridSizeX);
            _navGridSizeY = Mathf.Abs(_navGridSizeY);
            _navGridSizeZ = Mathf.Abs(_navGridSizeZ);
        }

        void CheckGridRotation()
        {
            if (!_allowGridRotation && transform.rotation.eulerAngles != Vector3.zero)
            {
                transform.rotation = Quaternion.Euler(Vector3.zero);
            }
        }

        void LoadBakedData()
        {
            if (_bakeDataFileName.Length > 0 && File.Exists(_bakeDataFileName))
            {
                string bakedDataJson = File.ReadAllText(_bakeDataFileName);

                NavGridBakeDataModel navGridBakeData = JsonUtility.FromJson<NavGridBakeDataModel>(bakedDataJson);
                _navUnitSize = navGridBakeData.NavUnitSize;
                _navGridSizeX = navGridBakeData.NavGridSizeX;
                _navGridSizeY = navGridBakeData.NavGridSizeY;
                _navGridSizeZ = navGridBakeData.NavGridSizeZ;

                ResetNavUnits();

                int l = 0;
                for (int i = 0; i < _navUnits.GetLength(0); i++)
                {
                    for (int j = 0; j < _navUnits.GetLength(1); j++)
                    {
                        for (int k = 0; k < _navUnits.GetLength(2); k++)
                        {
                            _navUnits[i, j, k]
                                .SetNavigable(navGridBakeData.NavUnits[l].IsNavigable != 0 ? true : false);
                            l++;
                        }
                    }
                }
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

        void DrawNavGizmos()
        {
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = rotationMatrix;

            if (ShowNavGrid)
            {
                Gizmos.color = Color.white;

                Vector3 gridCenter = Vector3.zero;
                gridCenter += Vector3.right * _navUnits.GetLength(0);
                gridCenter += Vector3.forward * _navUnits.GetLength(1);
                gridCenter += Vector3.up * _navUnits.GetLength(2);
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
                            else if (navUnit.HighlightColor != Color.black)
                            {
                                Gizmos.color = navUnit.HighlightColor;
                                Gizmos.DrawWireCube(relativeBounds.center, relativeBounds.size);
                            }
                        }
                    }
                }
            }
        }

        
        #region Editor Helper Functions

#if UNITY_EDITOR
        void RemoveBakedData()
        {
            ResetNavUnits();
            if (_bakeDataFileName.Length > 0 && File.Exists(_bakeDataFileName))
            {
                File.Delete(_bakeDataFileName);
            }

            _bakeDataFileName = "";

            SceneView.RepaintAll();
        }

        void BakeNavGrid()
        {
            ResetNavUnits();

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

            SceneView.RepaintAll();

            NavGridBakeDataModel navGridBakeData = new NavGridBakeDataModel();
            navGridBakeData.NavUnitSize = _navUnitSize;
            navGridBakeData.NavGridSizeX = _navGridSizeX;
            navGridBakeData.NavGridSizeY = _navGridSizeY;
            navGridBakeData.NavGridSizeZ = _navGridSizeZ;

            navGridBakeData.NavUnits =
                new NavUnitBakeDataModel[_navUnits.GetLength(0) * _navUnits.GetLength(1) * _navUnits.GetLength(2)];

            int l = 0;
            for (int i = 0; i < _navUnits.GetLength(0); i++)
            {
                for (int j = 0; j < _navUnits.GetLength(1); j++)
                {
                    for (int k = 0; k < _navUnits.GetLength(2); k++)
                    {
                        navGridBakeData.NavUnits[l] = new NavUnitBakeDataModel
                        {
                            IsNavigable = _navUnits[i, j, k].IsNavigable() ? 1 : 0
                        };
                        l++;
                    }
                }
            }

            string bakeDataDir = SceneManager.GetActiveScene().path;
            bakeDataDir = bakeDataDir.Substring(0, bakeDataDir.Length - Path.GetFileName(bakeDataDir).Length) + "NavGridData/";
            
            Directory.CreateDirectory(bakeDataDir);

            _bakeDataFileName = bakeDataDir + "navGridBakedData";
            
            File.WriteAllText(_bakeDataFileName, JsonUtility.ToJson(navGridBakeData));
            
            AssetDatabase.Refresh();
        }
#endif

        #endregion


        #region AStar

        private List<NavUnit> backTracePath(NavUnit endNavUnit)
        {
            Stack<NavUnit> pathStack = new Stack<NavUnit>();

            NavUnit cur = endNavUnit;
            while (cur != null)
            {
                pathStack.Push(cur);
                if (cur.AStarData.Parent != cur)
                {
                    cur = cur.AStarData.Parent;
                }
                else
                {
                    cur = null;
                }
            }

            List<NavUnit> path = new List<NavUnit>();
            while (pathStack.Count > 0)
            {
                path.Add(pathStack.Pop());
            }

            return path;
        }

        private List<NavUnit> FindPathUsingAStar(Vector3 from, Vector3 to)
        {
            List<NavUnit> path = new List<NavUnit>();

            NavUnit startNavUnit = GetNavUnit(from);
            NavUnit endNavUnit = GetNavUnit(to);

            if (startNavUnit == null || endNavUnit == null
                                     || !startNavUnit.IsNavigable() || !endNavUnit.IsNavigable()
                                     || startNavUnit == endNavUnit)
            {
                return path;
            }

            Bounds endNavUnitBounds = endNavUnit.GetRelativeBounds();

            List<NavUnit> openList = new List<NavUnit>();
            List<NavUnit> closedList = new List<NavUnit>();

            for (int i = 0; i < _navUnits.GetLength(0); i++)
            {
                for (int j = 0; j < _navUnits.GetLength(1); j++)
                {
                    for (int k = 0; k < _navUnits.GetLength(2); k++)
                    {
                        NavUnit navUnit = _navUnits[i, j, k];
                        navUnit.AStarData.G = 0;
                        navUnit.AStarData.H = float.MaxValue;
                        navUnit.AStarData.F = float.MaxValue;
                        navUnit.AStarData.Parent = null;
                    }
                }
            }

            openList.Add(startNavUnit);

            while (openList.Count > 0)
            {
                openList.Sort();

                NavUnit curNavUnit = openList[0];
                openList.RemoveAt(0);

                Bounds curNavUnitBounds = curNavUnit.GetRelativeBounds();

                List<NavUnit> neighbors = curNavUnit.GetNeighbors();
                foreach (NavUnit neighbor in neighbors)
                {
                    if (neighbor == endNavUnit)
                    {
                        neighbor.AStarData.Parent = curNavUnit;

                        return backTracePath(endNavUnit);
                    }
                    else if (!closedList.Contains(neighbor))
                    {
                        Bounds neighborBounds = neighbor.GetRelativeBounds();

                        float newG = curNavUnit.AStarData.G +
                                     Vector3.Distance(curNavUnitBounds.center, neighborBounds.center);
                        float newH = Vector3.Distance(neighborBounds.center, endNavUnitBounds.center);
                        float newF = newG + newH;

                        if (newF < neighbor.AStarData.F)
                        {
                            neighbor.AStarData.Parent = curNavUnit;
                            neighbor.AStarData.G = newG;
                            neighbor.AStarData.H = newH;
                            neighbor.AStarData.F = newF;
                            openList.Add(neighbor);
                        }
                    }
                }

                closedList.Add(curNavUnit);
            }

            return path;
        }

        #endregion

        #endregion


        #region Interface

        public int GetNavUnitSize()
        {
            return _navUnitSize;
        }

        public NavUnit GetNavUnit(int row, int col, int depth)
        {
            if (row >= 0 && row < _navUnits.GetLength(0)
                && col >= 0 && col < _navUnits.GetLength(1)
                && depth >= 0 && depth < _navUnits.GetLength(2))
            {
                return _navUnits[row, col, depth];
            }

            return null;
        }

        public NavUnit GetNavUnit(Vector3 pos)
        {
            Vector3 offset = transform.InverseTransformPoint(pos);
            offset.x /= _navUnitSize;
            offset.y /= _navUnitSize;
            offset.z /= _navUnitSize;

            int row = (int) offset.x;
            int col = (int) offset.y;
            int depth = (int) offset.z;

            return GetNavUnit(row, col, depth);
        }

        public List<NavUnit> GetShortestPath(Vector3 from, Vector3 to)
        {
            return FindPathUsingAStar(from, to);
        }

        #endregion
    }

    [Serializable]
    public class NavGridBakeDataModel
    {
        public int NavUnitSize = 10;
        public int NavGridSizeX = 10;
        public int NavGridSizeY = 10;
        public int NavGridSizeZ = 10;

        public NavUnitBakeDataModel[] NavUnits;
    }
}