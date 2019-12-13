using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Collections;
using System.Threading;
namespace UV_DLP_3D_Printer.Device_Interface.AutoDetect
{
    public class ConnectionTester
    {
        public string m_serialport;
        public int m_baud;
        public eConnTestStatus m_result; // results are store here
        private Thread m_thread;
        private bool m_running;
        public event ConnectionTesterStatus ConnectionTesterStatusEvent;
        public enum eConnTestStatus 
        {
            eOpenFailure, // port could not be opened
            eDeviceResponded, // device responded to the message we sent
            eNoResponse // no response or response not expected
        }
        public delegate void ConnectionTesterStatus(ConnectionTester obj,eConnTestStatus status); // report a final status

        public ConnectionTester(string port) 
        {
            m_serialport = port;
        }

        public void Start() 
        {
            m_thread = new Thread(new ThreadStart(run));
            m_thread.Start();
            m_running = true;
        }

        public void run() 
        {
            while (m_running) 
            {
                Thread.Sleep(0);
                // wait for response
            }
        }
    }
}
