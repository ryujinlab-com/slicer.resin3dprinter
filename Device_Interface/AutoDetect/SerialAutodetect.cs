using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO.Ports;
using System.Collections;

namespace UV_DLP_3D_Printer.Device_Interface.AutoDetect
{
    /// <summary>
    /// This is the Serial Auto Detect Class
    /// It's purpose is to identify which port(s) the printer/machine is connected with.
    /// It will list all the serial ports
    ///     - create a serial port detecter for each port
    ///     - run the serial detector on each port
    ///     - send one or more commands that will query the machine
    ///     - recieve a response (or not)
    ///     - report results
    /// </summary>
    public class SerialAutodetect
    {
        public class SerialAutodetectConfig 
        {
            public int m_baud;
        }
        public enum eDetectStatus
        {
            eStarted,
            eRunning,
            eCompleted
        }
        public delegate void DetectionStatus(eDetectStatus status);
        private static SerialAutodetect m_instance = null;
        private bool m_running;
        private Thread m_thread;
        private List<ConnectionTester> m_list; // list of connectionTester objects we're spawning
        private List<ConnectionTester> m_lstresults;
        public event DetectionStatus DetectionStatusEvent;
        private SerialAutodetectConfig m_config;
        private const long TIMEOUTTIME = 10000; // ten seconds total 
        private long m_starttime; // for timeout

        private SerialAutodetect() 
        {
                    
        }
        public bool Running 
        {
            get { return m_running; }
        }
        public static SerialAutodetect Instance() 
        {
            if (m_instance == null) 
            {
                m_instance = new SerialAutodetect();
            }
            return m_instance;
        }
        public void Start(SerialAutodetectConfig config) 
        {
            m_running = true;
            m_thread = new Thread(new ThreadStart(run));
            m_list = new List<ConnectionTester>();
            m_lstresults = new List<ConnectionTester>();
            m_thread.Start();
        }

        private void run() 
        {

            //get the list of serial ports
            foreach (String s in SerialPort.GetPortNames())
            {
                // create a new tester
                ConnectionTester tester = new ConnectionTester(s);
                //set the baud
                tester.m_baud = m_config.m_baud;
                //set up to listen to events
                tester.ConnectionTesterStatusEvent += new ConnectionTester.ConnectionTesterStatus(ConnectionTesterStatusDel);
                //start it off
                tester.Start();
            }
            //for each serial port, create a new serial port tester
            while (m_running) 
            {
                //check for timeout
                Thread.Sleep(0); 
            }
        }

        void ConnectionTesterStatusDel(ConnectionTester obj,ConnectionTester.eConnTestStatus status) 
        {
            //find obj in the list
            //mark it's results
            //check to see if we should return 
            m_lstresults.Add(obj);
            if (m_lstresults.Count == m_list.Count) 
            {
                // all reported, we're done
                m_running = false;
                if (DetectionStatusEvent != null) 
                {
                    DetectionStatusEvent(eDetectStatus.eCompleted);
                }
            }
        }
    }
}
