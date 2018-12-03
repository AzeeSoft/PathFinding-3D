using System.Collections;
using System.Collections.Generic;
using BasicTools.ButtonInspector;
using UnityEngine;

namespace Azee.PathFinding3D
{
    public class NavGridAgent : MonoBehaviour
    {
        #region Interface

        public List<Vector3> FindPathToTarget(Transform target)
        {
            List<Vector3> path = new List<Vector3>();

            if (Application.isPlaying)
            {
                List<NavUnit> navUnitPath = NavGrid.Instance.GetShortestPath(transform.position, target.position);
                foreach (NavUnit navUnit in navUnitPath)
                {
                    path.Add(navUnit.Parent.transform.TransformPoint(navUnit.GetRelativeBounds().center));
                }
            }

            return path;
        }

        #endregion
    }
}