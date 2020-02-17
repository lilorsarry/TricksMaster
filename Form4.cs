using System;
using System.Windows.Forms;
using System.IO;
using JinYiHelp.IniHelp;

namespace TricksSpeedMaster
{
    public partial class Form4 : Form
    {

        public Form4()
        {
            InitializeComponent();
        }

        public string ScriptID { get; internal set; }
        public string ScriptName { get; internal set; }
        public string ScriptContent { get; internal set; }
        private void Form4_Load(object sender, EventArgs e)
        {
            try
            {
                richTextBox2.Text = "脚本命令使用说明:\r\n" + File.ReadAllText(Application.StartupPath + "\\脚本命令使用说明.txt");
            }
            catch (Exception err) {
                Console.WriteLine(err.Message);
            }
            label1.Text = "脚本ID";
            label2.Text = "脚本名";
            label3.Text = "脚本描述";
            Console.WriteLine(ScriptID);
            textBox_Script_ID.Text = ScriptID;
            textBox_ScriptName.Text = ScriptName;
            textBox_Script_ID.Text = ScriptContent;
            ReSet();
        }
        private void Save()
        {
            try
            {
                if (!Directory.Exists(Application.StartupPath + "\\data"))
                {
                    Directory.CreateDirectory(Application.StartupPath + "\\data");
                }
                File.WriteAllText(Application.StartupPath + "\\Script\\" + ScriptID + ".lua", richTextBox1.Text);
                IniHelper ini = new IniHelper(Application.StartupPath + "\\data\\setsoft.ini");
                ScriptName = textBox_ScriptName.Text;
                ScriptContent = textBox_Script_ID.Text;
                ini.WriteValue(ScriptID, "ScriptContent", ScriptContent);
                ini.WriteValue(ScriptID, "ScriptName", ScriptName);
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }

        }
        private void ReSet()
        {
            if (!Directory.Exists(Application.StartupPath + "\\Script"))
            {
                Directory.CreateDirectory(Application.StartupPath + "\\Script");
            }
            try
            {
                textBox_Script_ID.Text = ScriptContent;
                textBox_ScriptName.Text = ScriptName;
                richTextBox1.Text = File.ReadAllText(Application.StartupPath + "\\Script\\" + ScriptID + ".lua");
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
        }
        private void button1_Click_1(object sender, EventArgs e)
        {
            ReSet();
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            Save();
            Dispose(false);
        }
    }
}
