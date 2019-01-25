﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TrackEdit.Node;
using UnityEngine;

namespace TrackEdit
{
    public class TrackSegmentHandler 
    {   
        private readonly FieldInfo _biNormalField;

        private bool _isSupportsInvalid;
        private float _meshGenerationTime;
        private float _supportRegenerateTime;

        public bool Invalidate { get; set; }
        public TrackSegment4 TrackSegment { get; private set; }
        public TrackEditHandler Handler { get; private set; }

        private readonly TrackEdgeNode _edgeNode;

        public TrackSegmentHandler(TrackSegment4 segment,TrackEditHandler handler)
        {
            TrackSegment = segment;
            Handler = handler;

            var flags = BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic;
            _biNormalField = typeof(TrackSegment4).GetField("startBinormal", flags);
            _edgeNode = TrackEdgeNode.Build<TrackEdgeNode>();
            

        }

        public void NotifySegmentChange()
        {
            var nextSegment = GetNextSegment(true);
            var previousSegment = GetPreviousSegment(true);

            if (nextSegment != null) {
                _edgeNode.Forward = nextSegment;
                _edgeNode.Current = this;
                _edgeNode.OnNotifySegmentChange();
            }
        }
        



        public int GetIndexOfSegment()
        {
            return TrackSegment.track.trackSegments.IndexOf(TrackSegment);
        }

        public void CalculateStartBinormal()
        {
            var previousSegment = GetPreviousSegment(true);
            if (previousSegment != null)
            {
                var nextSegment = GetNextSegment(true);

                _biNormalField.SetValue(TrackSegment,
                    TrackSegment.transform.InverseTransformDirection(Vector3.Cross(
                        previousSegment.TrackSegment.getNormal(1f), previousSegment.TrackSegment.getTangentPoint(1f))));

                if (nextSegment != null)
                {
                    //try to match the curve 
                    for (var x = 0; x < 10; x++)
                    {
                        TrackSegment.deltaRotation -= MathHelper.AngleSigned(
                            Quaternion.AngleAxis(0, nextSegment.TrackSegment.getTangentPoint(0.0f)) *
                            nextSegment.TrackSegment.getNormalPoint(0.0f),
                            Quaternion.AngleAxis(TrackSegment.deltaRotation, TrackSegment.getTangentPoint(1.0f)) *
                            TrackSegment.getNormalPoint(1.0f), TrackSegment.getTangentPoint(1.0f));
                        TrackSegment.totalRotation =
                            previousSegment.TrackSegment.totalRotation +
                            TrackSegment.deltaRotation; // + TrackSegment.getAdditionalRotation ();
                        TrackSegment.calculateLengthAndNormals(previousSegment.TrackSegment);
                        if (previousSegment.TrackSegment.isConnectedTo(nextSegment.TrackSegment))
                            break;
                    }
                }
                else
                {
                    TrackSegment.totalRotation =
                        previousSegment.TrackSegment.totalRotation +
                        TrackSegment.deltaRotation; // + TrackSegment.getAdditionalRotation ();
                    TrackSegment.calculateLengthAndNormals(previousSegment.TrackSegment);
                }
            }
        }


        //calculate new rotation for segment and update the total and delta rotation
        public void CalculateWithNewTotalRotation(float newRotation)
        {
            var nextSegment = GetNextSegment(true);
            var previousSegment = GetPreviousSegment(true);

            var diff = newRotation - TrackSegment.totalRotation;
            if (previousSegment != null)
            {
                TrackSegment.deltaRotation += diff;
                TrackSegment.totalRotation += diff;
                TrackSegment.calculateLengthAndNormals(previousSegment.TrackSegment);
            }

            if (nextSegment != null) nextSegment.CalculateStartBinormal();
        }


        public TrackSegmentHandler GetNextSegment(bool hasToBeConnected)
        {
            if (TrackSegment.isConnectedToNextSegment || !hasToBeConnected)
            {
                var track = Handler.TrackRide.Track;
                return Handler.GetSegmentHandler(track.trackSegments[track.getNextSegmentIndex(track.trackSegments.IndexOf(TrackSegment))]);
            }

            return null;
        }

