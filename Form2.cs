using System;
using System.Windows.Forms;
using System.Net;
using JinYiHelp.EasyHTTPClient;
using System.Text;
using System.Web.Script.Serialization;
using System.Net.Http;

namespace TricksSpeedMaster
{
    public partial class Form2 : Form
    {
        private string hosturl = "http://softauth.localhost/";
        private string apiurl = "";
        public Form2()
        {
            InitializeComponent();
            apiurl = hosturl + "api.php";
    }

        private void button1_Click(object sender, EventArgs e)
        {
            login();
        }
        private void login()
        {
            try
            {

                HttpItem item = new HttpItem()
                {
                    URL = "https://game.gtimg.cn/images/lol/act/img/js/heroList/hero_list.js",
                    Encoding = Encoding.UTF8
                };
                var result = item.GetHtml().Result;
                //Console.WriteLine(result.Cookie);
                if (result.IsSuccessStatusCode)
                {
                   string info = result.Html;
                    Console.WriteLine(info);
                }
                else
                {
                    MessageBox.Show(result.StatusDescription);
                    Console.WriteLine(result.StatusDescription);
                    return;
                }
                Console.WriteLine(apiurl);
                HttpItem httpItem = new HttpItem()
                {
                    URL = apiurl + "?action=login",
                    Encoding = Encoding.UTF8
                };
                result = httpItem.GetHtml().Result;
                if (result.IsSuccessStatusCode)
                {
                    string sResult = result.Html;
                    Console.WriteLine(sResult);
                }
                else
                {
                    MessageBox.Show("网络错误:" + result.StatusDescription);
                }
                //Close();
                //Dispose(true);
                //Form3 form = new Form3();
                //form.ShowDialog();
            }
            catch (Exception err) {
                Console.WriteLine(err.Message);
            }
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            login();
        }

        private void button2_Click(object sender, EventArgs e)
        {

        }
    }
}
