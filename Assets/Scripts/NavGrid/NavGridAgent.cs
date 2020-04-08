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
                List<Vector3> navUnitPath = NavGrid.Instance.GetShortestPath(transform.position, target.position);
                foreach (Vector3 pos in navUnitPath)
                {
                    path.Add(NavGrid.Instance.transform.TransformPoint(pos));
                }
            }

            return path;
        }

        #endregion
    }
}