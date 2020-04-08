using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BasicTools.ButtonInspector;
using JetBrains.Annotations;
using Priority_Queue;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
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

        [SerializeField] public int navGridSizeX = 10;
        [SerializeField] public int navGridSizeY = 10;
        [SerializeField] public int navGridSizeZ = 10;

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
        private NativeArray<NavUnit> _navUnits;

        private NativeArray<int3> _neighborOffsets;

        #endregion


        #region Unity API

        private void Awake()
        {
            Instance = this;

            if (_navUnits.IsCreated)
            {
                _navUnits.Dispose();
            }
            _navUnits = new NativeArray<NavUnit>(1000, Allocator.Persistent);

            ValidateGrid();
            ComputeNeighborOffsets();
        }

        // Start is called before the first frame update
        private void Start()
        {
            LoadBakedData();
        }

        // Update is called once per frame
        private void Update()
        {
            ValidateGrid();
            CheckGridRotation();
        }

        private void OnDrawGizmosSelected()
        {
            ValidateGrid();
            DrawNavGizmos();
        }

        private void OnDestroy()
        {
            if (_navUnits.IsCreated)
            {
                _navUnits.Dispose();
            }

            if (_neighborOffsets.IsCreated)
            {
                _neighborOffsets.Dispose();
            }
        }

        #endregion


        #region Implementation

        void ValidateGrid()
        {
            navGridSizeX = Mathf.Abs(navGridSizeX);
            navGridSizeY = Mathf.Abs(navGridSizeY);
            navGridSizeZ = Mathf.Abs(navGridSizeZ);
        }

        void CheckGridRotation()
        {
            if (!_allowGridRotation && transform.rotation.eulerAngles != Vector3.zero)
            {
                transform.rotation = Quaternion.Euler(Vector3.zero);
            }
        }

        void ComputeNeighborOffsets()
        {
            if (_neighborOffsets.IsCreated)
            {
                _neighborOffsets.Dispose();
            }

            _neighborOffsets = new NativeArray<int3>(26, Allocator.Persistent);

            int l = 0;
            for (int j = -1; j <= 1; j++)
            {
                for (int k = -1; k <= 1; k++)
                {
                    for (int i = -1; i <= 1; i++)
                    {
                        if (i != 0 || j != 0 || k != 0)
                        {
                            _neighborOffsets[l] = new int3(i, j, k);
                            l++;
                        }
                    }
                }
            }
        }

        void LoadBakedData()
        {
            if (_bakeDataFileName.Length > 0 && File.Exists(_bakeDataFileName))
            {
                string bakedDataJson = File.ReadAllText(_bakeDataFileName);

                NavGridBakeDataModel navGridBakeData = JsonUtility.FromJson<NavGridBakeDataModel>(bakedDataJson);
                _navUnitSize = navGridBakeData.NavUnitSize;
                navGridSizeX = navGridBakeData.NavGridSizeX;
                navGridSizeY = navGridBakeData.NavGridSizeY;
                navGridSizeZ = navGridBakeData.NavGridSizeZ;

                ResetNavUnits();

                int l = 0;
                for (int j = 0; j < navGridSizeY; j++)
                {
                    for (int k = 0; k < navGridSizeZ; k++)
                    {
                        for (int i = 0; i < navGridSizeX; i++)
                        {
                            _navUnits[l] = _navUnits[l].UpdateFromBakedData(this, navGridBakeData.NavUnits[l]);
                            l++;
                        }
                    }
                }
            }
        }

        void ResetNavUnits()
        {
            if (_navUnits.IsCreated)
            {
                _navUnits.Dispose();
            }

            _navUnits = new NativeArray<NavUnit>(navGridSizeX * navGridSizeY * navGridSizeZ, Allocator.Persistent);

            int l = 0;
            for (int j = 0; j < navGridSizeY; j++)
            {
                for (int k = 0; k < navGridSizeZ; k++)
                {
                    for (int i = 0; i < navGridSizeX; i++)
                    {
                        _navUnits[l] = new NavUnit(i, j, k, this);
                        l++;
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
                gridCenter += Vector3.right * navGridSizeX;
                gridCenter += Vector3.up * navGridSizeY;
                gridCenter += Vector3.forward * navGridSizeZ;
                gridCenter *= _navUnitSize;
                gridCenter /= 2f;

                Gizmos.DrawWireCube(gridCenter, gridCenter * 2f);
            }

            if (ShowNavUnits)
            {
                // print(_navUnits.Length);

                int l = 0;
                for (int j = 0; j < navGridSizeY; j++)
                {
                    for (int k = 0; k < navGridSizeZ; k++)
                    {
                        for (int i = 0; i < navGridSizeX; i++)
                        {
                            if (l < _navUnits.Length)
                            {
                                Bounds relativeBounds = _navUnits[l].GetRelativeBounds();

                                if (_navUnits[l].IsNavigable() && ShowNavigableUnits)
                                {
                                    Gizmos.color = Color.green;
                                    Gizmos.DrawWireCube(relativeBounds.center, relativeBounds.size);
                                }
                                else if (!_navUnits[l].IsNavigable() && ShowNonNavigableUnits)
                                {
                                    Gizmos.color = Color.red;
                                    Gizmos.DrawWireCube(relativeBounds.center, relativeBounds.size);
                                }
                                else if (_navUnits[l].HighlightColor != Color.black)
                                {
                                    Gizmos.color = _navUnits[l].HighlightColor;
                                    Gizmos.DrawWireCube(relativeBounds.center, relativeBounds.size);
                                }
                            }

                            l++;
                        }
                    }
                }
            }
        }


        #region Editor Helper Functions

#if UNITY_EDITOR
        [UsedImplicitly]
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

        [UsedImplicitly]
        void BakeNavGrid()
        {
            ResetNavUnits();

            int l = 0;
            for (int j = 0; j < navGridSizeY; j++)
            {
                for (int k = 0; k < navGridSizeZ; k++)
                {
                    for (int i = 0; i < navGridSizeX; i++)
                    {
                        _navUnits[l] = _navUnits[l].Update(this);
                        l++;
                    }
                }
            }

            SceneView.RepaintAll();

            NavGridBakeDataModel navGridBakeData = new NavGridBakeDataModel
            {
                NavUnitSize = _navUnitSize,
                NavGridSizeX = navGridSizeX,
                NavGridSizeY = navGridSizeY,
                NavGridSizeZ = navGridSizeZ,
                NavUnits = new NavUnitBakeDataModel[navGridSizeX * navGridSizeY * navGridSizeZ]
            };


            l = 0;
            for (int j = 0; j < navGridSizeY; j++)
            {
                for (int k = 0; k < navGridSizeZ; k++)
                {
                    for (int i = 0; i < navGridSizeX; i++)
                    {
                        navGridBakeData.NavUnits[l] = new NavUnitBakeDataModel
                        {
                            IsNavigable = _navUnits[l].IsNavigable() ? 1 : 0
                        };
                        l++;
                    }
                }
            }

            string bakeDataDir = SceneManager.GetActiveScene().path;
            bakeDataDir = bakeDataDir.Substring(0, bakeDataDir.Length - Path.GetFileName(bakeDataDir).Length) +
                          "NavGridData/";

            Directory.CreateDirectory(bakeDataDir);

            _bakeDataFileName = bakeDataDir + "navGridBakedData";

            File.WriteAllText(_bakeDataFileName, JsonUtility.ToJson(navGridBakeData));

            AssetDatabase.Refresh();
        }
#endif

        #endregion


        #region AStar

        [BurstCompile]
        struct FindPathJob : IJob
        {
            public int start, end;

            public int navGridSizeX, navGridSizeY, navGridSizeZ;

            public NativeArray<NavUnit> navUnits;
            public NativeArray<int3> neighborOffsets;

            public NativeList<int> path;

            public void Execute()
            {
                FindPathUsingAStar(start, end, path);
            }

            private void BackTracePath(int endNavUnit, NativeList<int> path)
            {
                path.Clear();

                int cur = endNavUnit;
                while (cur >= 0)
                {
                    path.Add(cur);
                    if (navUnits[cur].AStarData.ParentIndex != -1 &&
                        navUnits[cur].AStarData.ParentIndex != navUnits[cur].Index)
                    {
                        cur = navUnits[cur].AStarData.ParentIndex;
                    }
                    else
                    {
                        cur = -1;
                    }
                }

                for (int i = 0; i < path.Length / 2; i++)
                {
                    int temp = path[i];
                    path[i] = path[path.Length - 1 - i];
                    path[path.Length - 1 - i] = temp;
                }
            }

            private void FindPathUsingAStar(int startNavUnit, int endNavUnit, NativeList<int> path)
            {
                if (startNavUnit < 0 || endNavUnit < 0 ||
                    !navUnits[startNavUnit].IsNavigable() || !navUnits[endNavUnit].IsNavigable() ||
                    startNavUnit == endNavUnit)
                {
                    return;
                }

                Bounds endNavUnitBounds = navUnits[endNavUnit].GetRelativeBounds();

                // SimplePriorityQueue<int> openList = new SimplePriorityQueue<int>();
                // HashSet<int> closedList = new HashSet<int>();

                NativeMinHeap<int> openList = new NativeMinHeap<int>(Allocator.Temp);
                NativeArray<bool> closedList = new NativeArray<bool>(navUnits.Length, Allocator.Temp);

                int l = 0;
                for (int j = 0; j < navGridSizeY; j++)
                {
                    for (int k = 0; k < navGridSizeZ; k++)
                    {
                        for (int i = 0; i < navGridSizeX; i++)
                        {
                            navUnits[l] = navUnits[l].ResetPathFindingData();
                            l++;
                        }
                    }
                }
                
                openList.Push(startNavUnit, navUnits[startNavUnit].AStarData.F);

                while (openList.Size() > 0)
                {
                    int curNavUnit = openList.Pop();
                    int3 curNavPos = GetPosFromIndex(curNavUnit);

                    Bounds curNavUnitBounds = navUnits[curNavUnit].GetRelativeBounds();

                    for (int i = 0; i < neighborOffsets.Length; i++)
                    {
                        int3 neighborOffset = neighborOffsets[i];

                        int neighbor = GetIndexFromPos(curNavPos.x + neighborOffset.x, curNavPos.y + neighborOffset.y,
                            curNavPos.z + neighborOffset.z);

                        if (neighbor < 0 || !navUnits[neighbor].IsNavigable())
                        {
                            continue;
                        }

                        if (neighbor == endNavUnit)
                        {
                            navUnits[neighbor] = navUnits[neighbor].SetPathFindingParentIndex(curNavUnit);

                            BackTracePath(endNavUnit, path);
                            return;
                        }

                        if (!closedList[neighbor])
                        {
                            Bounds neighborBounds = navUnits[neighbor].GetRelativeBounds();

                            float newG = navUnits[curNavUnit].AStarData.G +
                                         Vector3.Distance(curNavUnitBounds.center, neighborBounds.center);
                            float newH = Vector3.Distance(neighborBounds.center, endNavUnitBounds.center);
                            float newF = newG + newH;

                            if (newF < navUnits[neighbor].AStarData.F)
                            {
                                navUnits[neighbor] =
                                    navUnits[neighbor].UpdatePathFindingValues(newF, newG, newH, curNavUnit);

                                openList.Push(neighbor, navUnits[neighbor].AStarData.F);
                            }
                        }
                    }

                    closedList[curNavUnit] = true;
                }

                openList.Dispose();
                closedList.Dispose();
            }

            private int FindCheapestIndex(NativeList<int> openList)
            {
                int lowestIndex = 0;
                for (int i = 1; i < openList.Length; i++)
                {
                    if (navUnits[openList[i]].AStarData.F < navUnits[openList[lowestIndex]].AStarData.F)
                    {
                        lowestIndex = i;
                    }
                }

                return lowestIndex;
            }

            private int GetIndexFromPos(int3 pos)
            {
                return GetIndexFromPos(pos.x, pos.y, pos.z);
            }

            private int GetIndexFromPos(int i, int j, int k)
            {
                if (i < 0 || j < 0 || k < 0)
                {
                    return -1;
                }

                if (i >= navGridSizeX || j >= navGridSizeY || k >= navGridSizeZ)
                {
                    return -1;
                }

                return (navGridSizeX * navGridSizeZ * j) + (navGridSizeX * k) + i;
            }

            private int3 GetPosFromIndex(int index)
            {
                int3 pos;
                pos.y = index / (navGridSizeX * navGridSizeZ);

                index = index % (navGridSizeX * navGridSizeZ);
                pos.z = index / navGridSizeX;

                index = index % navGridSizeX;
                pos.x = index;

                return pos;
            }
        }

        #endregion

        #endregion


        #region Interface

        public int GetNavUnitSize()
        {
            return _navUnitSize;
        }

        public int3 GetNavUnitPos(Vector3 worldPos)
        {
            Vector3 offset = transform.InverseTransformPoint(worldPos);
            offset.x /= _navUnitSize;
            offset.y /= _navUnitSize;
            offset.z /= _navUnitSize;

            int3 pos;
            pos.x = (int) offset.x;
            pos.y = (int) offset.y;
            pos.z = (int) offset.z;

            return pos;
        }

        public List<Vector3> GetShortestPath(Vector3 from, Vector3 to)
        {
            int startNavUnit = GetIndexFromPos(GetNavUnitPos(from));
            int endNavUnit = GetIndexFromPos(GetNavUnitPos(to));

            NativeList<int> aStarPath = new NativeList<int>(Allocator.TempJob);
            FindPathJob findPathJob = new FindPathJob()
            {
                navGridSizeX = navGridSizeX,
                navGridSizeY = navGridSizeY,
                navGridSizeZ = navGridSizeZ,
                navUnits = _navUnits,
                neighborOffsets = _neighborOffsets,
                start = startNavUnit,
                end = endNavUnit,
                path = aStarPath,
            };
            JobHandle jobHandle = findPathJob.Schedule();
            jobHandle.Complete();

            List<Vector3> path = new List<Vector3>();
            for (int i = 0; i < aStarPath.Length; i++)
            {
                path.Add(_navUnits[aStarPath[i]].GetRelativeBounds().center);
            }

            aStarPath.Dispose();

            return path;
        }

        public int GetIndexFromPos(int3 pos)
        {
            return GetIndexFromPos(pos.x, pos.y, pos.z);
        }

        public int GetIndexFromPos(int i, int j, int k)
        {
            if (i < 0 || j < 0 || k < 0)
            {
                return -1;
            }

            if (i >= navGridSizeX || j >= navGridSizeY || k >= navGridSizeZ)
            {
                return -1;
            }

            return (navGridSizeX * navGridSizeZ * j) + (navGridSizeX * k) + i;
        }

        public int3 GetPosFromIndex(int index)
        {
            int3 pos;
            pos.y = index / (navGridSizeX * navGridSizeZ);

            index = index % (navGridSizeX * navGridSizeZ);
            pos.z = index / navGridSizeX;

            index = index % navGridSizeX;
            pos.x = index;

            return pos;
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