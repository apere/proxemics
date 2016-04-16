using System;
using Microsoft.Kinect;
using System.Collections.Generic;
using System.Linq;
using System.Text;


using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;

namespace proxemics
{
    /// <summary>
    /// Memory Class
    /// Authors: Adam M Pere, Keebaik Sim, Enol Vallina
    /// Introduction to Computational Design - Fall 2015, Harvard Graduate School of Design
    /// 
    /// The memory class represents a recording of people that were once standing in front of the kinect sensor but are no longer.
    /// The 'memory' of a person object.
    /// </summary>
    public class Memory
    {
        DateTime timeCreated;
        ulong id;
        int currentFrame; // the index of the snapshot that should be displayed next time render is called
        List<List<KinectPoint>> snapshots; // visual representation of memory

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="i">The ID associated with this memory. It should be the same ID as the person object this memory belongs to and the kinect skeleton associated with that body</param>
        public Memory(ulong i)
        {
            timeCreated = DateTime.Now;
            snapshots = new List<List<KinectPoint>>();
            currentFrame = 0;
            id = i;
        }

        /// <summary>
        /// getID
        /// </summary>
        /// <returns>The ID of this memory object</returns>
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
        /// getNumberOfFrames
        /// </summary>
        /// <returns>the number of frames contained in this memory</returns>
        public int getNumberOfFrames()
        {
            return snapshots.Count;
        }

        /// <summary>
        /// getCurrentFrameIndex
        /// </summary>
        /// <returns>the index of the currently displayed frame</returns>
        public int getCurrentFrameIndex()
        {
            return currentFrame;
        }
        
        /// <summary>
        /// getLocation
        /// </summary>
        /// <returns>The location of this memory = the Vector3d of the first point in the current frame</returns>
        public Vector3d getLocation()
        {
            double offset = getTimeSinceCreated().TotalSeconds / 20;
            return new Vector3d(snapshots[currentFrame][0].p.X, snapshots[currentFrame][0].p.Y, snapshots[currentFrame][0].p.Z + offset);
        }

        /// <summary>
        /// add
        /// This function is called to add a ‘snapshot’ to our array of frames. This should only be called from a person object.
        /// </summary>
        /// <param name="snapshotPoints">A list of Kinect points that represent an image that we want to add to this memory</param>
        public void add(List<KinectPoint> snapshotPoints) // parameters?
        {
            snapshots.Add(snapshotPoints);
            Console.WriteLine("mem " + id + " " + snapshots.Count + "snapshots with " + snapshotPoints.Count + " points");
        }


        /// <summary>
        /// getClosestPerson
        /// This function loops through the supplied array of Person objects and finds the one physically closest to this memory. --- may not be using anymore
        /// </summary>
        /// <param name="persons">a list of person objects that we want to check within<param>
        /// <returns>the person in 'persons' that is physically closest to this memory</returns>
        public Person getClosestPerson(List<Person> persons)
        {  
            Person closestPerson=persons[0];
            Vector3d position = closestPerson.getPosition();
            Vector3d location = getLocation();
            double dist = Math.Abs(Vector3d.Subtract(position, location).Length);
            double closestDist = dist;

            foreach (Person p in persons)
            {
                position = p.getPosition();
                dist = Math.Abs(Vector3d.Subtract(position, location).Length);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestPerson = p;
                }
            }
            return closestPerson;
        }

        /// <summary>
        /// getOrientation
        /// Finds the direction facing the closest person
        /// </summary>
        /// <param name="persons"></param>
        /// <returns>A Vector pointing in the direction that we want to face</returns>
        public Vector3d getOrientation(List<Person> persons)
        {
            return this.getClosestPerson(persons).getPosition().Normalized();
        }

        public int step = 1;
        /// <summary>
        /// render
        /// This function should be called every time we want to display and update the memory. 
        /// This function draws the current frame (snapshot) to the screen.
        /// 
        /// The longer since timeCreated, the larger the z-index offset (frame pushed back)
        /// </summary>
        public void render()
        {
            if (snapshots.Count > 0 && currentFrame >= 0 && currentFrame < snapshots.Count)
            {
                List<KinectPoint> currShot = snapshots[currentFrame];
                double zIndex = currShot[0].p.Z;
                double offset = getTimeSinceCreated().TotalSeconds / 20; // time offset for z-axis
                double buzz = getTimeSinceCreated().Seconds / 20;


                //**** Optimizations: 
                //**** try putting Gl.Begin... outside of memeory (outside of the for loop in mediawindow) *****
                //**** do you need to change the color every time?


                GL.PointSize(4);  // Changing point size gives some cool abstract results
                GL.Enable(EnableCap.DepthTest);
                GL.Begin(PrimitiveType.Points); // try other primatives  
                double avg;
                foreach (KinectPoint kp in currShot)
                {
                    avg = (kp.color.R + kp.color.G + kp.color.G) / 3;
                    GL.Color4(avg, avg, avg, 1);
                    GL.Vertex3(kp.p.X, kp.p.Y + offset/25, kp.p.Z + offset); // add z-index offset?? ***** then change to scalar and add to the z OR use gltranslate?
                }
                GL.End();

                //Console.WriteLine("mem " + id + " frame:" + currentFrame); // debugging

                currentFrame = currentFrame + step;
                if (currentFrame >= snapshots.Count - 10) // start over?
                {
                    step = -1;
                }
                else if (currentFrame <= 0)
                {
                    step = 1;
                }
            }
            if(currentFrame < 0)
            {
                currentFrame = 0;
                step = 1;
            } else if(currentFrame >= snapshots.Count - 10)
            {
                currentFrame = snapshots.Count - 10;
            }
        }

    }
}