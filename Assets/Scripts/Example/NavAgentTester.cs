using System.Collections.Generic;
using BasicTools.ButtonInspector;
using UnityEngine;

namespace Azee.PathFinding3D.Example
{
    public class NavAgentTester : MonoBehaviour
    {
        public NavGridAgent Agent;
        public Transform Target;

        public bool FindPathContinuously;

        [Button("Find New Path", "FindNewPath")]
        public bool BtnFindNewPath;

        private List<Vector3> _lastFoundPath;
        private Vector3 lastAgentLocation;
        private Vector3 lastTargetLocation;

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
                if (Vector3.Distance(lastTargetLocation, Target.position) > 1f || Vector3.Distance(lastAgentLocation, Agent.transform.position) > 1f)
                {
                    FindNewPath();
                    lastTargetLocation = Target.position;
                    lastAgentLocation = Agent.transform.position;
                }
            }
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
            if (Agent)
            {
                _lastFoundPath = Agent.FindPathToTarget(Target);
            }
        }

        #endregion

        
    }
}