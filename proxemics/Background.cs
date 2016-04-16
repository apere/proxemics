using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using System.Windows.Forms;

namespace proxemics
{
    /// <summary>
    /// Background Class
    /// Authors: Adam M Pere, Keebaik Sim, Enol Vallina
    /// Introduction to Computational Design - Fall 2015, Harvard Graduate School of Design
    /// 
    /// The Background class handles all computations and drawing of the background scene.
    /// </summary>
    public class Background
    {

        public double time = 0.0; //time variable. We'll increase its value by a small amount at each frame iteration

        public double heightT = 0.5;
        public double lengthT = 100;
        public float pointDensity = 0.1f;
        public float yRot = 90;
        public float smoothW = 200;
        public float smoothH = 20;

        public double minX = 0.0; // 0 is a bad idea ... but we'll allow it
        public double minZ = 0.0;
        public double maxX = 0.0;
        public double maxZ = 0.0;
        public double x, y, z;


        public Background()
        {

        }

        float offset = 1f;

        /// <summary>
        /// onframeUpdate
        /// The function to be called on every frame update. This calculates and draws the background scene.
        /// </summary>
        public void onframeUpdate()
        {
            time += 0.1; //time step
            smoothH = smoothH + offset;
            if(smoothH < 20)
            {
                offset = 1f;
            } else if(smoothH > 80)
            {
                offset = -1f;
            }
            smoothW = smoothW + offset;
 
            GL.Rotate(yRot, -.1, 1.0, 0);
            GL.Translate(-700, -1.9, 6.0);
            GL.Begin(PrimitiveType.LineStrip);
            GL.Color3(0.5f, 0.5f, 0.5f);
            GL.PointSize(0.5f);

            for (float v = 0; v <= lengthT * Math.PI; v = v + pointDensity)
            {
                for (float u = 0; u <= Math.PI; u = u + pointDensity)
                {
                    x = u + v;
                    y = (u + Math.Sin(4 * v) / smoothW) * Math.Sin(u);
                    z = (u * heightT + Math.Sin(u + v) / smoothH) * Math.Cos(u);

                    GL.Vertex3(x*10, y*16, z*10);

                    if (x < minX)
                    {
                        minX = x;
                    }
                    if (z < minZ)
                    {
                        minZ = z;
                    }
                    if (x > maxX)
                    {
                        maxX = x;
                    }
                    if (z > maxZ)
                    {
                        maxZ = z;
                    }
                }
            }
            GL.End();
        }
    }
}

      


