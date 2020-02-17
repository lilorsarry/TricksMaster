using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
namespace TricksSpeedMaster
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            string dir = Application.StartupPath;
            
            string path = dir + "/data.sqlite";
            if (!File.Exists(path)) {
                File.Create(path);
            }
            path = dir + "\\dm.dll";
            if (!File.Exists(path))
            {
                File.WriteAllBytes(path, TricksMaster.Properties.Resources.dm);
            }
            path = dir + "\\dmc.dll";
            if (!File.Exists(path))
            {
                File.WriteAllBytes(path, TricksMaster.Properties.Resources.dmc);
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
