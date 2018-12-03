using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

namespace Azee.PathFinding3D
{
    public class NavUnit : IComparable<NavUnit>
    {
        public struct AStarDataModel
        {
            public float F, G, H;
            public NavUnit Parent;
        }

        #region Fields

        public readonly int Row, Col, Depth;
        public readonly NavGrid Parent;

        public AStarDataModel AStarData = new AStarDataModel();
        public Color HighlightColor = Color.black;

        public int Size => Parent.GetNavUnitSize();

        private Bounds _relativeBounds;
        private bool _navigable = true;

        #endregion


        #region Interface

        public NavUnit(int row, int col, int depth, NavGrid parent)
        {
            Row = row;
            Col = col;
            Depth = depth;
            Parent = parent;

            ComputeRelativeBounds();
        }

        public void Update()
        {
            ValidateRelativeBounds();
            CheckForColliders();
        }

        public Bounds GetRelativeBounds()
        {
            return _relativeBounds;
        }

        public bool IsNavigable()
        {
            return _navigable;
        }

        public void SetNavigable(bool isNavigable)
        {
            _navigable = isNavigable;
        }

        public List<NavUnit> GetNeighbors()
        {
            List<NavUnit> neighbors = new List<NavUnit>();
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    for (int k = -1; k <= 1; k++)
                    {
                        if (i != 1 || j != 1 || k != 1)
                        {
                            NavUnit neighbor = Parent.GetNavUnit(Row + i, Col + j, Depth + k);
                            if (neighbor.IsNavigable())
                            {
                                neighbors.Add(neighbor);
                            }
                        }
                    }
                }
            }

            return neighbors;
        }

        #endregion


        #region Implementation

        private void CheckForColliders()
        {
            Vector3 transformedCenter = Parent.transform.TransformPoint(_relativeBounds.center);
            Vector3 transformedExtents = Parent.transform.TransformVector(_relativeBounds.extents);

            Collider[] hitColliders = Physics.OverlapBox(transformedCenter,
                transformedExtents, Parent.transform.rotation);

            _navigable = true;
            foreach (Collider col in hitColliders)
            {
                if (col.gameObject.GetComponent<NavGridObstacle>() != null)
                {
                    _navigable = false;
                    break;
                }
            }
        }

        private void ValidateRelativeBounds()
        {
            if (Math.Abs(_relativeBounds.size.magnitude - Size) > 0.001) // If size has changed
            {
                ComputeRelativeBounds();
            }
        }

        private void ComputeRelativeBounds()
        {
            Vector3 unitLocalCenter = new Vector3(Size, Size, Size) / 2f;

            Vector3 unitCenter = Vector3.zero;
            unitCenter += Vector3.right * Row;
            unitCenter += Vector3.up * Col;
            unitCenter += Vector3.forward * Depth;
            unitCenter *= Size;
            unitCenter += unitLocalCenter;

            _relativeBounds = new Bounds
            {
                center = unitCenter,
                extents = unitLocalCenter
            };
        }

        public int CompareTo(NavUnit other)
        {
            return AStarData.F.CompareTo(other.AStarData.F);
        }

        #endregion
    }

    [Serializable]
    public class NavUnitBakeDataModel
    {
        public int IsNavigable;
    }
}