        public TrackSegmentHandler GetPreviousSegment(bool hasToBeConnected)
        {
            if (TrackSegment.isConnectedToPreviousSegment || !hasToBeConnected)
            {
                var track = Handler.TrackRide.Track;
                return Handler.GetSegmentHandler(track.trackSegments[track.getPreviousSegmentIndex(track.trackSegments.IndexOf(TrackSegment))]);
            }

            return null;
        }

        public bool IsConnected(TrackSegmentHandler segment)
        {
            TrackSegmentHandler next = GetNextSegment(true);
            TrackSegmentHandler previous = GetPreviousSegment(true);
            if (segment == previous)
                return previous.TrackSegment.isConnectedTo(TrackSegment);
            if (segment == next)
                return TrackSegment.isConnectedTo(next.TrackSegment);
            return false;
        }


        public bool ConnectWithForwardSegment(TrackSegmentHandler next)
        {
            TrackSegment.isConnectedToNextSegment = true;
            next.TrackSegment.isConnectedToPreviousSegment = true;


            var nextFirstCurve = next.TrackSegment.curves.First();            
            var currentLastCurve = TrackSegment.curves.Last();

            float magnitude = Mathf.Abs((
                next.TrackSegment.transform.TransformPoint(nextFirstCurve.p0) - 
                next.TrackSegment.transform.TransformPoint(nextFirstCurve.p1)).magnitude);

            TrackSegment.curves.Last().p2 = TrackSegment.transform.InverseTransformPoint(
                TrackSegment.transform.TransformPoint(currentLastCurve.p3) + next.TrackSegment.getTangentPoint(0f) * -1f * magnitude);
            
           
            next.TrackSegment.calculateLengthAndNormals(TrackSegment);
            next.CalculateStartBinormal();
            Invalidate = true;
            return true;
        }


     

        private void ResetMeshForTrackSegment(TrackSegment4 segment)
        {
            var generatedMesh = typeof(TrackSegment4).GetField("generatedMeshes",
                BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic);
            if (generatedMesh != null)
            {
                var meshes = (List<Mesh>) generatedMesh.GetValue(segment);
                foreach (var m in meshes) Object.DestroyImmediate(m);
                meshes.Clear();
            }

            foreach (Transform child in segment.gameObject.transform)
            {
                var meshFilter = child.gameObject.GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    GameObject.DestroyImmediate(meshFilter.mesh);
                    GameObject.DestroyImmediate(meshFilter.sharedMesh);
                }

                Object.DestroyImmediate(child.gameObject);
            }
            MouseCollisions.Instance.removeColliders(segment,segment.gameObject);
            //UnityEngine.Object.DestroyImmediate( segment.gameObject.GetComponent<MouseCollider> ());
            Object.DestroyImmediate(segment.gameObject.GetComponent<MeshCollider>());
            Object.DestroyImmediate(segment.gameObject.GetComponent<BoundingMesh>());
        }


        public void OnDestroy()
        {
            if(_edgeNode != null)
                Object.Destroy(_edgeNode);
        }

        public void Update()
        {
            if (Invalidate)
                _supportRegenerateTime = Time.time;

            if (Invalidate && Time.time - _meshGenerationTime > .05f)
            {
                _isSupportsInvalid = true;

                ResetMeshForTrackSegment(TrackSegment);
                
                TrackSegment.generateMesh(Handler.TrackRide.meshGenerator);

                _meshGenerationTime = Time.time;
                Invalidate = false;
            }

            if (_isSupportsInvalid && Time.time - _supportRegenerateTime > .1f)
            {
                for (var x = 0; x < 5; x++)
                {
                    ResetMeshForTrackSegment(TrackSegment);
                    var localCrossTile = typeof(TrackSegment4).GetField("localCrossedTiles",
                        BindingFlags.GetField | BindingFlags.Instance | BindingFlags.NonPublic);
                    if (localCrossTile != null) localCrossTile.SetValue(TrackSegment, null);

                    TrackSegment.generateMesh(Handler.TrackRide.meshGenerator);
                }

                _isSupportsInvalid = false;
            }

        }
    }
}