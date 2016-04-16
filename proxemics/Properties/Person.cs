using System;
using System.Collections.Generic;
using Microsoft.Kinect;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;

namespace proxemics
{
    /// <summary>
    /// Person Class
    /// Authors: Adam M Pere, Keebaik Sim, Enol Vallina
    /// Introduction to Computational Design - Fall 2015, Harvard Graduate School of Design
    /// 
    /// The person class represents a person that is physically standing in front of a kinect sensor
    /// </summary>
    public class Person
    {
        DateTime timeCreated;
        protected ulong id;
        protected Body myBody; // reference to the body object that created this person
        KinectHelper kinect;

        // constructor
        public Person(Body skeleton, KinectHelper k)
        {
            // initialize instance variables & other setup
            timeCreated = DateTime.Now;
            myBody = skeleton;
            kinect = k;
            id = myBody.TrackingId;
            Console.WriteLine("new person " + id);
        }

        /// <summary>
        /// getID
        /// </summary>
        /// <returns> The ID of this string</returns>
        public ulong getID()
        {
            return id;
        }

        /// <summary>
        /// getTimeCreated
        /// </summary>
        /// <returns>A DateTime object representing the time this memory was first instantiated</returns>
        public DateTime getTimeCreated()
        {
            return timeCreated;
        }

        /// <summary>
        /// getTimeSinceCreated
        /// </summary>
        /// <returns>A TimeSpan object representing the amount of time that has passed since this object was created</returns>
        public TimeSpan getTimeSinceCreated()
        {
            return DateTime.Now.Subtract(timeCreated);
        }



        /// <summary>
        /// Function getJoints
        /// </summary>
        /// <returns>a list of this person's joints & their location in 3-space/returns>
        public List<Vector3d> getJoints()
        {
            CameraSpacePoint joint;
            IEnumerable<JointType> keys = myBody.Joints.Keys;
            List<Vector3d> jointPoints = new List<Vector3d>();
            foreach (JointType key in keys)
            {
                joint = myBody.Joints[key].Position;
                jointPoints.Add(new Vector3d(joint.X, joint.Y, joint.Z));

            }

            return jointPoints;
        }

        /// <summary>
        /// Function animateProjection
        /// 
        /// </summary>
        /// <param name="points"> List of points in 3-space with color data</param>
        public void animateProjection(List<KinectPoint> points)
        {
            if(points != null && points.Count > 0)
            {
                Console.WriteLine("ID " + id + " new frame: " +points.Count + " " + " points");
            } else
            {
                Console.Write("no joints");
            }
        }


        /// <summary>
        /// compare
        /// This function takes in a Body object & returns true if it is the body
        ///  that corresponds to this person and false otherwise
        /// </summary>
        /// <param name="Skeleton">the Body object we are comparing with (Kinect Skeleton)</param>
        /// <returns>whether or not the skeleton belongs to this person</returns>
        public bool compare(Body Skeleton)
        {
            if (Skeleton != null)
            {
                return this.id == Skeleton.TrackingId;
            }
            return false;

        }

        /// <summary>
        /// getPosition
        /// </summary>
        /// <returns> Returns a 3D vector representing one of this person's joints</returns>
        public Vector3d getPosition() {
            Vector3d ps=new Vector3d(myBody.Joints[0].Position.X, myBody.Joints[0].Position.Y, myBody.Joints[0].Position.Z);
            return ps;
        }

        /// <summary>
        /// render
        /// This function should be called every time we want to display and update the memory. 
        /// This function draws the current frame (snapshot) to the screen.
        /// 
        /// The longer since timeCreated, the larger the z-index offset (frame pushed back)
        /// </summary>
        public void render()
        {
            GL.Color4(100,200, 20, 1);
            GL.PointSize(300);  // Changing point size gives some cool abstract results
            GL.Enable(EnableCap.DepthTest);
            GL.Begin(PrimitiveType.Points); // try other primitives  

            CameraSpacePoint joint;
            IEnumerable<JointType> keys = myBody.Joints.Keys;

            foreach (JointType key in keys)
            {
                joint = myBody.Joints[key].Position;
                GL.Vertex3(joint.X*-10, joint.Y*-10, joint.Z*-10);

                Console.WriteLine("ID " + id + " new frame: (" + joint.X + ", " + joint.Y + ", " +  joint.Z + ")");
                break;
            }

            GL.End();

                //Console.WriteLine("mem " + id + " frame:" + currentFrame); // debugging
                
            
            
        }


    }
}