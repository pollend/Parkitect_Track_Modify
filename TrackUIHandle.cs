﻿using System;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

namespace HelloMod
{
	public class TrackUIHandle : MonoBehaviour
	{
		private FieldInfo _trackerRiderField;

		private Dictionary<TrackSegment4,TrackSegmentModify> _segments = new Dictionary<TrackSegment4,TrackSegmentModify> ();

		private Transform _selected;
		private Vector3 _offset;
		private float _dist;

		public TrackBuilder TrackBuilder{ get; private set; }
		public TrackedRide TrackRide{ get; private set; }



		public TrackSegmentModify GetSegment(TrackSegment4 segment)
		{
			return _segments [segment];
		}

		public TrackUIHandle ()
		{
			
		}

		void Awake()
		{
	
			TrackBuilder = this.gameObject.GetComponent<TrackBuilder>();
			BindingFlags flags = BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic;
			_trackerRiderField = TrackBuilder.GetType ().GetField ("trackedRide", flags);
		}
		void Start() {
			

			TrackRide = ((TrackedRide)_trackerRiderField.GetValue (TrackBuilder));
			TrackBuilder = this.gameObject.GetComponent<TrackBuilder>();
		
			TrackRide.Track.OnAddTrackSegment += (trackSegment) => {
				_segments.Add (trackSegment, new TrackSegmentModify (trackSegment, this));
			};
			TrackRide.Track.OnRemoveTrackSegment += (trackSegment) => {
				_segments [trackSegment].Destroy ();
				_segments.Remove (trackSegment);
			};	
		

		}

		void OnDestroy() {
			foreach (var segment in _segments.Values) {
				segment.Destroy ();
			}
		}
		void Update()
		{
			if (_segments.Count == 0) {

				TrackRide = ((TrackedRide)_trackerRiderField.GetValue (TrackBuilder));

				for (int x = 0; x < TrackRide.Track.trackSegments.Count; x++) {
					_segments.Add (TrackRide.Track.trackSegments [x], new TrackSegmentModify (TrackRide.Track.trackSegments [x], this));

				}
			}

			foreach (var segment in _segments.Values) {
				segment.Update ();
			}
	
			var ray = Camera.main.ScreenPointToRay (Input.mousePosition);

			if (Input.GetMouseButtonDown (0)) {
				if (!_selected) {
					RaycastHit hit;
					if (Physics.Raycast (ray, out hit, Mathf.Infinity,-1)) {
						if (hit.transform.name == "BezierNode") {
							_selected = hit.transform;
							_offset = hit.transform.position - hit.point;
							_dist = (ray.origin - hit.point).magnitude;

						}
					}
				}
			} else if (Input.GetMouseButtonUp (0)) 
			{
				_selected = null;
			}

			if (_selected) {
				_selected.gameObject.GetComponent<TrackCurveNode> ().NodeUpdate ();

				_selected.position = ray.GetPoint (_dist) + _offset;
			}
				

		}
	
	}
}

