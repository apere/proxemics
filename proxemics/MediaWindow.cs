using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;
using Microsoft.Kinect;

using System.Windows.Forms;
using System.Media;

namespace proxemics
{
    /// <summary>
    /// Media Window
    /// Authors: Adam M Pere, Keebaik Sim, Enol Vallina
    /// Introduction to Computational Design - Fall 2015, Harvard Graduate School of Design
    /// 
    /// The MediaWindow class is where the OpenGL scene is set up and where the main communication with the Kinect Sensor happens.
    /// </summary>
    public class MediaWindow
    {
        public int Width = 0;       //width of the viewport in pixels
        public int Height = 0;      //height of the viewport in pixels
        public double MouseX = 0.0; //location of the mouse along X
        public double MouseY = 0.0; //location of the mouse along Y

        public KinectHelper kinect = new KinectHelper(true, true, true);  // wrapper object for kinect sensor
        public List<Person> persons = new List<Person>();                 // list of people currently standing in front of kinect sensor
        public List<Memory> memories = new List<Memory>();                // list of the 'memories' of people who once stood in front of the kinect sensor

        public IDictionary<ulong, List< Vector3d >> allJoints = new Dictionary<ulong,List< Vector3d>>();        // list of all joints currently being detected - separated by user (key = id)
        public IDictionary<ulong, List< KinectPoint >> allPoints = new Dictionary<ulong, List< KinectPoint >>();   // list of all points within a certain distance of a joint - separated by user (key = id)

        Background b = new Background();
        WMPLib.WindowsMediaPlayer everlastingSound =  new WMPLib.WindowsMediaPlayer();
        WMPLib.WindowsMediaPlayer drop = new WMPLib.WindowsMediaPlayer();

        /// <summary>
        /// Initialize
        /// </summary>
        public void Initialize()
        {
            kinect.Initialize();
            everlastingSound.URL = @"C:\Users\adam pere\Documents\Visual Studio 2015\Projects\Computational-Design---Final-Project\media\ambient_loop.wav";
            drop.URL = @"C:\Users\adam pere\Documents\Visual Studio 2015\Projects\Computational-Design---Final-Project\media\echo1-1drop.wav";
            everlastingSound.settings.setMode("loop", true);
            everlastingSound.controls.play();
        }

        /// <summary>
        /// Terminate
        /// </summary>
        public void Terminate()
        {
            kinect.Close();
        }

        // Camera Variables
        public double AngleXZ = 300.0;
        public double AngleY = 0.0;
        public double Distance = 4.0;

        public Vector3d Eye;
        public Vector3d Target=new Vector3d(0.0, 0.0, 2.0);
        public Vector3d Up = new Vector3d(0.0, 1.0, 0.0);

        double mouseX0 = 0.0;
        double mouseY0 = 0.0;


