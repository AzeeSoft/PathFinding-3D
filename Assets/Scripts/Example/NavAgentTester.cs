using System.Collections.Generic;
using BasicTools.ButtonInspector;
using UnityEngine;

namespace Azee.PathFinding3D.Example
{
    public class NavAgentTester : MonoBehaviour
    {
        #region Inspector Fields

        public NavGridAgent Agent;
        public LineRenderer LineRenderer;
        public Transform Target;

        public float PathSegments = 1f;

        public bool FindPathContinuously;

        [Button("Find New Path", "FindNewPath")]
        public bool BtnFindNewPath;

        #endregion

        #region Non Inspector Fields

        private List<Vector3> _lastFoundPath;
        private Vector3 _lastAgentLocation;
        private Vector3 _lastTargetLocation;

        #endregion

        
        #region Unity API

        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
            if (FindPathContinuously)
            {
                if (Vector3.Distance(_lastTargetLocation, Target.position) > 1f || Vector3.Distance(_lastAgentLocation, Agent.transform.position) > 1f)
                {
                    FindNewPath();
                    _lastTargetLocation = Target.position;
                    _lastAgentLocation = Agent.transform.position;
                }
            }
        }

        private void OnDrawGizmos()
        {
            /*if (_lastFoundPath != null && _lastFoundPath.Count > 1)
            {
                Vector3[] smoothPath = LineSmoother.SmoothLine(_lastFoundPath.ToArray(), PathSegments);
                
                Gizmos.color = Color.green;
                for (int i = 1; i < smoothPath.Length; i++)
                {
                    Gizmos.DrawLine(smoothPath[i - 1], smoothPath[i]);
                }
            }*/
        }

        #endregion

        
        #region Implementation

        void FindNewPath()
        {
            if (Agent)
            {
                float startTime = Time.realtimeSinceStartup;
                _lastFoundPath = Agent.FindPathToTarget(Target);
                print("Time: " + (Time.realtimeSinceStartup - startTime) * 1000f);

                UpdateLineRenderer(_lastFoundPath);
            }
        }

        void UpdateLineRenderer(List<Vector3> positions)
        {
            LineRenderer.positionCount = positions.Count;
            LineRenderer.SetPositions(positions.ToArray());
        }

        #endregion
    }
}