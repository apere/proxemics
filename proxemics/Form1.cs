using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace proxemics
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            
            InitializeComponent();

            glControl1.Resize += glControl1_Resize;
            glControl1.Load += glControl1_Load;
            glControl1.Paint += glControl1_Paint;
            this.FormClosing += Form1_FormClosing;

            glControl1.MouseDown += GlControl1_MouseDown;
            glControl1.MouseMove += GlControl1_MouseMove;
            glControl1.MouseUp += GlControl1_MouseUp;
        }

        MouseButtons db = MouseButtons.None;
        private void GlControl1_MouseUp(object sender, MouseEventArgs e)
        {
            db = MouseButtons.None;
            glControl1.Capture = false;
        }

        private void GlControl1_MouseMove(object sender, MouseEventArgs e)
        {
            mediawin.MouseMove(e.X, glControl1.Height - e.Y, db);
        }

        private void GlControl1_MouseDown(object sender, MouseEventArgs e)
        {
            db = e.Button;
            glControl1.Capture = true;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            mediawin.Terminate();
        }

        MediaWindow mediawin = new MediaWindow();
        bool loaded = false;
        Timer timer = new Timer();


        void UpdateFrame()
        {
            if (!loaded) return;
            mediawin.OnFrameUpdate();
            glControl1.SwapBuffers();
        }

        private void glControl1_Load(object sender, EventArgs e)
        {
            mediawin.Initialize();

            loaded = true;
            timer.Interval = 35;
            timer.Enabled = true;
            timer.Start();
            timer.Tick += new EventHandler(timer_Tick);

            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.PointSmooth);
            GL.Enable(EnableCap.LineSmooth);

            GL.ColorMaterial(MaterialFace.FrontAndBack, ColorMaterialParameter.Diffuse);
            GL.Enable(EnableCap.ColorMaterial);
            GL.Enable(EnableCap.Normalize);


            GL.LightModel(LightModelParameter.LightModelTwoSide, 1);
            GL.LightModel(LightModelParameter.LightModelLocalViewer, 1);

            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
        }

        void timer_Tick(object sender, EventArgs e)
        {
            if (!loaded) return;
            UpdateFrame();
        }

        private void glControl1_Resize(object sender, EventArgs e)
        {
            mediawin.Width = glControl1.Width;
            mediawin.Height = glControl1.Height;
            

            if (!loaded) return;

            GL.Viewport(0, 0, mediawin.Width, mediawin.Height); // Use all of the glControl painting area

            UpdateFrame();
        }

        private void glControl1_Paint(object sender, PaintEventArgs e)
        {
            if (!loaded) return;
            UpdateFrame();
        }

        private void glControl1_Load_1(object sender, EventArgs e)
        {

        }
    }
}
