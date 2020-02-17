using System;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using JinYiHelp.DM;
using JinYiHelp.API;
using JinYiHelp.IniHelp;
using System.IO;
using System.Threading;
using System.Data.SQLite;
using LuaInterface;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TricksMaster;

namespace TricksSpeedMaster
{
    public partial class Form3 : Form
    {
        KeyboardHookLib _keyboardHook;
        public string apppath = Application.StartupPath;
        private SQLiteConnection sQLiteConnection = new SQLiteConnection("data source=" + Application.StartupPath + "/data.sqlite");
        private Lua lua = new Lua();
        private bool Interrupted;
        private bool Stop;
        private CDmSoft dm = new CDmSoft();
        public string ScriptId = "";
        public string ScriptName = "";
        private string HeroID="";
        public string ScriptContent ="";
        public string HeroName { get; private set; }
        private SynchronizationContext _syncContext = null;
        private bool AutoHero;
        private bool isHook=false;

        private void LabHandText(object msg)
        {
            if (richTextBox1.Lines.Length > 1000)
            {
                richTextBox1.ResetText();
            }
            richTextBox1.AppendText(now()+" "+ msg.ToString() + "\r\n");
        }

        public Form3()
        {
            InitializeComponent();
            _syncContext = SynchronizationContext.Current;
        }

        private void Form3_FormClosing(object sender, FormClosingEventArgs e)
        {
            HotKeyUnInit();
            Interrupted = false;
            Stop = true;
            Application.Exit();
        }
        private void readfromsqlite(string role = "", string sql = "")
        {
            if (sql == "") {
                switch (role)
                {
                    case "战士":
                        sql = "select * from hero where roles like 'fighter'";
                        break;
                    case "法师":
                        sql = "select * from hero where roles like 'mage'";
                        break;
                    case "刺客":
                        sql = "select * from hero where roles like 'assassin'";
                        break;
                    case "坦克":
                        sql = "select * from hero where roles like 'tank'";
                        break;
                    case "射手":
                        sql = "select * from hero where roles like 'marksman'";
                        break;
                    case "辅助":
                        sql = "select * from hero where roles like 'support'";
                        break;
                    default:
                        sql = "select * from hero";
                        break;
                }
            }
            listView1.Items.Clear();
            imageList1.Images.Clear();
            listView1.LargeImageList = null;
            SQLiteCommand sQLiteCommand = new SQLiteCommand();
            SQLiteDataReader sQLiteDataReader = runsql(sql, sQLiteCommand, false);
            //SQLiteDataAdapter sQLiteDataAdapter = new SQLiteDataAdapter(sQLiteCommand);
            //DataTable dataTable = new DataTable();
            //sQLiteDataAdapter.Fill(dataTable);
            //foreach (DataRow r in dataTable.Rows)
            //{
            //    //Console.WriteLine($"{r["id"]},{r["name"]},{r["title"]},{r["alias"]},{r["heroid"]}");
            //    string path = apppath + "\\images\\" + r["alias"].ToString() + ".png";
            //    //Console.WriteLine(path);
            //    imageList1.Images.Add(r["title"].ToString(), Image.FromFile(path));
            //    ListViewItem listView = listView1.Items.Add(r["title"].ToString());
            //    listView.Name = r["name"].ToString();
            //    listView.ToolTipText = r["alias"].ToString();
            //    listView.Tag = r["heroid"].ToString();
            //}
            try
            {
                while (sQLiteDataReader.Read())
                {
                    //for (int i = 0; i < sQLiteDataReader.FieldCount; i++)
                    //{
                    //    Console.WriteLine($"{i},{sQLiteDataReader[i].ToString()}");

                    //}
                    string path = apppath + "\\images\\" + sQLiteDataReader[3].ToString() + ".png";
                    imageList1.Images.Add(sQLiteDataReader[2].ToString(), Image.FromFile(path));
                    ListViewItem listView = listView1.Items.Add(sQLiteDataReader[2].ToString());
                    listView.Name = sQLiteDataReader[1].ToString();
                    listView.ToolTipText = sQLiteDataReader[3].ToString();
                    listView.Tag = sQLiteDataReader[4].ToString();
                }
                sQLiteDataReader.Close();
                listView1.LargeImageList = imageList1;
                for (int i = 0; i < imageList1.Images.Count; i++)
                {
                    listView1.Items[i].ImageIndex = i;
                    // Console.WriteLine(listView1.Items[i].Text);
                    //Console.WriteLine(listView1.Items[i].Name);
                    //Console.WriteLine(listView1.Items[i].ToolTipText);
                    //Console.WriteLine(listView1.Items[i].Tag);
                }
                sQLiteConnection.Close();
                listView1.Refresh();
                //dataTable = null;
                //sQLiteDataAdapter = null;
                sQLiteCommand = null;
                sQLiteDataReader = null;
                //Console.WriteLine(imageList1.Images.Count);

            }

            catch (System.Exception e)
            {
                msg(e.Message);
            }

        }
        private void Form3_Load(object sender, EventArgs e)
        {
            dm.SetShowErrorMsg(0);
            //物品//https://game.gtimg.cn/images/lol/act/img/js/items/items.js
            readfromsqlite();
            LuaEnvInit();
            _keyboardHook = new KeyboardHookLib();
            _keyboardHook.InstallHook(this.OnKeyDown);
            Directory.CreateDirectory(apppath + "\\Script");
            //HookHeper.RegisterHotKey(this.Handle,100,HookHeper.KeyModifiers.Alt,Keys.D1);
            //HookHeper.RegisterHotKey(this.Handle, 101, HookHeper.KeyModifiers.Alt, Keys.D2);
        }
        /// <summary>
        /// 客户端键盘捕捉事件.
        /// </summary>
        /// <param name="hookStruct">由Hook程序发送的按键信息</param>
        /// <param name="handle">是否拦截</param>
        public void OnKeyDown(KeyboardHookLib.HookStruct hookStruct, out bool handle)
        {
            //是否拦截这个键
            handle = false;
            Keys key = (Keys)hookStruct.vkCode;
            int scanCode = hookStruct.scanCode;
            int dwExtraInfo= hookStruct.dwExtraInfo;
            if (key == Keys.F8)
            {
                isHook = !isHook;
            }
            else if (key == Keys.D1)
            {
                StartClick();
            }
            else if (key == Keys.D2)
            {
                StopClick();
            }
            //else
            //{
            //    Console.WriteLine(string.Format("{0}  HOOK按下了：{1} ,scanCode:{2} ,dwExtraInfo:{3}", DateTime.Now, key, scanCode, dwExtraInfo));
            //}
        }

