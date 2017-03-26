using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Text;
using System.IO;

namespace SinoPrintForm
{
    static class Program
    {
      
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;
            //SerialPortHttpService.Instance().Start();
            Form1 form = new Form1();
            form.ShowInTaskbar = false;
            form.WindowState = FormWindowState.Minimized;
            //DataBaseHelper.DBInit();

            Application.Run(form);
          
            
        }
    }
}
