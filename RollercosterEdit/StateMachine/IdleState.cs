﻿using System;
using UnityEngine;

namespace RollercoasterEdit
{
    public class IdleState : IState
    {
        private SharedStateData _stateData;
		public IdleState (SharedStateData stateData)
        {
            _stateData = new SharedStateData ();
			_stateData.SetActiveNode(stateData.ActiveNode);
			_stateData.SegmentManager = stateData.SegmentManager;
		}

        public void Update(FiniteStateMachine stateMachine)
        {


            var ray = Camera.main.ScreenPointToRay (Input.mousePosition);

			if (Input.GetMouseButtonDown (1)) {
				RaycastHit hit;
				if (Physics.Raycast (ray, out hit, Mathf.Infinity, LayerMasks.COASTER_TRACKS)) {
					if (hit.transform.name == "BezierNode") {
						_stateData.SetActiveNode(hit.transform);
					}
				}
			}

            if (Input.GetMouseButtonDown (0)) {
                RaycastHit hit;

				if (Physics.Raycast (ray, out hit, Mathf.Infinity,LayerMasks.COASTER_TRACKS)) {
					_stateData.Selected = hit.transform;
					_stateData.FixedY = hit.transform.position.y;
					_stateData.Offset = new Vector3(hit.transform.position.x - hit.point.x, 0, hit.transform.position.z - hit.point.z);

					_stateData.Distance = (ray.origin - hit.point).magnitude;

					if (hit.transform.name == "BezierNode") {
						_stateData.SetActiveNode(hit.transform);

						TrackNode node = hit.transform.GetComponent<TrackNode> ();

						var nextSegment = node.TrackSegmentModify.GetNextSegment ();
						var previousSegment = node.TrackSegmentModify.GetPreviousSegment ();

						if (node.NodePoint == TrackNode.NodeType.P1 && previousSegment != null && previousSegment.TrackSegment is Station) {
							stateMachine.ChangeState (new LinearDragState (_stateData));
						} else if (node.NodePoint == TrackNode.NodeType.P2 && nextSegment != null && nextSegment.TrackSegment is Station) {
							stateMachine.ChangeState (new LinearDragState (_stateData));
						} else {

							stateMachine.ChangeState (new FreeDragState (_stateData));
						}
                    }
					if (hit.transform.name == "ExtureNode") {
						stateMachine.ChangeState (new ConsumeExtrudeNodeState (_stateData));
					}
                }
              
            }
        }

		public void Unload()
		{
		}
    
    }
}