        private void HotKeyUnInit()
        {
            if (_keyboardHook != null) _keyboardHook.UninstallHook();
        }

        public string UTF8ToGB2312(string str)

        {
            try
            {
                Encoding utf8 = Encoding.UTF8;
                Encoding gb2312 = Encoding.GetEncoding("GB2312");//Encoding.Default ,936
                byte[] temp = utf8.GetBytes(str);
                byte[] temp1 = Encoding.Convert(utf8, gb2312, temp);
                string result = gb2312.GetString(temp1);
                return result;
            }
            catch (Exception ex)//(UnsupportedEncodingException ex)
            {
                MessageBox.Show(ex.ToString());
                return null;
            }
        }
        public string GB2312ToUTF8(string str)
        {
            try
            {
                Encoding uft8 = Encoding.GetEncoding(65001);
                Encoding gb2312 = Encoding.GetEncoding("gb2312");
                byte[] temp = gb2312.GetBytes(str);
                MessageBox.Show("gb2312的编码的字节个数：" + temp.Length);
                for (int i = 0; i < temp.Length; i++)
                {
                    MessageBox.Show(Convert.ToUInt16(temp[i]).ToString());
                }
                byte[] temp1 = Encoding.Convert(gb2312, uft8, temp);
                MessageBox.Show("uft8的编码的字节个数：" + temp1.Length);
                for (int i = 0; i < temp1.Length; i++)
                {
                    MessageBox.Show(Convert.ToUInt16(temp1[i]).ToString());
                }
                string result = uft8.GetString(temp1);
                return result;
            }
            catch (Exception ex)//(UnsupportedEncodingException ex)
            {
                MessageBox.Show(ex.ToString());
                return null;
            }
        }
        private void LuaRegFun(string str,string Fun,string orgFun="", bool iswrite = false)
        {
            try
            {
                if (orgFun == "")
                {
                    orgFun = Fun;
                }
                lua.RegisterFunction(Fun.ToLower(), this, this.GetType().GetMethod(orgFun));
                if (iswrite)
                {
                    File.AppendAllText(apppath + "/脚本命令使用说明.txt", Fun + ":" + str + "\r\n");
                }
                
                //Console.WriteLine(Fun+":"+str);
            }
            catch (System.Exception e)
            {
                Console.WriteLine(e.Message);
                msg(e.Message);
            }
        }
        private void LuaEnvInit()
        {
            try
            {
                bool isshowmsg = (!File.Exists(apppath + "/脚本命令使用说明.txt"));
                LuaRegFun("弹出对话框(内容)","msg","", isshowmsg);
                LuaRegFun("延时(毫秒)","sleep", "", isshowmsg);
                LuaRegFun("鼠标左键点击","LeftClick", "", isshowmsg);
                LuaRegFun("鼠标右键点击", "RightClick", "", isshowmsg);
                LuaRegFun("鼠标左键双击", "LeftDoubleClick", "", isshowmsg);
                LuaRegFun("鼠标移动到坐标(x,y)","MoveTo", "", isshowmsg);
                LuaRegFun("键盘按键(文本型 键名)","KeyPressChar", "", isshowmsg);
                LuaRegFun("鼠标左键按下","LeftDown", "", isshowmsg);
                LuaRegFun("鼠标左键弹起", "LeftUp", "", isshowmsg);
                LuaRegFun("鼠标相对移动(x,y)", "MoveR", "", isshowmsg);
                LuaRegFun("键盘按下(键码)","KeyDown", "dmKeyDown", isshowmsg);
                LuaRegFun("键盘弹起(键码)", "KeyUp", "dmKeyUp", isshowmsg);
                LuaRegFun("键盘按下(键名)","KeyDownChar", "", isshowmsg);
                LuaRegFun("键盘弹起(键名)","KeyUpChar", "", isshowmsg);
                LuaRegFun("键盘按键(键码)","KeyPress", "dmKeyPress", isshowmsg);
                LuaRegFun("查找图片(x1,y1,x2,y2,图片名,相似度)", "FindPic", "", isshowmsg);
                LuaRegFun("设置大漠路径(路径)","SetPath", "", isshowmsg);
                LuaRegFun("结束程序(进程名)","StopAPP", "", isshowmsg);
                LuaRegFun("取现行时间","now", "", isshowmsg);
                LuaRegFun("日志输出(内容)","printf", "", isshowmsg);
                LuaRegFun("停止脚本","EndScript", "", isshowmsg);
                LuaRegFun("绑定窗口","BindWindow", "", isshowmsg);
                LuaRegFun("取消绑定窗口","UnBindWindow", "", isshowmsg);
                LuaRegFun("查找窗口","FindWindow", "", isshowmsg);
                LuaRegFun("取当前选择英雄名","getheroname", "", isshowmsg);
                LuaRegFun("是否结束脚本","isend", "", isshowmsg);
                LuaRegFun("设置剪切板(文本)","SetClipboard", "", isshowmsg);
                LuaRegFun("A人","AR", "", isshowmsg);
                LuaRegFun("A兵","AB", "", isshowmsg);
                if (isshowmsg)
                {
                    Process.Start("notepad.exe", apppath + "/脚本命令使用说明.txt") ;
                }
            }
            catch (Exception e)
            {
                throw new NotImplementedException(e.Message);
            }
        }
        public void SetClipboard(string str)
        {
           dm.SetClipboard(str);
        }
        public string getheroname()
        {
            dm.SetClipboard(HeroName);
            return HeroName;
        }
        public bool isend()
        {
            return (Stop == true);
        }
        public string now()
        {
            return System.DateTime.Now.ToString();
        }
        public void printf(object msg)
        {
            try
            {
                if (null != msg)
                {
                    ThreadPool.UnsafeQueueUserWorkItem(new WaitCallback((object s) =>
                    {
                        _syncContext.Post(LabHandText, msg.ToString());
                    }), null);
                    Console.WriteLine(msg.ToString());
                }
            }
            catch (Exception e)
            {   
                throw new NotImplementedException(e.Message);
            }
        }
        public bool AR()
        {
            object x=-1, y=-1;
            int dmret = dm.FindPic(0, 0, 2000, 2000, "xue.bmp|xue1.bmp","1010140",0.9,0,out x,out y);
            if (dmret > -1)
            {
                dm.MoveTo(Convert.ToInt32(x)+50, Convert.ToInt32(y)+100);
                dm.RightClick();
                return true;
            }
            return false;
        }
        public void AB()
        {
            object x = -1, y = -1;
            int dmret = dm.FindPic(0, 0, 2000, 2000, "bin.bmp|bin2.bmp", "1010140", 0.9, 0, out x, out y);
            if (dmret > -1)
            {
                dm.MoveTo(Convert.ToInt32(x) + 50, Convert.ToInt32(y) + 50);
                dm.RightClick();
            }
        }
        public int FindWindow(string class_name,string title_name)
        {
            return dm.FindWindow(class_name, title_name);
        }
        public int UnBindWindow()
        {
            return dm.UnBindWindow();
        }
        public int BindWindow(int hwnd,string display,string mouse, string keypad, int mode)
        {
            return dm.BindWindow(hwnd, display, mouse, keypad, mode);
        }
        public void StopAPP(string app)
        {
            Process.Start("taskkill.exe","/f /im "+app);
        }
        public void SetPath(string path=null)
        {
            if (path == null)
            {
                path= Application.StartupPath + "\\pic";
            }
            dm.SetPath(path);
        }
        public void EndScript()
        {
            Stop = true;
            Interrupted = false;
        }
        public string FindPic(int x1,int y1,int x2,int y2,string pic_name,double sim)
        {
            object intX=-1, intY=-1;
            int dmret= dm.FindPic(x1, y1, x2, y2, pic_name, "101010", sim, 0, out intX, out intY);
            if (dmret > -1)
            {
                Console.WriteLine(string.Format("找到图图片:{0},返回值:{1},返回坐标:{2},{3}", pic_name, dmret, intX, intY));
            }
            string result= string.Format(" {0} , {1} , {2} ",dmret,intX,intY);
            return result;
        }
        public void dmKeyPress(int key)
        {
            dm.KeyPress(key);
        }
        public void KeyUpChar(string key)
        {
            dm.KeyUpChar(key);
        }
        public void KeyDownChar(string key)
        {
            dm.KeyDownChar(key);
        }
        public void dmKeyUp(int key)
        {
            dm.KeyUp(key);
        }
        public void dmKeyDown(int key)
        {
            dm.KeyDown(key);
        }
        public void MoveR(int x,int y)
        {
            dm.MoveR(x,y);
        }
        public void LeftUp()
        {
            dm.LeftUp();
        }
        public void LeftDown()
        {
            dm.LeftDown();
        }
        public void KeyPressChar(string key)
        {
            dm.KeyPressChar(key);
        }
        public void MoveTo(int x,int y)
        {
            dm.MoveTo(x,y);
        }
        public void LeftDoubleClick()
        {
            dm.LeftDoubleClick();
        }
        public void LeftClick()
        {
            dm.LeftClick();
        }
        public void RightClick()
        {
            dm.RightClick();
        }
        public void sleep(int t)
        {
            Thread.Sleep(t);
        }
        public void msg(string msg, string caption = "提示", MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.Information, MessageBoxDefaultButton messageBoxDefaultButton = MessageBoxDefaultButton.Button1)
        {
            MessageBox.Show(msg, caption, buttons, icon, messageBoxDefaultButton);
        }
        private SQLiteDataReader runsql( string sql, SQLiteCommand sQLiteCommand = null, bool autoclose = true)
        {
            SQLiteDataReader sQLiteDataReader=null;
            try
            {
                if (sQLiteConnection.State != ConnectionState.Open)
                {
                    sQLiteConnection.Open();
                }
                if (sQLiteCommand == null)
                {
                    sQLiteCommand = new SQLiteCommand();
                    
                }
                if (sQLiteCommand.Connection==null)
                {
                    sQLiteCommand.Connection = sQLiteConnection;
                }
                sQLiteCommand.CommandText = sql;
                sQLiteDataReader = sQLiteCommand.ExecuteReader();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                msg("执行SQL语句(" + sql + ")出错:" + e.Message);
            }
            if (autoclose)
            {
                sQLiteConnection.Close();
            }
            return sQLiteDataReader;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            readfromsqlite();
        }
        private void updatetricklist(string heroid)
        {
            listView2.Items.Clear();
            SQLiteCommand sQLiteCommand = new SQLiteCommand();
            SQLiteDataReader sQLiteDataReader = runsql("select * from tricks where heroid='" + heroid+ "' or heroid='通用'", sQLiteCommand, false);
            try
            {
                while (sQLiteDataReader.Read())
                {
                    //for (int i = 0; i < sQLiteDataReader.FieldCount; i++)
                    //{
                    //    Console.WriteLine($"{i},{sQLiteDataReader[i].ToString()}");
                    //}
                    ListViewItem listView = listView2.Items.Add("");
                    listView.Text = listView2.Items.Count.ToString();
                    listView.SubItems.Add(sQLiteDataReader[1].ToString());
                    listView.SubItems.Add(sQLiteDataReader[2].ToString());
                    listView.SubItems.Add(sQLiteDataReader[3].ToString());
                    listView.SubItems.Add(sQLiteDataReader[4].ToString());
                    listView.SubItems.Add(sQLiteDataReader[6].ToString());
                }
                sQLiteDataReader.Close();
                sQLiteConnection.Close();
                listView2.Refresh();
                sQLiteCommand = null;
                sQLiteDataReader = null;
            }
            catch (System.Exception err)
            {
                msg(err.Message);
            }
        }
        private void listView1_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            if (e.IsSelected == true)
            {
                int index = e.ItemIndex;
                HeroName = listView1.Items[index].Text;
                HeroID = listView1.Items[index].Tag.ToString();
                Console.WriteLine(string.Format("索引:{0},英雄名:{1},称号:{2},英文名:{3},英雄ID:{4}", index, listView1.Items[index].Text, listView1.Items[index].Name, listView1.Items[index].ToolTipText, listView1.Items[index].Tag));
                updatetricklist(listView1.Items[index].Tag.ToString());
            }
            else {
                listView2.Items.Clear();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            readfromsqlite("战士");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            readfromsqlite("法师");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            readfromsqlite("刺客");
        }

