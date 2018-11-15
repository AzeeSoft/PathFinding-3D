using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

namespace Azee.PathFinding3D
{
    public class NavUnit
    {
        #region Fields

        public readonly int Row, Col, Depth;
        public readonly NavGrid Parent;

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

        #endregion


        #region Implementation

        private void CheckForColliders()
        {
            Vector3 transformedCenter = Parent.transform.TransformPoint(_relativeBounds.center);
            Vector3 transformedExtents = Parent.transform.TransformVector(_relativeBounds.extents);

            Collider[] hitColliders = Physics.OverlapBox(transformedCenter,
                transformedExtents, Parent.transform.rotation);

            _navigable = hitColliders.Length <= 0;
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

        #endregion
    }
}