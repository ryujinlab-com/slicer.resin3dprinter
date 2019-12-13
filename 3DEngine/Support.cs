using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UV_DLP_3D_Printer;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform.Windows;
using System.Drawing;
namespace Engine3D
{
    /// <summary>
    /// a support derives from the cylinder class
    /// </summary>
    /// 
    /*  
     // typical support resting on floor
      __
     /__\
     |  |
     |  |
     |__|
    /____\
      __
     /__\
     |  |
     |  |
    _|__|_
   |______|
     
     * 
     * 
    
     //typical support acting as a suport between object parts
      __
     /__\   - D3  - The 'Head'
     |  |
     |  |   - D2  - The 'Body'
     |__|
     \__/   - D1  - The 'Foot'
     
     * We'll generate this object from the bottom up
     * it has 4 segments , the top and the bottom face each have an extra point in the center for easier triangulation 
     * the bottom face has numdivpnts +1 
     * the next seg up has numdivpnts
     * the next seg up has numdivpnts
     * the top seg up has numdivpnts + 1
     * 
     * we want to support scaling of this object initially by keeping the distance D1 and D3 the same while scaling D2
     * 
     * We should be able to change the top and bottom radius of any of the head, body,or foot, 
     * as well as the height of any of these segments
     * 
     * In latyer revisions of this, we would want to:
     * translate (x,y) the body, but keep the top and bottom stationary
     * translate the bottom of the foot, and the top of the head
     * match the surface normal of the top of the head, bottom of the foot to meet with the surface we're attaching too
     */
    public class Support : Cylinder3d
    {
        // here, we keep indexes into the point array, so we can scale/move them easier

        private int s1i; // the starting index of the bottom of the foot 
        private int s2i; // the starting index of the top of the foot/ bottom of the body
        private int s3i; // the starting index of the top of the foot/ bottom of the body
        private int s4i; // top of the body, bottom of the head
        private int s5i; // top of the head
        private int cdivs;
        public Object3d m_supporting; // the obeject that this is attached to
        private static int supcnt = 0;
        public Support() 
        {
            tag = Object3d.OBJ_SUPPORT; // tag for support
            m_supporting = null;
            this.Name = "Support_" + supcnt.ToString();
            supcnt++;
        }
        /// <summary>
        /// This function creates a new support structure
        /// you can specify the:
        /// Foot Bottom Radius  - fbrad
        /// Foot Top Radius     - ftrad
        /// Head Bottom Radius  - hbrad
        /// Head Top Radius     - htrad
        /// 
        /// as well as the segment lengths from bottom to top D1,D2,D3
        /// you can also specify the number of divisions in the circle - divs
        /// </summary>
        /// <param name="fbrad"></param>
        /// <param name="ftrad"></param>
        /// <param name="hbrad"></param>
        /// <param name="htrad"></param>
        /// <param name="d1"></param>
        /// <param name="d2"></param>
        /// <param name="d3"></param>
        public void Create(Object3d supporting, float fbrad, float ftrad, float hbrad, float htrad, float d1, float d2, float d3, int divs)
        {
            try
            {
                m_supporting = supporting;
                cdivs = divs;
                float zlev = 0.0f; // start at the bottom of the cylinder
                s1i = 0; // set 0 to be the starting index for the bottom of the foot
                GenerateCirclePoints(fbrad, divs, zlev, true); // foot bottom
                zlev += d1;
                //now the top of the foot
                s2i = m_lstpoints.Count;
                GenerateCirclePoints(ftrad, divs, zlev, false); // foot top

                //zlev += d1;
                s3i = m_lstpoints.Count;
                GenerateCirclePoints(ftrad, divs, zlev, false); // foot top
                
                zlev += d2;

                //now the bottom of the shaft
                s4i = m_lstpoints.Count;
                GenerateCirclePoints(hbrad, divs, zlev, false); // bottom of head
                zlev += d3;
                //now the top of the shaft, bottom of the head
                s5i = m_lstpoints.Count;
                GenerateCirclePoints(htrad, divs, zlev, true); // top of head

                MakeTopBottomFace(s1i, divs, false);// bottom
                MakeTopBottomFace(s5i, divs, true);// top
                //MakeTopBottomFace(s4i, divs, true);// top
                
                makeWalls(s1i, s2i, divs);               
                makeWalls(s2i, s3i - divs - 1, divs);
                makeWalls(s3i, s4i - (2*divs) - 1, divs);

                makeWalls(s4i, s5i - (3 * divs) - 1, divs);

                Update();
                SetColor(Color.Yellow);
                ScaleToHeight(d1 + d2 + d3);
            }
            catch (Exception ex) 
            {
                DebugLogger.Instance().LogError(ex.Message);

            }
        }

