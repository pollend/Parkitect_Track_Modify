﻿using System;
using UnityEngine;

namespace RollercoasterEdit
{
    public class FreeDragState : IState
    {
		private BuilderHeightMarker _heightMaker;
        private bool _verticalDragState;
        private SharedStateData _stateData;
        public FreeDragState (SharedStateData stateData)
        {
			
			this._stateData = stateData;
			_heightMaker = UnityEngine.Object.Instantiate<BuilderHeightMarker>(ScriptableSingleton<AssetManager>.Instance.builderHeightMarkerGO);
			_heightMaker.attachedTo = stateData.Selected.transform;
			_heightMaker.heightChangeDelta = 1f;

        }

        public void Update(FiniteStateMachine stateMachine)
        {
            var ray = Camera.main.ScreenPointToRay (Input.mousePosition);
            Vector3 point = ray.GetPoint (_stateData.Distance);
            Vector3 position = Vector3.zero;
            if (!_verticalDragState) {
                position = new Vector3 (point.x, _stateData.FixedY, point.z) + new Vector3(_stateData.Offset.x, _stateData.Offset.y, _stateData.Offset.z);

            } else
            {
                _stateData.FixedY = point.y;
                position = new Vector3 (_stateData.Selected.position.x, _stateData.FixedY, _stateData.Selected.position.z) + new Vector3(0, _stateData.Offset.y, 0);
            }

            TrackNode trackNode = _stateData.Selected.gameObject.GetComponent<TrackNode> ();

            if (Input.GetKeyDown (Main.Configeration.VerticalKey)) {
                _stateData.Offset = new Vector3(_stateData.Offset.x,_stateData.Selected.transform.position.y - point.y, _stateData.Offset.z);
                _verticalDragState = true;
 
            } else if (Input.GetKeyUp (Main.Configeration.VerticalKey)) {
                _verticalDragState = false;
                _stateData.Offset = (_stateData.Selected.transform.position - point);

            }

            var nextSegment = trackNode.TrackSegmentModify.GetNextSegment ();
            var previousSegment = trackNode.TrackSegmentModify.GetPreviousSegment ();

            switch (trackNode.NodePoint) {
            case TrackNode.NodeType.PO:
                break;
            case TrackNode.NodeType.P1:
                trackNode.SetPoint (position);

                if (previousSegment != null) {

                    trackNode.CalculateLenghtAndNormals ();


                    float magnitude = Mathf.Abs((previousSegment.GetLastCurve.P2.GetGlobal () - previousSegment.GetLastCurve.P3.GetGlobal ()).magnitude);
                    previousSegment.GetLastCurve.P2.SetPoint (previousSegment.GetLastCurve.P3.GetGlobal() + (trackNode.TrackSegmentModify.TrackSegment.getTangentPoint(0f) *-1f* magnitude));
                    previousSegment.Invalidate = true;
                    trackNode.CalculateLenghtAndNormals ();

                    trackNode.TrackSegmentModify.CalculateStartBinormal ();
                    //TrackSegmentModify.TrackSegment.upgradeSavegameRecalculateBinormal (previousSegment.TrackSegment);


                }


                break;
            case TrackNode.NodeType.P2:

                trackNode.SetPoint (position);

                if (nextSegment != null) {

                    trackNode.CalculateLenghtAndNormals ();


                    float magnitude = Mathf.Abs((nextSegment.GetFirstCurve.P0.GetGlobal () - nextSegment.GetFirstCurve.P1.GetGlobal ()).magnitude);
                    nextSegment.GetFirstCurve.P1.SetPoint (nextSegment.GetFirstCurve.P0.GetGlobal () + (trackNode.TrackSegmentModify.TrackSegment.getTangentPoint(1f) * magnitude));
                    nextSegment.Invalidate = true;
                    trackNode.CalculateLenghtAndNormals ();

                    nextSegment.CalculateStartBinormal ();

                }


                break;
            case TrackNode.NodeType.P3:

                if (trackNode.TrackCurve.Group == TrackNodeCurve.Grouping.End || trackNode.TrackCurve.Group == TrackNodeCurve.Grouping.Both ) {

                    if (nextSegment != null) {

                        var NextP1Offset = nextSegment.GetFirstCurve.P1.GetGlobal() -trackNode.GetGlobal();
                        var P2Offset = trackNode.TrackCurve.P2.GetGlobal () - trackNode.GetGlobal ();


                        nextSegment.GetFirstCurve.P0.SetPoint (position);
                        nextSegment.GetFirstCurve.P1.SetPoint (position+ NextP1Offset);
                        trackNode.TrackCurve.P2.SetPoint(position+ P2Offset);

                        nextSegment.Invalidate = true;
                    }

                }
                trackNode.SetPoint (position);

                break;
            }

          //  _stateData.Selected.position = position;

           trackNode.Validate ();
			trackNode.TrackSegmentModify.Invalidate = true;

            
			nextSegment = trackNode.TrackSegmentModify.GetNextSegment (false);
			if (!_stateData.Selected.gameObject.GetComponent<TrackNode> ().TrackSegmentModify.TrackSegment.isConnectedToNextSegment) {
			
			
				if (_stateData.Selected.gameObject.GetComponent<TrackNode> ().NodePoint == TrackNode.NodeType.P3 && (position - nextSegment.GetFirstCurve.P0.GetGlobal ()).sqrMagnitude < .2f) {
					
					float magnitude = Mathf.Abs ((nextSegment.GetFirstCurve.P0.GetGlobal () - nextSegment.GetFirstCurve.P1.GetGlobal ()).magnitude);


					_stateData.Selected.gameObject.GetComponent<TrackNode> ().SetPoint (nextSegment.GetFirstCurve.P0.GetGlobal ());
					_stateData.Selected.gameObject.GetComponent<TrackNode> ().TrackSegmentModify.GetLastCurve.P2.SetPoint (_stateData.Selected.gameObject.GetComponent<TrackNode> ().TrackSegmentModify.GetLastCurve.P3.GetGlobal () + (nextSegment.TrackSegment.getTangentPoint (0f) * -1f * magnitude));

					_stateData.Selected.gameObject.GetComponent<TrackNode> ().CalculateLenghtAndNormals ();

					_stateData.Selected.gameObject.GetComponent<TrackNode> ().TrackSegmentModify.GetFirstCurve.P0.TrackSegmentModify.CalculateStartBinormal (false);



					if (nextSegment.GetFirstCurve.P0.Validate ()) {
						if (Input.GetMouseButtonUp (0)) {

							if (nextSegment.TrackSegment is Station) {
								_stateData.Selected.gameObject.SetActive (false);
							}

							this._stateData.SegmentManager.ConnectEndPieces (_stateData.Selected.gameObject.GetComponent<TrackNode> ().TrackSegmentModify,nextSegment);
							stateMachine.ChangeState (new IdleState (_stateData.SegmentManager));
						}
						nextSegment.Invalidate = true;

						_stateData.Selected.gameObject.GetComponent<TrackNode> ().TrackSegmentModify.Invalidate = true;
					}


				}
			}



	



            if (Input.GetMouseButtonUp (0)) {
				stateMachine.ChangeState(new IdleState (_stateData.SegmentManager));
            }
        }

		public void Unload()
		{
			UnityEngine.Object.Destroy (_heightMaker.gameObject);
		}
    }
}

