using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;


namespace UV_DLP_3D_Printer.Configs
{
    /// <summary>
    /// This is a class for holding configuartion to generate
    /// Automatic or manual support structures.
    /// </summary>
[Serializable]
    public class SupportConfig
    {
        public enum eAUTOSUPPORTTYPE 
        {
            eBON, // bed of nails
            eADAPTIVE, // a tree-like structure
            eADAPTIVE2, // a tree-like structure
        }
        public const int FILE_VERSION = 1; // this should change every time the format changes
        public double xspace, yspace;
        public double mingap; // minimum gap between adaptively generated supports
        public double htrad; // head top radius 
        public double hbrad; // head bottom radius
        public double ftrad; // foot top radius
        public double fbrad; // foot bottom radius
        public double fbrad2; // foot bottom radius 2
        public int vdivs; // vertical divisions, not used
        public bool m_onlydownward;
        public eAUTOSUPPORTTYPE eSupType;

        public SupportConfig() 
        {
            eSupType = eAUTOSUPPORTTYPE.eBON;
            xspace = 5.0; // 5 mm spacing
            yspace = 5.0; // 5 mm spacing
            mingap = 5.0; // 5 mm spacing
            htrad = .2;//
            hbrad = .5; //
            ftrad = .5;
            fbrad = 2; // for support on the platform
            fbrad2 = .2; // for intra-object support
            //vdivs = 1; // divisions vertically
            m_onlydownward = false;
        }
        
        public void Load(String filename)
        {
            XmlHelper xh = new XmlHelper();
            bool fileExist = xh.Start(filename, "SupportConfig");
            XmlNode sc = xh.m_toplevel;

            xspace = xh.GetDouble(sc, "XSpace", 5.0);
            yspace = xh.GetDouble(sc, "YSpace", 5.0);
            mingap = xh.GetDouble(sc, "MinAdaptiveGap", 5.0);
            htrad = xh.GetDouble(sc, "HeadTopRadiusMM", 0.2);
            hbrad = xh.GetDouble(sc, "HeadBottomRadiusMM", 0.5);
            ftrad = xh.GetDouble(sc, "FootTopRadiusMM", 0.5);
            fbrad = xh.GetDouble(sc, "FootBottomRadiusMM", 2.0);
            fbrad2 = xh.GetDouble(sc, "FootBottomIntraRadiusMM", 0.2);

            if (!fileExist)
            {
                xh.Save(FILE_VERSION);
            }
        }

        public void Save(String filename)
        {
            XmlHelper xh = new XmlHelper();
            xh.Start(filename, "SupportConfig");
            XmlNode sc = xh.m_toplevel;
            xh.SetParameter(sc, "XSpace", xspace);
            xh.SetParameter(sc, "YSpace", yspace);
            xh.SetParameter(sc, "MinAdaptiveGap", mingap);
            xh.SetParameter(sc, "HeadTopRadiusMM", htrad);
            xh.SetParameter(sc, "HeadBottomRadiusMM", hbrad);
            xh.SetParameter(sc, "FootTopRadiusMM", ftrad);
            xh.SetParameter(sc, "FootBottomRadiusMM", fbrad);
            xh.SetParameter(sc, "FootBottomIntraRadiusMM", fbrad2);
            xh.Save(FILE_VERSION);
        }

    }
}