        private void button5_Click(object sender, EventArgs e)
        {
            readfromsqlite("射手");
        }

        private void button6_Click(object sender, EventArgs e)
        {
            readfromsqlite("辅助");
        }
        private void listView2_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            int index = e.Item.Index;
            if (listView2.Items[index].Selected)
            {
                ScriptId = listView2.Items[index].SubItems[2].Text;
                ScriptName= listView2.Items[index].SubItems[3].Text;
                ScriptContent = listView2.Items[index].SubItems[4].Text;
                Console.WriteLine(string.Format("索引:{0},脚本ID:{1},脚本名称:{2},脚本描述:{3}", index, ScriptId,ScriptName,ScriptContent));
            }
        }
        private void button7_Click(object sender, EventArgs e)
        {
            searchhero();
        }
        private void searchhero()
        {
            string str = textBox_yxName.Text;
            if (str == "")
            {
                msg("请输入要搜索的关键字");
            }
            else
            {
                readfromsqlite("", "select * from hero where name like '%" + str + "%' or title like '%" + str + "%' or alias like '%" + str + "%'");
            }
        }
        private void listView2_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listView2.SelectedItems.Count > 0)
            {
                int index = listView2.SelectedItems[0].Index;
                EditScript(listView2.SelectedItems[0].SubItems[2].Text);
                updatetricklist(HeroID);
            }

        }

        private void EditScript(string scriptId)
        {
            Form4 form = new Form4();
            form.ScriptID = scriptId;
            form.ScriptName = ScriptName;
            form.ScriptContent = ScriptContent;
            form.ShowDialog();

            IniHelper ini = new IniHelper(Application.StartupPath + "\\data\\setsoft.ini");
            ScriptContent = ini.ReadValue(scriptId, "ScriptContent");
            ScriptName = ini.ReadValue(scriptId, "ScriptName");
            runsql("UPDATE 'main'.'tricks' SET 'content'='"+ ScriptContent + "',name='"+ ScriptName +"' WHERE strickid = '" + scriptId + "'",null,true);
        }
        private void listView2_MouseUp(object sender, MouseEventArgs e)
        {
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            contextMenu.Show();
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count > 0)
            {
                int index = listView2.SelectedItems[0].Index;
                EditScript(listView2.SelectedItems[0].SubItems[2].Text);
                updatetricklist(HeroID);
            }
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            sQLiteConnection.Close();
            string ScriptID_Pre = "";
            string ID = "";
            string sql = "";
            int index=-1;
            if (listView1.SelectedItems.Count > 0)
            {
                //指定英雄
                index = listView1.SelectedItems[0].Index;
                HeroID = listView1.Items[index].Tag.ToString();
                ScriptID_Pre = "H";
            }
            else
            {
                //通用
                HeroID = "通用";
            }
            sql = "select * from tricks where heroid='" + HeroID + "'";
            SQLiteCommand sQLiteCommand = new SQLiteCommand();
            SQLiteDataReader sQLiteDataReader = runsql(sql, sQLiteCommand, false);
            try
            {
                ID = (sQLiteDataReader.StepCount + 1).ToString().PadLeft(3,'0');
                ScriptId = ScriptID_Pre + HeroID.PadLeft(3,'0') + ID;
                runsql("INSERT INTO 'main'.'tricks' ( 'heroid', 'strickid', 'name', 'content', 'Author', 'updatetime') VALUES ('"+ HeroID + "', '"+ScriptId+"', '', '', '', '"+System.DateTime.Now.ToString()+"')");
                EditScript(ScriptId);
                updatetricklist(HeroID);
                sQLiteConnection.Close();

            }
            catch (System.Exception err)
            {
                Console.WriteLine(err.Message);
                msg(err.Message);
            }
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count > 0)
            {

                int index = listView2.SelectedItems[0].Index;
                ScriptId = listView2.SelectedItems[0].SubItems[2].Text;
                try
                {
                    runsql("DELETE FROM 'main'.'tricks' WHERE strickid = '" + ScriptId + "'");
                    updatetricklist(HeroID);
                    File.Delete(apppath+ "\\Script\\"+ScriptId+".lua");
                }
                catch (Exception err)
                {
                    Console.WriteLine(err.Message);
                }

            }
        }
        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            listView2.Items.Clear();
            runsql("DELETE FROM 'main'.'tricks'");
            try
            {
                string[] files = Directory.GetFiles(apppath + "\\Script", "*.*");
                foreach (string sFile in files)
                {
                    Console.WriteLine(string.Format("删除文件:{0}", sFile));
                    File.Delete(sFile);
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }
            
        }
        private void StartClick()
        {
            if (isHook)
            {
                return;
            }
            AutoHero = checkBox_autohero.Checked;
            if (HeroName == "")
            {
                msg("对不起，你还未选中英雄!");
                return;
            }
            if (button8.Text == "开始")
            {
                button8.Text = "暂停";
                Interrupted = false;
                Stop = false;
                button9.Enabled = true;
                Thread thread = new Thread(MainThread);
                thread.Start();
            }
            else if (button8.Text == "暂停")
            {
                button8.Text = "继续";
                Interrupted = true;
                Stop = false;
            }
            else if (button8.Text == "继续")
            {
                button8.Text = "暂停";
                Interrupted = false;
                Stop = false;
            }
            else
            {
                button8.Text = "开始";
                Interrupted = false;
                Stop = false;
            }
            Console.WriteLine(string.Format("Stop:{0},Interrupted:{1}", Stop, Interrupted));
        }
        private void button8_Click(object sender, EventArgs e)
        {
            StartClick();
        }
        
        private void MainThread()
        {
            int hwndmain=0, hwndgame=0;
            while (Stop == false)
            {
                while (Interrupted)
                {
                    sleep(1000);
                }
                hwndmain = WinAPI.FindWindowHwnd("RCLIENT", "League of Legends");
                hwndgame = WinAPI.FindWindowHwnd("RiotWindowClass", "League of Legends (TM) Client");
                if (hwndmain > 0)
                {
                    if (hwndgame > 0)
                    {
                        DoFile(apppath + "\\Script\\" + ScriptId + ".lua");
                    }
                    else
                    {
                        if (AutoHero)
                        {
                            DoFile(apppath + "\\Script\\main.lua");
                        }
                    }
                }
                sleep(200);
            }
        }
        private void DoFile(string path = null)
        {
            if (path == null)
            {
                path = apppath + "\\Script\\main.lua";
            }
            if (File.Exists(path))
            {
                printf("执行脚本:"+path);
                try
                {
                    lua.DoFile(path);
                }
                catch (System.Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
        private void StopClick()
        {
            Interrupted = false;
            Stop = true;
            button8.Text = "开始";
            button9.Enabled = false;
        }
        private void button9_Click(object sender, EventArgs e)
        {
            StopClick();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (Stop)
            {
                if (button8.Text != "开始")
                {
                    button8.Text = "开始";
                    button8.Enabled = true;
                    button9.Enabled = false;
                    printf("脚本已停止");
                }
            }
        }

        private void textBox_yxName_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
            {
                searchhero();
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            readfromsqlite("坦克");
        }
    }
    // <summary>
    /// 键盘Hook管理类
    /// </summary>
    public class KeyboardHookLib
    {
        private const int WH_KEYBOARD_LL = 13; //键盘
                                               //键盘处理事件委托 ,当捕获键盘输入时调用定义该委托的方法.
        private const int WM_KEYUP = 0x101;     //按键抬起
        private const int WM_KEYDOWN = 0x100;       //按键按下
        private delegate int HookHandle(int nCode, int wParam, IntPtr lParam);
        //客户端键盘处理事件
        public delegate void ProcessKeyHandle(HookStruct param, out bool handle);
        //接收SetWindowsHookEx返回值
        private static int _hHookValue = 0;
        //勾子程序处理事件
        private HookHandle _KeyBoardHookProcedure;
        //Hook结构
        [StructLayout(LayoutKind.Sequential)]
        public class HookStruct
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }
        //设置钩子 
        [DllImport("user32.dll")]
        private static extern int SetWindowsHookEx(int idHook, HookHandle lpfn, IntPtr hInstance, int threadId);
        //取消钩子 
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern bool UnhookWindowsHookEx(int idHook);
        //调用下一个钩子 
        [DllImport("user32.dll")]
        private static extern int CallNextHookEx(int idHook, int nCode, int wParam, IntPtr lParam);
        //获取当前线程ID
        [DllImport("kernel32.dll")]
        private static extern int GetCurrentThreadId();
        //Gets the main module for the associated process.
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string name);
        private IntPtr _hookWindowPtr = IntPtr.Zero;
        //构造器
        public KeyboardHookLib() { }
        //外部调用的键盘处理事件
        private static ProcessKeyHandle _clientMethod = null;
        /// <summary>
        /// 安装勾子
        /// </summary>
        /// <param name="hookProcess">外部调用的键盘处理事件</param>
        public void InstallHook(ProcessKeyHandle clientMethod)
        {
            _clientMethod = clientMethod;
            // 安装键盘钩子 
            if (_hHookValue == 0)
            {
                _KeyBoardHookProcedure = new HookHandle(OnHookProc);
                _hookWindowPtr = GetModuleHandle(Process.GetCurrentProcess().MainModule.ModuleName);
                _hHookValue = SetWindowsHookEx(
                WH_KEYBOARD_LL,
                _KeyBoardHookProcedure,
                _hookWindowPtr,
                0);
                //如果设置钩子失败. 
                if (_hHookValue == 0) UninstallHook();
            }
        }
        //取消钩子事件 
        public void UninstallHook()
        {
            if (_hHookValue != 0)
            {
                bool ret = UnhookWindowsHookEx(_hHookValue);
                if (ret) _hHookValue = 0;
            }
        }
        //钩子事件内部调用,调用_clientMethod方法转发到客户端应用。
        private static int OnHookProc(int nCode, int wParam, IntPtr lParam)
        {
            //Console.WriteLine(string.Format("OnHookProc:{0},{1},{2}",nCode,wParam,lParam));
            if (nCode >= 0 && wParam==256)//256键盘按下//257键盘弹起
            {
                //转换结构
                HookStruct hookStruct = (HookStruct)Marshal.PtrToStructure(lParam, typeof(HookStruct));
                if (_clientMethod != null)
                {
                    bool handle = false;
                    //调用客户提供的事件处理程序。
                    _clientMethod(hookStruct, out handle);
                    if (handle) return 1; //1:表示拦截键盘,return 退出
                }
            }
            return CallNextHookEx(_hHookValue, nCode, wParam, lParam);
        }
    }
}
