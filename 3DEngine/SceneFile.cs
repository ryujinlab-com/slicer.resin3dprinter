using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ionic.Zip;
using Engine3D;
using System.IO;
using System.Xml;
using UV_DLP_3D_Printer.Configs;
using UV_DLP_3D_Printer.Slicing;
using System.Drawing;

namespace UV_DLP_3D_Printer._3DEngine
{
    /// <summary>
    /// This class is a singleton. It's purpose is to load and save an entire scene into a zip file
    /// This allows the user to later load/save the scene that they were previously working on
    /// it will save the scene into a zip file along with an XML file that stores a manifest as well as additional
    /// metadata about each object such as tag info, current selected onject, etc...
    /// this file can also store the png slices (if sliced) or an SVG file
    /// </summary>
    public class SceneFile
    {
        private static SceneFile m_instance = null;
        private ZipFile mZip; // this handle is used for when we're slicing and adding entries
        private XmlHelper mManifest; // this handle is used for when we're slicing and adding slice entries
        
        private SceneFile() 
        {
            mZip = null;
            mManifest = null;
        }

        /// <summary>
        /// Opens a file in preparation for writing a series of image entries
        /// should load the manifest so we can add / remove entries from it
        /// </summary>
        /// <returns></returns>
        public bool OpenSceneFile(string scenefilename) 
        {
            try
            {
                mZip = ZipFile.Read(scenefilename);
                //open the manifest file                
                string xmlname = "manifest.xml";
                ZipEntry manifestentry = mZip[xmlname];
                //get memory stream
                MemoryStream manistream = new MemoryStream();
                //extract the stream
                manifestentry.Extract(manistream);
                //read from stream
                manistream.Seek(0, SeekOrigin.Begin); // rewind the stream for reading
                //create a new XMLHelper to load the stream into
                mManifest = new XmlHelper();
                //load the stream
                mManifest.LoadFromStream(manistream, "manifest");

                return true;
            }
            catch (Exception ex) 
            {
                DebugLogger.Instance().LogError(ex);
            }
            return false;
        }
        /// <summary>
        /// Add a single image slice to the entry in the manifest and store in the zip
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool AddSlice(MemoryStream ms, string imname) 
        {
            try
            {
                // store the slice file into the zip
                mZip.AddEntry(imname, ms);
                // find the slices node in the top level
                XmlNode slicesnode = mManifest.FindSection(mManifest.m_toplevel, "Slices");
                if (slicesnode == null)  // no slice node
                {
                    //create one
                    slicesnode = mManifest.AddSection(mManifest.m_toplevel, "Slices");
                }
                //add the slice file name into the manifest
                XmlNode curslice = mManifest.AddSection(slicesnode, "Slice");
                mManifest.SetParameter(curslice,"name",imname);                
            }
            catch (Exception ex) 
            {
                DebugLogger.Instance().LogError(ex);
            }
            return false;
        }
        /// <summary>
        /// Slicing is finished (or cancelled) close the cws file
        /// </summary>
        public void CloseSceneFile(bool cancel) 
        {
            try
            {
                if (mZip != null && !cancel)
                {
                    string xmlname = "manifest.xml";
                    //remove the old manifest entry
                    mZip.RemoveEntry(xmlname);
                    //create a new memory stream to store the manifest file
                    MemoryStream manifeststream = new MemoryStream();
                    //store the modified manifest stream
                    ZipEntry manifestentry = new ZipEntry();
                    //save the XML document to memorystream
                    if (mManifest != null)
                    {
                        mManifest.Save(1, ref manifeststream);
                        manifeststream.Seek(0, SeekOrigin.Begin);
                        //save the memorystream for the xml metadata manifest into the zip file
                        mZip.AddEntry(xmlname, manifeststream);
                        //save the file
                        mZip.Save();
                    }
                }
                if (mZip != null) // could be null here...
                {
                    mZip.Dispose();
                }
                mZip = null;// set it back to null;
            }
            catch (Exception ex) 
            {
                DebugLogger.Instance().LogError(ex);
            }
        }

