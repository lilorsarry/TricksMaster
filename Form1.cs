using System;
using System.Data;
using System.Text;
using System.Windows.Forms;
using JinYiHelp.EasyHTTPClient;
using System.IO;
using System.Threading;
using System.Web.Script.Serialization;
using System.Data.SQLite;
namespace TricksSpeedMaster
{
    public partial class Form1 : Form
    {
       
        private SynchronizationContext _syncContext = null;
        public string apppath = Application.StartupPath;
        private SQLiteConnection sQLiteConnection = new SQLiteConnection("data source=" + Application.StartupPath + "/data.sqlite");
        private SQLiteCommand sQLiteCommand = new SQLiteCommand();
        public Form1()
        {
            InitializeComponent();
            _syncContext = SynchronizationContext.Current;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Thread thread = new Thread(UpdateRoleInfo);
            thread.Start();

        }
        private void LabHandText(object msg) {
           int index= listBox1.Items.Add(msg.ToString());
            listBox1.SelectedIndex = index;
        }
        private void UpdateRoleInfo()
        {
            ThreadPool.UnsafeQueueUserWorkItem(new WaitCallback((object s) =>
            {
                _syncContext.Post(LabHandText, "检查本地资源");
            }), null);
            string info = "";
            string jsonpath = apppath + "\\herolist.json";
            if (!File.Exists(jsonpath)) {
                ThreadPool.UnsafeQueueUserWorkItem(new WaitCallback((object s) =>
                {
                    _syncContext.Post(LabHandText, "正在从服务器获取资源列表...");
                }), null);
                HttpItem item = new HttpItem()
                {
                    URL = "https://game.gtimg.cn/images/lol/act/img/js/heroList/hero_list.js",
                    Encoding = Encoding.UTF8
                };
                var result = item.GetHtml().Result;
                //Console.WriteLine(result.Cookie);
                if (result.IsSuccessStatusCode)
                {
                    info = result.Html;
                    //Console.WriteLine(info);
                    ThreadPool.UnsafeQueueUserWorkItem(new WaitCallback((object s) =>
                    {
                        _syncContext.Post(LabHandText, "获取资源列表成功");
                    }), null);
                }
                else
                {
                    msg(result.StatusDescription);
                    Console.WriteLine(result.StatusDescription);
                    return;
                }
            }
            if (info == "")
            {
                if (File.Exists(jsonpath))
                {
                    info = File.ReadAllText(jsonpath);
                }
            }
            if (info == "")
            {
                ThreadPool.UnsafeQueueUserWorkItem(new WaitCallback((object s) =>
                {
                    _syncContext.Post(LabHandText, "获取资源列表失败,请重启软件重试。");
                }), null);
            }
            else
            {
                JavaScriptSerializer js = new JavaScriptSerializer();//实例化一个能够序列化数据的类
                JsonParser jsonParser = js.Deserialize<JsonParser>(info); //将json数据转化为对象类型并赋值给list
                jsonHero[] jsonHeroes = jsonParser.hero;
                if (runsql("CREATE TABLE IF NOT EXISTS hero(id varchar(4),name string,title string,alias string,heroid string,roles string)"))
                {
                    Directory.CreateDirectory(apppath + "\\images");
                    foreach (jsonHero jsonHero in jsonHeroes)
                    {
                        //DELETE FROM hero WHERE heroid='875';INSERT into hero (name ,title ,alias ,heroid ) VALUES ('腕豪','瑟提','Sett','875') ;
                        string sql = "DELETE FROM hero WHERE heroid='"+ jsonHero.heroid + "';insert into hero (name ,title ,alias ,heroid ,roles) values ('" + jsonHero.name + "','" + jsonHero.title + "','" + jsonHero.alias + "','" + jsonHero.heroid + "','" + ArrayToString(jsonHero.roles) + "');";
                        //Console.WriteLine(sql);
                        runsql(sql);
                        string path = apppath + "\\images\\" + jsonHero.alias + ".png";
                        if (!File.Exists(path))
                        {
                            ThreadPool.UnsafeQueueUserWorkItem(new WaitCallback((object s) =>
                            {
                                _syncContext.Post(LabHandText, "更新资源:\\images\\" + jsonHero.alias + ".png");
                            }), null);
                            //URL = "https://lol.qq.com/data/info-defail.shtml?id=" + jsonHero.heroid,
                            HttpItem httpItem = new HttpItem()
                            {
                               
                                URL= "https://game.gtimg.cn/images/lol/act/img/champion/"+ jsonHero .alias+ ".png",
                                ResultType = ResultType.Byte
                            };
                            var result = httpItem.GetHtml().Result;
                            if (result.StatusCode == System.Net.HttpStatusCode.OK)
                            {
                                File.WriteAllBytes(path, result.ResultByte);
                            }
                            else {
                                Console.WriteLine(result.StatusDescription);
                            }
                        }
                    }
                }
                ThreadPool.UnsafeQueueUserWorkItem(new WaitCallback((object s) =>
                {
                    _syncContext.Post(LabHandText, "更新资源完毕");
                }), null);
                runsql("CREATE TABLE IF NOT EXISTS tricks(id varchar(4),heroid string,strickid string,name string,content string,Author string,updatetime string)",false);
                runsql("INSERT INTO 'main'.'tricks' ('id', 'heroid', 'strickid', 'name', 'content', 'Author', 'updatetime', 'ROWID') VALUES (1, '通用', 1, '走A', '-', '-', '-', 1)",false,false);
                runsql("INSERT INTO 'main'.'tricks' ('id', 'heroid', 'strickid', 'name', 'content', 'Author', 'updatetime', 'ROWID') VALUES (2, '通用', 2, 'QWER', '-', '-', '-', 2)",false,false);
                Dispose(false);
                Form2 form = new Form2();
                form.ShowDialog();
            }
        }

        private string ArrayToString(string[] strings)
        {
            string result = "";
            for (int i = 0; i < strings.Length; i++)
            {
                if (result == "")
                {
                    result = strings[i];
                }
                else
                {
                    result =result+","+strings[i];
                }
            }
            return result;
        }

        public void sleep(int t)
        {
            Thread.Sleep(t);
        }
        public void msg(string msg,string caption="提示",MessageBoxButtons buttons=MessageBoxButtons.OK,MessageBoxIcon icon=MessageBoxIcon.Information,MessageBoxDefaultButton messageBoxDefaultButton=MessageBoxDefaultButton.Button1)
        {
            MessageBox.Show(msg, caption, buttons, icon, messageBoxDefaultButton);
        }
        private bool runsql(string sql,bool autoclose=true ,bool alert=true)
        {
            
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
                if (sQLiteCommand.Connection == null)
                {
                    sQLiteCommand.Connection = sQLiteConnection;
                }
                sQLiteCommand.CommandText = sql;
                sQLiteCommand.ExecuteNonQuery();
                if (autoclose) 
                {
                    sQLiteConnection.Close();
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                if (alert)
                {
                    msg("执行SQL语句(" + sql + ")出错:" + e.Message);
                }
            }
            if (autoclose)
            {
                sQLiteConnection.Close();
            }
            return false;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            sQLiteConnection.Close();
        }
    }
    public class jsonHero
    {
        public string heroid;
        public string name;
        public string alias;
        public string title;
        public string[] roles;
        public string isweekfree;
        public string attack;
        public string defense;
        public string magic;
        public string difficulty;
        public string selectAudio;
        public string banAudio;
    }
    public class JsonParser
    {
        public jsonHero[] hero;
        public string version;
        public string fileName;
        public string FileTime;
    }
}
