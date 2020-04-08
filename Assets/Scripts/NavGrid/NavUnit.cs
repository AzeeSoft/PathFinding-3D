using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

#endif

namespace Azee.PathFinding3D
{
    public struct NavUnit
    {
        public struct AStarDataModel
        {
            public float F, G, H;
            public int ParentIndex;
        }

        #region Fields

        public readonly int Row, Col, Depth;

        public readonly int Index;
        // public readonly NavGrid Parent;

        public AStarDataModel AStarData;
        public Color HighlightColor;

        // public int Size => Parent.GetNavUnitSize();

        private Bounds _relativeBounds;
        private bool _navigable;

        // private List<int> _neighbors;

        #endregion


        #region Interface

        public NavUnit(int row, int col, int depth, NavGrid parent)
        {
            Row = row;
            Col = col;
            Depth = depth;
            // Parent = parent;

            Index = parent.GetIndexFromPos(Row, Col, Depth);

            AStarData = new AStarDataModel();

            HighlightColor = Color.black;

            _navigable = true;
            // _neighbors = new List<int>();

            _relativeBounds = new Bounds();
            ComputeRelativeBounds(parent);

            ResetPathFindingData();
        }

        public NavUnit Update(NavGrid parent)
        {
            ValidateRelativeBounds(parent);
            CheckForColliders(parent);

            return this;
        }

        public NavUnit UpdateFromBakedData(NavGrid parent, NavUnitBakeDataModel navUnitBakeData)
        {
            _navigable = navUnitBakeData.IsNavigable != 0;
            return this;
        }

        public Bounds GetRelativeBounds()
        {
            return _relativeBounds;
        }

        public bool IsNavigable()
        {
            return _navigable;
        }

        public NavUnit ResetPathFindingData()
        {
            AStarData.G = 0;
            AStarData.H = float.MaxValue;
            AStarData.F = float.MaxValue;
            AStarData.ParentIndex = -1;

            return this;
        }

        public NavUnit UpdatePathFindingValues(float f, float g, float h, int parentIndex)
        {
            AStarData.F = f;
            AStarData.G = g;
            AStarData.H = h;
            AStarData.ParentIndex = parentIndex;

            return this;
        }

        public NavUnit SetPathFindingParentIndex(int index)
        {
            AStarData.ParentIndex = index;

            return this;
        }

        #endregion


        #region Implementation

        private void CheckForColliders(NavGrid parent)
        {
            Vector3 transformedCenter = parent.transform.TransformPoint(_relativeBounds.center);
            Vector3 transformedExtents = parent.transform.TransformVector(_relativeBounds.extents);

            Collider[] hitColliders = Physics.OverlapBox(transformedCenter,
                transformedExtents, parent.transform.rotation);

            _navigable = true;
            foreach (Collider col in hitColliders)
            {
                if (col.gameObject.GetComponent<NavGridObstacle>() != null)
                {
                    _navigable = false;
                    break;
                }

#if UNITY_EDITOR
                if (GameObjectUtility.AreStaticEditorFlagsSet(col.gameObject, StaticEditorFlags.NavigationStatic))
                {
                    _navigable = false;
                    break;
                }
#endif
            }
        }

        private void ValidateRelativeBounds(NavGrid parent)
        {
            if (Math.Abs(_relativeBounds.size.magnitude - parent.GetNavUnitSize()) > 0.001) // If size has changed
            {
                ComputeRelativeBounds(parent);
            }
        }

        private void ComputeRelativeBounds(NavGrid parent)
        {
            float navUnitSize = parent.GetNavUnitSize();
            Vector3 unitLocalCenter = new Vector3(navUnitSize, navUnitSize, navUnitSize) / 2f;

            Vector3 unitCenter = Vector3.zero;
            unitCenter += Vector3.right * Row;
            unitCenter += Vector3.up * Col;
            unitCenter += Vector3.forward * Depth;
            unitCenter *= navUnitSize;
            unitCenter += unitLocalCenter;

            _relativeBounds = new Bounds
            {
                center = unitCenter,
                extents = unitLocalCenter
            };
        }

        #endregion
    }

    [Serializable]
    public class NavUnitBakeDataModel
    {
        public int IsNavigable;
    }
}