        public Support MakeCopy() 
        {
            Support obj = new Support();
            try
            {
                obj.m_name = UVDLPApp.Instance().Engine3D.GetUniqueName(this.m_name); // need to find unique name
                obj.m_fullname = this.m_fullname;
                obj.tag = this.tag;

                foreach (Polygon ply in m_lstpolys)
                {
                    Polygon pl2 = new Polygon();
                    pl2.m_color = ply.m_color;
                    pl2.m_points = new Point3d[3];
                    obj.m_lstpolys.Add(pl2);
                    pl2.m_points[0] = new Point3d(ply.m_points[0]);
                    pl2.m_points[1] = new Point3d(ply.m_points[1]);
                    pl2.m_points[2] = new Point3d(ply.m_points[2]);
                }
                foreach (Polygon ply in obj.m_lstpolys)
                {
                    foreach (Point3d pnt in ply.m_points)
                    {
                        obj.m_lstpoints.Add(pnt); // a fair bit of overlap, but whatever...
                    }
                }
                obj.Update();
            }
            catch (Exception ex)
            {
                DebugLogger.Instance().LogError(ex);
            }
            return obj;            
        }
        private void makeWalls(int li, int ui, int numdivs)
        {
            for (int cnt = 0; cnt < numdivs; cnt++)
            {
                int topidx = ui;// +1;// numdivs + 1; // index to the first point in the top circle
                Polygon plyl = new Polygon();
                m_lstpolys.Add(plyl);
                plyl.m_points = new Point3d[3]; // create some point storage
                plyl.m_points[0] = (Point3d)m_lstpoints[cnt + li];
                plyl.m_points[1] = (Point3d)m_lstpoints[((cnt + 1) % numdivs) + li];
                plyl.m_points[2] = (Point3d)m_lstpoints[cnt + topidx + li];
                
                Polygon plyr = new Polygon();
                m_lstpolys.Add(plyr);
                plyr.m_points = new Point3d[3]; // create some point storage
                plyr.m_points[0] = (Point3d)m_lstpoints[(cnt + 1) % numdivs + li];
                plyr.m_points[1] = (Point3d)m_lstpoints[((cnt + 1) % numdivs) + topidx + li]; // 
                plyr.m_points[2] = (Point3d)m_lstpoints[cnt + topidx + li]; // the point directly above it
                 
            }
        }
        // given the top or bottom starting index, make the surface face
        private void MakeTopBottomFace(int idx, int numdivs, bool top) 
        {
            try
            {
                int centeridx = idx + numdivs;
                for (int cnt = 0; cnt < numdivs; cnt++)
                {
                    Polygon plt = new Polygon();
                    m_lstpolys.Add(plt);
                    plt.m_points = new Point3d[3]; // create some point storage
                    plt.m_points[0] = (Point3d)m_lstpoints[centeridx]; // the first point is always the center pointt
                    if (top)
                    {
                        plt.m_points[2] = (Point3d)m_lstpoints[(cnt + 1) % numdivs + idx];
                        plt.m_points[1] = (Point3d)m_lstpoints[idx + cnt];
                    }
                    else
                    {
                        plt.m_points[1] = (Point3d)m_lstpoints[(cnt + 1) % numdivs + idx];
                        plt.m_points[2] = (Point3d)m_lstpoints[idx + cnt];
                    }
                }
            }
            catch (Exception ex) 
            {
                DebugLogger.Instance().LogError(ex.Message);
            }
        }
        /// <summary>
        /// Calculate the centroid of the given points
        /// </summary>
        /// <param name="?"></param>
        /// <param name="startidx"></param>
        /// <param name="endidx"></param>
        /// <returns></returns>
        private Point3d CalcCentroid(int startidx, int endidx)
        {
            Point3d center = new Point3d();
            //calculate the center
            for (int c = startidx; c < endidx; c++) 
            {
                center.x += m_lstpoints[c].x;
                center.y += m_lstpoints[c].y;
                center.z += m_lstpoints[c].z;
            }
            float num = endidx - startidx;
            center.x /= num;
            center.y /= num;
            center.z /= num;
            return center;
        }

