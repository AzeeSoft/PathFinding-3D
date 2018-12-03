using System.Collections;
using System.Collections.Generic;
using BasicTools.ButtonInspector;
using UnityEngine;

namespace Azee.PathFinding3D
{
    public class NavGridAgent : MonoBehaviour
    {
        public Transform Target;

        [Button("Find New Path", "FindNewPath")]
        public bool BtnFindNewPath;

        private List<Vector3> _lastFoundPath;

        #region Unity API

        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
        }

        private void OnDrawGizmos()
        {
            if (_lastFoundPath != null && _lastFoundPath.Count > 1)
            {
                Gizmos.color = Color.green;
                for (int i = 1; i < _lastFoundPath.Count; i++)
                {
                    Gizmos.DrawLine(_lastFoundPath[i - 1], _lastFoundPath[i]);
                }
            }
        }

        #endregion

        #region Implementation

        void FindNewPath()
        {
            _lastFoundPath = FindPathToTarget(Target);
        }

        #endregion

        #region Interface

        List<Vector3> FindPathToTarget(Transform target)
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