        /// <summary>
        /// onFrameUpdate
        /// This is the animation function. It is called ~20x per second to update the screen
        /// </summary>
        public void OnFrameUpdate()
        {
            // Scene Setup
            GL.ClearColor(1f, 1f, 1f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.MatrixMode(MatrixMode.Projection);
            Matrix4d pmat = Matrix4d.Perspective(Math.PI * 0.35, Width / Height, 0.1, 700.0);
            GL.LoadMatrix(ref pmat);

            GL.MatrixMode(MatrixMode.Modelview);
            Eye.X = Target.X + Distance * Math.Cos(AngleXZ) * Math.Cos(AngleY);
            Eye.Y = Target.Y + Distance * Math.Sin(AngleY) - .8; 
            Eye.Z = Target.Z + Distance * Math.Sin(AngleXZ) * Math.Cos(AngleY);

            Matrix4d vmat = Matrix4d.LookAt(Eye, Target, Up);
            GL.LoadMatrix(ref vmat);





            // Any new people standing in front of the kinect?
            if (kinect.HasSkeletonData && kinect.HasDepthData)
            {
                addSkeletons();
                allPoints = findPoints();
            }

            // Remove or take a snapshot of any person currently in front of the kinect
            removeOrSnap();

            List<Memory> memToRemove = new List<Memory>();
            // Render each memory
            foreach (Memory mem in memories)
            {
                if(mem.getNumberOfFrames() < 20)
                {
                    memToRemove.Add(mem);
                } else
                {
                    mem.render();
                }
                 
            }
            foreach (Memory removeMe in memToRemove)
            {
                memories.Remove(removeMe);
            }

            

            Console.WriteLine(" ");
            Console.WriteLine(persons.Count + " persons");
            Console.WriteLine(memories.Count + " memories");
            Console.WriteLine("----");
            allJoints.Clear();
            //allPoints.Clear();
            b.onframeUpdate();

        } 


        /// <summary>
        /// MouseMove
        /// Use the mouse to change the camera's view
        /// </summary>
        public void MouseMove(double x, double y, MouseButtons button)
        {
            mouseX0 = MouseX;
            mouseY0 = MouseY;

            MouseX = x;
            MouseY = y;

            double dx = MouseX - mouseX0;
            double dy = MouseY - mouseY0;

            if (button == MouseButtons.Left)
            {
                AngleXZ += dx * 0.01;
                AngleY += dy * 0.01;
            }
            else if (button == MouseButtons.Right)
            {
                Distance += dy * 0.01;
            }
        }

        /// <summary>
        /// findPoints
        /// </summary>
        /// <return>a dictionary of user's kinect points. Each kinect point corresponds to a user's joint</return> 
        public IDictionary<ulong, List<KinectPoint>> findPoints()
        {
            IDictionary<ulong, List<KinectPoint>> allKPoints = new Dictionary<ulong, List<KinectPoint>>();
            ICollection<ulong> ids = allJoints.Keys;
            List<Vector3d> jointPoints;
            KinectPoint kp;
            double dist;
            int step = 6;

            if (allJoints != null && allJoints.Count > 0) // error checking 
            {
                foreach (ulong id in ids) // get all id's
                {
                    if (allJoints[id] != null && allJoints[id].Count > 0)
                    {
                        allKPoints.Add(id, new List<KinectPoint>());
                        
                    }
                    
                }

                ids = allKPoints.Keys; // only loop through ids that have joints

                if (ids != null && ids.Count > 0)
                {
                    for (int j = 0; j < kinect.DepthHeight; j += step) // loop through every kinect point
                    {
                        for (int k = 0; k < kinect.DepthWidth; k += step)
                        {
                            kp = kinect.Points[k, j]; // get current KinectPoint

                            foreach (ulong id in ids) // loop through every user (w/ joints)
                            {
                                jointPoints = allJoints[id];
                                foreach(Vector3d jointPoint in jointPoints) // check to see if current kinect point belongs to any joint
                                {
                                    dist = Math.Abs(Vector3d.Subtract(kp.p, jointPoint).Length);
                                    if (dist <= .275) // if the point is within the threshold, add it
                                    {
                                        allKPoints[id].Add(kp);
                                    }
                                }
                                
                            }
                        }
                    }
                } 
                
            }
            
            return allKPoints;
        }

        /// <summary>
        /// addSkeletons
        /// Gets the joint data for every person object ---- may have to do this after add (or remove).
        /// If it finds a person who has just been detected by the kinect this frame, it will add them to our list of persons
        /// </summary>
        public void addSkeletons()
        {
            bool identified = false;
            bool getJoints = true;
            Person temp; 

            foreach (Body skeleton in kinect.Bodies)  // do I have a person object for each skeleton
            {
                foreach (Person p in persons)
                {
                    if (getJoints) // collect joint data for every person
                    {
                        getJoints = false;
                        allJoints.Add(p.getID(), p.getJoints());
                    }
                    if (!identified && p.compare(skeleton)) // ignore previously identified person
                    {
                        identified = true;
                    }
                }
                if (!identified && skeleton.TrackingId != 0) // if we haven't identified the person, add them to our persons list.
                {
                    temp = new Person(skeleton, kinect);
                    persons.Add(temp);
                    if(kinect.Bodies.Last() == skeleton)
                    {
                        allJoints.Add(temp.getID(), temp.getJoints());
                    }
                }

                identified = false; // reset 'identified' for next skeleton
            }
        }

        /// <summary>
        /// removeOrSnap
        /// This function either removes the person object (and stores their memory) of a person no longer being tracked by the kinect 
        /// OR it invokes taking a snapshot of that person
        /// </summary>
        public void removeOrSnap()
        {
            bool removed = true;
            List<Person> toRemove = new List<Person>();
            Person pp;
            int numPeople = persons.Count;

            for (int i = 0; i < numPeople; i++) // check to see if we are keeping track of people that are no longer in front of the installation and should be removed (turned into memories)
            {
                pp = persons[i];
                foreach (Body skeleton in kinect.Bodies)
                {
                    if (pp.compare(skeleton)) // assume removed until we've found the person's skeleton
                    {
                        Console.WriteLine("Not removing person " + pp.getID());
                        removed = false;
                        break;
                    }
                }

                if (removed) // if the person is no longer physically in front of the installation
                {
                    memories.Add(pp.getMemory()); // add person's memory to our list of memories
                    toRemove.Add(pp); // remove person from our list of currently tracked persons
                    //  Console.Write("person removed");
                }
                else // if the person is still physically in front of installation and being tracked
                {                    
                    if(allPoints.ContainsKey(pp.getID()) && allPoints[pp.getID()] != null && allPoints[pp.getID()].Count > 0)
                    {
                        pp.takeSnapshot(allPoints[pp.getID()]); // add to that person's memory
                    }
                }
                removed = true;
            }

            foreach (Person p in toRemove)
            {
                persons.Remove(p);
                Console.WriteLine("removed " + p.getID());
            }
            if(toRemove.Count > 0)
            {
                drop.controls.play();
            }
            toRemove.Clear();
        }
    }
}