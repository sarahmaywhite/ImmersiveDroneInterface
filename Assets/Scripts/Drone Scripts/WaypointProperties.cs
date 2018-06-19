﻿namespace ISAACS
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using VRTK;

    public class WaypointProperties : MonoBehaviour
    {
        public Waypoint classPointer;
        public Drone referenceDrone;
        public GameObject referenceDroneGameObject;
        private GameObject prevPoint;

        public Material unpassedWaypoint;
        public Material passedWaypoint;
        public Material selectedGroundpoint;
        public Material selectedUnpassedLine;
        public Material unselectedUnpassedLine;
        public Material selectedPassedLine;
        public Material unselectedPassedLine;
        public Material selectedGroundpointLine;
        public Material unselectedGroundpointLine;

        public bool passed; // Indicates whether this waypoint has been passed by the drone

        public GameObject modelGroundpoint; // Refers to the groundpoint object being instantiated
        private GameObject thisGroundpoint; // groundpoint instantiated under current waypoint
        private LineRenderer groundpointLine; // Connects the groundpoint to the waypoint

        private LineRenderer LineProperties;
        private CapsuleCollider lineCollider;

        private GameObject world;
        private GameObject controller;

        public bool setLineOriginWaypointToggle;
        public static GameObject controller_right;

        void Start()
        {
            passed = false;

            referenceDrone = classPointer.referenceDrone;
            referenceDroneGameObject = referenceDrone.gameObjectPointer;

            world = GameObject.FindGameObjectWithTag("World");
            controller = GameObject.FindGameObjectWithTag("GameController");
            controller_right = GameObject.Find("controller_right");

            LineProperties = this.GetComponentInParent<LineRenderer>();
 
            lineCollider = new GameObject("Collider").AddComponent<CapsuleCollider>();
            lineCollider.tag = "Line Collider";
            lineCollider.isTrigger = true;
            lineCollider.radius = 0.1f;
            lineCollider.gameObject.AddComponent<LineProperties>().originWaypoint = classPointer;

            setLineOriginWaypointToggle = true;

            // Sets up interaction events
            GetComponent<VRTK_InteractableObject>().InteractableObjectUngrabbed += new InteractableObjectEventHandler(InteractableObjectUngrabbed);
        }

        void Update()
        {
            // Establishing the previous point in the path. (could be the drone)
            if (classPointer.prevPathPoint != null)
            {
                prevPoint = classPointer.prevPathPoint.gameObjectPointer;
            } else
            {
                prevPoint = referenceDrone.gameObjectPointer;
            }
            
            
            if (prevPoint != null)
            {
                ResetWaypoint();

                SetPassedState();

                SetLine();
                
                if (thisGroundpoint == null)
                {
                    CreateGroundpoint();
                }             

                CreateWaypointIndicator();

                ChangeColor();
            }

            UpdateLine();
        }

        // Positions line between waypoints and drones
        public void SetLine()
        {
            if (prevPoint != null)
            {
                LineProperties.SetPosition(0, this.transform.position);

                Vector3 endpoint;

                if (referenceDroneGameObject.GetComponent<MoveDrone>().targetWaypoint != this.gameObject || passed)
                {
                    endpoint = prevPoint.transform.position;
                    LineProperties.SetPosition(1, endpoint);
                } else
                {
                    endpoint = referenceDroneGameObject.transform.position;
                    LineProperties.SetPosition(1, endpoint);
                }
                SetLineCollider(endpoint);

                // If line being selected by controller
                if (controller.GetComponent<VRTK_StraightPointerRenderer>().lineSelected == this.gameObject && referenceDrone.selected)
                {
                    LineProperties.startWidth = world.GetComponent<MapInteractions>().actualScale.y / 100;
                    LineProperties.endWidth = world.GetComponent<MapInteractions>().actualScale.y / 100;

                    if (OVRInput.Get(OVRInput.Button.SecondaryIndexTrigger))
                    {
                        //if (setLineOriginWaypointToggle)
                        //{
                        //    SetLineOriginWaypoint();
                        //}
                    } else
                    {
                        setLineOriginWaypointToggle = true;
                    }
                }
                else
                {
                    LineProperties.startWidth = world.GetComponent<MapInteractions>().actualScale.y / 200;
                    LineProperties.endWidth = world.GetComponent<MapInteractions>().actualScale.y / 200;
                }
            }
        }
        
        // Places a collider around the waypoint line
        public void SetLineCollider(Vector3 endpoint)
        {
            if (passed)
            {
                Destroy(lineCollider);
            } else if (referenceDrone.selected)
            {
                lineCollider.transform.parent = LineProperties.transform;
                lineCollider.radius = world.GetComponent<MapInteractions>().actualScale.y / 50;
                lineCollider.center = Vector3.zero;
                lineCollider.transform.position = (endpoint + this.gameObject.transform.position) / 2;
                lineCollider.direction = 2;
                lineCollider.transform.LookAt(this.gameObject.transform, Vector3.up);
                lineCollider.height = (endpoint - this.transform.position).magnitude;
                lineCollider.transform.parent = world.transform;
            }
        }

        // Creates the groundpoint under waypoint
        public void CreateGroundpoint()
        {
            if (groundpointLine != null)
                Destroy(groundpointLine);

            Vector3 groundpoint = new Vector3(this.transform.position.x, world.transform.position.y + modelGroundpoint.transform.localScale.y, this.transform.position.z);
            thisGroundpoint = Instantiate(modelGroundpoint, groundpoint, Quaternion.identity);
            thisGroundpoint.transform.localScale = world.GetComponent<MapInteractions>().actualScale / 100;
            thisGroundpoint.transform.parent = world.transform;
            groundpointLine = thisGroundpoint.GetComponent<LineRenderer>();
        }

        // Creates a new Waypoint Indicator
        public void CreateWaypointIndicator()
        {
            groundpointLine.SetPosition(0, thisGroundpoint.transform.position);
            groundpointLine.SetPosition(1, this.transform.position);
            groundpointLine.startWidth = world.GetComponent<MapInteractions>().actualScale.y / 400;
            groundpointLine.endWidth = world.GetComponent<MapInteractions>().actualScale.y / 400;
            if (referenceDrone.selected)
            {
                groundpointLine.material = selectedGroundpointLine;
            } else
            {
                groundpointLine.material = unselectedGroundpointLine;
            }
        }

        // Changes the colors of waypoints and lines based on their selected and passed states
        public void ChangeColor()
        {
            if (passed)
            {
                this.GetComponent<MeshRenderer>().material = passedWaypoint;
                if (referenceDrone.selected)
                {
                    LineProperties.material = selectedPassedLine;
                }
                else
                {
                    LineProperties.material = unselectedPassedLine;
                }
            } else if ((controller.GetComponent<VRTK_StraightPointerRenderer>().lineSelected == this.gameObject || 
                (controller_right.GetComponent<ControllerInteractions>().mostRecentCollision.waypoint != null && 
                controller_right.GetComponent<ControllerInteractions>().mostRecentCollision.waypoint.gameObjectPointer == this.gameObject)) && 
                referenceDrone.selected)
            {
                LineProperties.material = unpassedWaypoint;
            } else
            {
                this.GetComponent<MeshRenderer>().material = unpassedWaypoint;
                if (referenceDrone.selected)
                {
                    LineProperties.material = selectedUnpassedLine;
                }
                else
                {
                    LineProperties.material = unselectedUnpassedLine;
                }
            }
        }

        // Destroys groundpoint when waypoint is destroyed
        public void OnDestroy()
        {
            Destroy(thisGroundpoint);
        }

        // Sets this waypoint's passed state
        public void SetPassedState()
        {
            if (!passed && referenceDroneGameObject.transform.position == this.transform.position)
            {
                passed = true;
            }
        }

        public void ResetWaypoint()
        {
            if (OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch) == this.transform.position)
            {
                Debug.Log("move");
            }
            //Debug.Log(OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch));
            //Debug.Log(this.transform.position);
        }

        void InteractableObjectUngrabbed(object sender, VRTK.InteractableObjectEventArgs e)
        {
            CreateGroundpoint();
        }

        public void deleteLineCollider()
        {
            Destroy(this.lineCollider.gameObject);
        }

        //Update groundpoint line 
        public void UpdateLine()
        { 
            if (thisGroundpoint == null) {
                return;
            }

            Vector3 groundPointLocation = new Vector3(this.transform.position.x, world.transform.position.y + modelGroundpoint.transform.localScale.y, this.transform.position.z);
            thisGroundpoint.transform.position = groundPointLocation;
            groundpointLine = thisGroundpoint.GetComponent<LineRenderer>(); 
        }
    }
}