        public static SceneFile Instance() 
        {
            if (m_instance == null) 
            {
                m_instance = new SceneFile();
            }
            return m_instance;
        }
        /// <summary>
        /// This removes any previous exsitng models from the scene file
        /// and the manifest
        /// </summary>
        /// <param name="scenefilename"></param>
        public void RemoveExistingModels(string scenefilename) 
        {
            try
            {
                // open the zip file
                if (OpenSceneFile(scenefilename))
                {
                    //remove all *.stl files
                    XmlNode models = mManifest.FindSection(mManifest.m_toplevel, "Models");
                    if (models != null)
                    {
                        models.RemoveAll(); // remove all child nodes for this manifest entry
                        List<ZipEntry> etr = new List<ZipEntry>(); // entries to remove
                        foreach (ZipEntry ze in mZip) // create a list of entries to remove
                        {
                            if (ze.FileName.Contains(".stl"))
                            {
                                etr.Add(ze);
                            }
                        }
                        //and remove them
                        mZip.RemoveEntries(etr);
                    }
                    else
                    {
                        //slices does equal null, nothing to do...
                    }
                    CloseSceneFile(false);
                }
            }
            catch (Exception ex) 
            {
                DebugLogger.Instance().LogError(ex);

            }

        }
        // we might be able to do the slice add the same way as before,
        // trap for the slice events and add them
        public void RemoveExistingSlices(string scenefilename) 
        {
            try
            {
                // open the zip file
                if (OpenSceneFile(scenefilename))
                {
                    //remove all *.png files
                    //delete the slices node
                    XmlNode slices = mManifest.FindSection(mManifest.m_toplevel, "Slices");
                    if (slices != null)
                    {
                        slices.RemoveAll(); // remove all child nodes for this manifest entry
                        List<ZipEntry> etr = new List<ZipEntry>(); // entries to remove
                        foreach (ZipEntry ze in mZip) // create a list of entries to remove
                        {
                            if (ze.FileName.Contains(".png"))
                            {
                                etr.Add(ze);
                            }
                        }
                        //and remove them
                        mZip.RemoveEntries(etr);
                    }
                    else
                    {
                        //slices does equal null, nothing to do...
                    }
                    CloseSceneFile(false);
                }
            }
            catch (Exception ex)             
            {
                DebugLogger.Instance().LogError(ex);
            }
        }
        public bool RemoveExistingGCode(string scenefilename) 
        {
            try
            {
                // open the zip file
                if (OpenSceneFile(scenefilename))
                {
                    //remove all *.gcode files
                    //delete the Gcode node
                    XmlNode gcodenode = mManifest.FindSection(mManifest.m_toplevel, "GCode");
                    if (gcodenode != null)
                    {
                        gcodenode.RemoveAll(); // remove all child nodes for this manifest entry
                        List<ZipEntry> etr = new List<ZipEntry>(); // entries to remove
                        foreach (ZipEntry ze in mZip) // create a list of entries to remove
                        {
                            if (ze.FileName.Contains(".gcode"))
                            {
                                etr.Add(ze);
                            }
                        }
                        //and remove them
                        mZip.RemoveEntries(etr);
                    }
                    else
                    {
                        //slices does equal null, nothing to do...
                    }
                    CloseSceneFile(false);
                }

            }
            catch (Exception ex) 
            {
                DebugLogger.Instance().LogError(ex);
            }
            return false;
        }

        public void RemoveExistingSliceProfile(string scenefilename) 
        {
            try
            {
                // open the zip file
                if (OpenSceneFile(scenefilename))
                {
                    //remove all *.slicing files
                    //delete the SliceProfile nodes
                    XmlNode slicprofilenodes = mManifest.FindSection(mManifest.m_toplevel, "SliceProfile");
                    if (slicprofilenodes != null)
                    {
                        slicprofilenodes.RemoveAll(); // remove all child nodes for this manifest entry
                        List<ZipEntry> etr = new List<ZipEntry>(); // entries to remove
                        foreach (ZipEntry ze in mZip) // create a list of entries to remove
                        {
                            if (ze.FileName.Contains(".slicing"))
                            {
                                etr.Add(ze);
                            }
                        }
                        //and remove them
                        mZip.RemoveEntries(etr);
                    }
                    CloseSceneFile(false);
                }

            }
            catch (Exception ex)
            {
                DebugLogger.Instance().LogError(ex);
            }        
        }