        private void TranslateRange(Point3d pnt, int startidx, int endidx) 
        {
            for (int c = startidx; c < endidx; c++)
            {
                m_lstpoints[c].x += pnt.x;
                m_lstpoints[c].y += pnt.y;
                m_lstpoints[c].z += pnt.z;
            }        
        }
        /// <summary>
        /// This function is designed to move a support by it's tip
        /// it will angle up from the s5i index and position the points to intersect around the 
        /// specified tip point,
        /// all points below s5i  - 
        /// 1) could stay where they are
        /// 2) or move to be a relative distance from the tip - 5mm or so in the direction of vec
        /// </summary>
        /// <param name="tip"></param>
        public void MoveFromTip(Point3d tip, Vector3d vec) 
        {
            //starting at s5i, center all points around this.
            Point3d diff = new Point3d();
            ScaleToHeight(tip.z * .85);
            Point3d center;
            // first, move along the vec in the direction of the vector
            center = CalcCentroid(0,s5i);
            diff.Set(tip.x + vec.x - center.x, tip.y + vec.y - center.y, 0.0f); // only slide along the x/y plane
            TranslateRange(diff,0, s5i);

            center = CalcCentroid(s5i, m_lstpoints.Count);            
            diff.Set(tip.x - center.x,tip.y - center.y,tip.z - center.z);
            TranslateRange(diff, s5i, m_lstpoints.Count);
            Update();
        }
        public void ScaleToHeight(double height) 
        {
            if (height == 0.0d) height = 1.0;
            if (height <= 0.0d) height = 1.0;

            FindMinMax();
            double h = m_max.z - m_min.z; // current height
            //assuming the h= 1.5, and I want it to be height = 4
            double sval = h/height;
            sval = 1/sval;
            Scale(1.0f, 1.0f, (float)sval); // scale to the height   
            if (height > 3.0d)
            {
                float bpos = m_lstpoints[0].z;
                for (int c = s2i; c < s4i; c++ )
                {
                    m_lstpoints[c].z = (float)(bpos + 1.0f);
                }

                for (int c = s4i; c < s5i; c++)
                {
                    m_lstpoints[c].z = (float)(height + bpos - 2.0f);
                }

                // the distance from the top of the foot to the bottom should be 1
                // the tip should be 2 mm (or more)
            }
                
        }

        public void AddToHeight(double delta_height)
        {
            FindMinMax();
            double h = m_max.z - m_min.z; // current height
            ScaleToHeight(h + delta_height);
        }


        public override void RenderGL(bool showalpha, bool selected, bool renderOutline, Color renderColor)
         {
             GL.Begin(PrimitiveType.Lines);//.LineStrip);
             GL.Color4(Color4.Red);
             for (int c = 0; c < cdivs; c++) 
             {
                 Point3d p = (Point3d)m_lstpoints[s1i  + c];
                 GL.Vertex3(p.x, p.y, p.z);
             }
             GL.End();

             GL.Begin(PrimitiveType.Lines);//.LineStrip);
             GL.Color4(Color4.Red);
             for (int c = 0; c < cdivs; c++)
             {
                 Point3d p = (Point3d)m_lstpoints[s2i + c];
                 GL.Vertex3(p.x, p.y, p.z);
             }
             GL.End();

             GL.Begin(PrimitiveType.Lines);//.LineStrip);
             GL.Color4(Color4.Red);
             for (int c = 0; c < cdivs; c++)
             {
                 Point3d p = (Point3d)m_lstpoints[s3i + c];
                 GL.Vertex3(p.x, p.y, p.z);
             }
             GL.End();

             GL.Begin(PrimitiveType.Lines);//.LineStrip);
             GL.Color4(Color4.Red);
             for (int c = 0; c < cdivs; c++)
             {
                 Point3d p = (Point3d)m_lstpoints[s4i  + c];
                 GL.Vertex3(p.x, p.y, p.z);
             }
             GL.End();
             base.RenderGL(showalpha, selected, renderOutline, renderColor);
         }
    }
}