        // add a slice to the cws / manifest file
        //add/replace gcode in a cws / manifest file
        public bool AddGCodeToFile(string scenefilename, MemoryStream ms, string gcname) 
        {
            try
            {
                if (OpenSceneFile(scenefilename))
                {
                    // store the slice file into the zip
                    mZip.AddEntry(gcname, ms);
                    // find the slices node in the top level
                    XmlNode gcodenode = mManifest.FindSection(mManifest.m_toplevel, "GCode");
                    if (gcodenode == null)  // no gcode node
                    {
                        //create one
                        gcodenode = mManifest.AddSection(mManifest.m_toplevel, "GCode");
                    }
                    //add the gcode file name into the manifest
                    mManifest.SetParameter(gcodenode, "name", gcname);
                    CloseSceneFile(false);
                    return true;
                }
            }
            catch (Exception ex) 
            {
                DebugLogger.Instance().LogError(ex);
            }
            return false;
        }
        public bool AddSliceProfileToFile(string scenefilename, MemoryStream ms, string sliceprofilename)
        {
            try
            {
                if (OpenSceneFile(scenefilename))
                {
                    // store the slice file into the zip
                    mZip.AddEntry(sliceprofilename, ms);
                    // find the slices node in the top level
                    XmlNode sliceprofilenode = mManifest.FindSection(mManifest.m_toplevel, "SliceProfile");
                    if (sliceprofilenode == null)  // no gcode node
                    {
                        //create one
                        sliceprofilenode = mManifest.AddSection(mManifest.m_toplevel, "SliceProfile");
                    }
                    //add the slice profile file name into the manifest
                    mManifest.SetParameter(sliceprofilenode, "name", sliceprofilename);
                    CloseSceneFile(false);
                    return true;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Instance().LogError(ex);
            }
            return false;
        }
        
        public GCodeFile LoadGCodeFromScene(string scenefilename) 
        {
            if (OpenSceneFile(scenefilename))
            {
                XmlNode gcn = mManifest.FindSection(mManifest.m_toplevel, "GCode");
                string gcodename = mManifest.GetString(gcn, "name", "none");
                if (!gcodename.Equals("none"))
                {
                    try
                    {
                        ZipEntry gcodeentry = mZip[gcodename];
                        MemoryStream gcstr = new MemoryStream();
                        gcodeentry.Extract(gcstr);
                        //rewind to beginning
                        gcstr.Seek(0, SeekOrigin.Begin);
                        GCodeFile gcf = new GCodeFile(gcstr);
                        return gcf;
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.Instance().LogError(ex);
                    }
                }
            }
            return null;
        }
        public bool LoadSliceProfileFromScene(string scenefilename)
        {
            if (OpenSceneFile(scenefilename))
            {
                XmlNode gcn = mManifest.FindSection(mManifest.m_toplevel, "SliceProfile");
                string sliceprofilename = mManifest.GetString(gcn, "name", "none");
                if (!sliceprofilename.Equals("none"))
                {
                    try
                    {
                        ZipEntry gcodeentry = mZip[sliceprofilename];
                        MemoryStream gcstr = new MemoryStream();
                        gcodeentry.Extract(gcstr);
                        //rewind to beginning
                        gcstr.Seek(0, SeekOrigin.Begin);
                        //GCodeFile gcf = new GCodeFile(gcstr);
                        UVDLPApp.Instance().m_buildparms = new SliceBuildConfig();
                        UVDLPApp.Instance().m_buildparms.Load(gcstr, sliceprofilename);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.Instance().LogError(ex);
                    }
                }
            }
            return false;
        }
        
        public bool Load(string scenefilename) 
        {
            try
            {
                UVDLPApp.Instance().SceneFileName = scenefilename;

                mZip = ZipFile.Read(scenefilename);
                string xmlname = "manifest.xml";
                OpenSceneFile(scenefilename);

                //examine manifest
                //find the node with models
                XmlNode topnode = mManifest.m_toplevel;

                XmlNode models = mManifest.FindSection(topnode, "Models");
                List<XmlNode> modelnodes = mManifest.FindAllChildElement(models, "model");
               // bool supportLoaded = false;
                foreach (XmlNode nd in modelnodes) 
                {
                    string name = mManifest.GetString(nd, "name", "noname");
                    string modstlname = name + ".stl";
                    int tag = mManifest.GetInt(nd, "tag", 0);
                    ZipEntry modelentry = mZip[modstlname]; // the model name will have the _XXXX on the end with the stl extension
                    MemoryStream modstr = new MemoryStream();
                    modelentry.Extract(modstr);
                    //rewind to beginning
                    modstr.Seek(0, SeekOrigin.Begin);
                    //fix the name
                    name = name.Substring(0, name.Length - 5);// get rid of the _XXXX at the end
                    string parentName = mManifest.GetString(nd, "parent", "noname");
                    Object3d obj, tmpObj;
                    switch (tag)
                    {
                        case Object3d.OBJ_SUPPORT:
                        case Object3d.OBJ_SUPPORT_BASE:
                            if (tag == Object3d.OBJ_SUPPORT)
                                obj = (Object3d)(new Support());
                            else
                                obj = (Object3d)(new SupportBase());
                            //load the model
                            obj.LoadSTL_Binary(modstr, name);
                            //add to the 3d engine
                            UVDLPApp.Instance().m_engine3d.AddObject(obj);
                            //set the tag
                            obj.tag = tag;
                            obj.SetColor(System.Drawing.Color.Yellow);
                            //find and set the parent
                            tmpObj = UVDLPApp.Instance().m_engine3d.Find(parentName);
                            if (tmpObj != null)
                            {
                                tmpObj.AddSupport(obj);
                            }
                            //supportLoaded = true;
                            break;

                        default:
                            //load as normal object
                            obj = new Object3d();
                            //load the model
                            obj.LoadSTL_Binary((MemoryStream)modstr, name);
                            //add to the 3d engine
                            UVDLPApp.Instance().m_engine3d.AddObject(obj);
                            //set the tag
                            obj.tag = tag;
                            break;
                    }
                }
                CloseSceneFile(true);
                UVDLPApp.Instance().RaiseAppEvent(eAppEvent.eModelAdded, "Scene loaded");
                return true;
            }
            catch (Exception ex) 
            {
                DebugLogger.Instance().LogError(ex);
                return false;
            }
        }


        /// <summary>
        /// Save the entire scene into a zip file with a manifest
        /// This file will later be re-used to store png slicee, gcode & svg
        /// </summary>
        /// <param name="scenename"></param>
        /// <returns></returns>
        public bool Save(string scenefilename)
        {
            try
            {
                // get the scene name
                UVDLPApp.Instance().SceneFileName = scenefilename;
                MemoryStream manifeststream = new MemoryStream(); ;
                string xmlname = "manifest.xml";
                //check to see if the file already exists
                if (File.Exists(scenefilename))
                {

                    RemoveExistingModels(scenefilename); // opens file, removes models out of file and manifest, and closes
                    // open the existing file and open up the manifest,
                    if (!OpenSceneFile(scenefilename)) 
                    {
                        DebugLogger.Instance().LogError("Could not open existing scene file for update");
                        return false;
                    }
                }
                else 
                {                    
                    mManifest = new XmlHelper();
                    mManifest.StartNew("", "manifest");                    
                    mZip = new ZipFile();
                    
                }

                XmlNode mc = null;
                //find or create
                mc = mManifest.FindSection(mManifest.m_toplevel, "Models");
                if(mc == null)
                    mc = mManifest.AddSection(mManifest.m_toplevel, "Models");

                //we need to make sure that only unique names are put in the zipentry
                // cloned objects yield the same name
                List<string> m_uniquenames = new List<string>();
                // we can adda 4-5 digit code to the end here to make sure names are unique
                int id = 0;
                string idstr;
                foreach (Object3d obj in UVDLPApp.Instance().m_engine3d.m_objects)
                {
                    //create a unique id to post-fix item names
                    id++;
                    idstr = string.Format("{0:0000}", id);
                    idstr = "_" + idstr;
                    //create a new memory stream
                    MemoryStream ms = new MemoryStream();
                    //save the object to the memory stream
                    obj.SaveSTL_Binary(ref ms);
                    //rewind the stream to the beginning
                    ms.Seek(0, SeekOrigin.Begin);
                    //get the file name with no extension
                    string objname = Path.GetFileNameWithoutExtension(obj.Name);
                    //spaces cause this to blow up too
                    objname = objname.Replace(' ', '_');
                    // add a value to the end of the string to make sure it's a unique name
                    objname = objname + idstr;
                    string objnameNE = objname;
                    objname += ".stl";  // stl file

                    mZip.AddEntry(objname, ms);
                    //create an entry for this object, using the object name with no extension
                    //save anything special about it

                    //XmlNode objnode = manifest.AddSection(mc, objnameNE);
                    XmlNode objnode = mManifest.AddSection(mc, "model");
                    mManifest.SetParameter(objnode, "name", objnameNE);
                    mManifest.SetParameter(objnode, "tag", obj.tag);
                    if (obj.tag != Object3d.OBJ_NORMAL && obj.m_parrent != null)
                    {
                        // note it's parent name in the entry
                        mManifest.SetParameter(objnode, "parent", Path.GetFileNameWithoutExtension(obj.m_parrent.Name));
                    }
                }
                //save the gcode

                //save the XML document to memorystream
                mManifest.Save(1, ref manifeststream);
                manifeststream.Seek(0, SeekOrigin.Begin);
                //remove the old one if present
                if (mZip[xmlname] != null) 
                {
                    mZip.RemoveEntry(xmlname);
                }
                //save the memorystream for the xml metadata manifest into the zip file
                mZip.AddEntry(xmlname, manifeststream);
                //save the zip file
                mZip.Save(scenefilename);
                mZip.Dispose();
                mZip = null; 
                return true;
            }
            catch (Exception ex)
            {
                DebugLogger.Instance().LogError(ex);
            }
            return false;
        }

        /// <summary>
        /// Save the entire scene into a zip file with a manifest
        /// This file will later be re-used to store png slicee, gcode & svg
        /// </summary>
        /// <param name="scenename"></param>
        /// <returns></returns>
        public bool SaveOld(string scenefilename) 
        {
            try
            {
                UVDLPApp.Instance().SceneFileName = scenefilename;
                // open a zip file with the scenename
                // iterate through all objects in engine
                string xmlname = "manifest.xml";
                XmlHelper manifest = new XmlHelper();
                //start the doc with no filename, becasue we're saving to a memory stream
                manifest.StartNew("", "manifest");
                //start a new stream to store the manifest file
                MemoryStream manifeststream = new MemoryStream();
                //create a new zip file
                ZipFile zip = new ZipFile();
                //get the top-level node in the manifest
                //XmlNode mc = manifest.m_toplevel;

                // Add in a section for GCode if present
                XmlNode gcn = manifest.AddSection(manifest.m_toplevel, "GCode");
                if (UVDLPApp.Instance().m_gcode != null)
                {
                    //create the name of the gcode file
                    String GCodeFileName = Path.GetFileNameWithoutExtension(scenefilename) + ".gcode";
                    manifest.SetParameter(gcn, "filename", GCodeFileName);
                    Stream gcs = new MemoryStream();
                    //save to memory stream
                    UVDLPApp.Instance().m_gcode.Save(gcs);
                    //rewind
                    gcs.Seek(0, SeekOrigin.Begin);
                    //create new zip entry   
                    zip.AddEntry(GCodeFileName, gcs);
                }
                XmlNode mc = manifest.AddSection(manifest.m_toplevel, "Models");
                //we need to make sure that only unique names are put in the zipentry
                // cloned objects yield the same name
                List<string> m_uniquenames = new List<string>();
                // we can adda 4-5 digit code to the end here to make sure names are unique
                int id = 0;
                string idstr;
                foreach (Object3d obj in UVDLPApp.Instance().m_engine3d.m_objects)
                {
                    //create a unique id to post-fix item names
                    id++;
                    idstr = string.Format("{0:0000}", id);
                    idstr = "_" + idstr;
                    //create a new memory stream
                    MemoryStream ms = new MemoryStream();
                    //save the object to the memory stream
                    obj.SaveSTL_Binary(ref ms);
                    //rewind the stream to the beginning
                    ms.Seek(0, SeekOrigin.Begin);
                    //get the file name with no extension
                    string objname = Path.GetFileNameWithoutExtension(obj.Name);
                    //spaces cause this to blow up too
                    objname = objname.Replace(' ', '_');
                    // add a value to the end of the string to make sure it's a unique name
                    objname = objname + idstr;
                    string objnameNE = objname;
                    objname += ".stl";  // stl file

                    zip.AddEntry(objname, ms);
                    //create an entry for this object, using the object name with no extension
                    //save anything special about it

                    //XmlNode objnode = manifest.AddSection(mc, objnameNE);
                    XmlNode objnode = manifest.AddSection(mc, "model");
                    manifest.SetParameter(objnode, "name", objnameNE);
                    manifest.SetParameter(objnode, "tag", obj.tag);
                    if (obj.tag != Object3d.OBJ_NORMAL && obj.m_parrent != null) 
                    {
                        // note it's parent name in the entry
                        manifest.SetParameter(objnode, "parent", Path.GetFileNameWithoutExtension(obj.m_parrent.Name));
                    }
                }
                //save the gcode

                //save the XML document to memorystream
                manifest.Save(1, ref manifeststream);
                manifeststream.Seek(0, SeekOrigin.Begin);
                //manifeststream.
                //save the memorystream for the xml metadata manifest into the zip file
                zip.AddEntry(xmlname, manifeststream);

                //save the zip file
                zip.Save(scenefilename);
                return true;
            }
            catch (Exception ex) 
            {
                DebugLogger.Instance().LogError(ex);
            }
            return false;
        }

    }
}
