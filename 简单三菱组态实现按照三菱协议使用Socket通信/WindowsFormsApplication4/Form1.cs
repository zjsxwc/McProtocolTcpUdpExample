using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Collections;
using System.Threading;
using MCP;

namespace WindowsFormsApplication4
{



    public partial class Form1 : Form
    {
        public int count=0;
        Hashtable hshTable = new Hashtable();
        Plc qplc;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
            if (!File.Exists("iterms.txt"))
            {
                MessageBox.Show("不存在要被监视的软元件列表文件items.txt");
                return;
            }
            StreamReader sr_iterms = new StreamReader("iterms.txt");
            string iterm_line;
            while((iterm_line=sr_iterms.ReadLine())!=null)
            {
                if (iterm_line.Length <= 0) continue;
                //MessageBox.Show(iterm_line+"::"+iterm_line.Length.ToString());
                count++;
                Panel p = new Panel();
                p.Size = new Size(80, 40);
                p.BorderStyle = BorderStyle.FixedSingle;
                Label lb = new Label();
                lb.Text = count.ToString()+": " + iterm_line;
                p.Controls.Add(lb);
                
                Label lb_value = new Label();
                lb_value.Text = "null";
                lb_value.Location = new Point(lb.Location.X,lb.Location.Y+25);
                p.Controls.Add(lb_value);
                info i=new info();
                i.lb = lb_value;
                i.temp_value = lb_value.Text;
                hshTable.Add(iterm_line, i);
                //MessageBox.Show(((info)hshTable[iterm_line]).lb.Text);
                flowLayoutPanel1.Controls.Add(p);

            }
            sr_iterms.Close();
            button2.Enabled = true;
           
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
             //把hshTable中的temp_value赋给其lb的text，用以刷新
            //foreach (DictionaryEntry de in hshTable)
            IDictionaryEnumerator de = hshTable.GetEnumerator();
            while (de.MoveNext())
            {
                //MessageBox.Show(de.Key.ToString());
                //MessageBox.Show(((info)de.Value).temp_value);

                ((info)de.Value).lb.Text = ((info)de.Value).temp_value; //+ new Random().Next(0, 10).ToString()
                //这里可以添加<<规则>>

            }
        }


        Thread monitor_Thread = null;
        string plc_ip, plc_port, plc_staionnumber;

        public void monitor()
        {
            qplc = new McProtocolTcp(plc_ip, int.Parse(plc_port), McFrame.MC3E, (uint)int.Parse(plc_staionnumber));
            //qplc = new McProtocolTcp("192.168.0.2", int.Parse("2000"), McFrame.MC3E, (uint)int.Parse("8"));
            try { qplc.Open(); }
            catch(Exception e)
            {
                MessageBox.Show(e.Message);
                return;
            }

            while (true)
            {
                //foreach (DictionaryEntry de in hshTable)
                IDictionaryEnumerator de = hshTable.GetEnumerator();
                while (de.MoveNext())
                {
                    //if markup abortflag then qplc.close,thread.abort
                    PlcDeviceType type;
                    int addr;
                    McProtocolApp.GetDeviceCode(de.Key.ToString(), out type, out addr);

                    var val = new int[1];
                    //int rtCode = McProtocolApp.IsBitDevice(type) ? qplc.GetBitDevice(de.Key.ToString(), val.Length, val) :
                    //                                                       qplc.ReadDeviceBlock(de.Key.ToString(), val.Length, val);
                    try
                    {
                        int rtCode = McProtocolApp.IsBitDevice(type) ? qplc.GetBitDevice(de.Key.ToString(), val.Length, val) :
                                                                           qplc.ReadDeviceBlock(de.Key.ToString(), val.Length, val);
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message);
                        return;
                    }
                    ((info)de.Value).temp_value=val[0].ToString();
                }
                Thread.Sleep(20);
            }
            qplc.Close();
        }


        private void button2_Click(object sender, EventArgs e)
        {
            if (!File.Exists("config.txt"))
            {
                MessageBox.Show("不存在用于设置plc网络信息的文件config.txt");
                return;
            }
            StreamReader sr_config = new StreamReader("config.txt");
            
            plc_ip = sr_config.ReadLine();
            plc_port = sr_config.ReadLine();
            plc_staionnumber = sr_config.ReadLine();

            /*config.txt内容：
             * 192.168.0.2
             * 2000
             * 8
             * 
             */
            sr_config.Close();

            button2.Enabled = false;
            monitor_Thread = new Thread(new ThreadStart(this.monitor));
            monitor_Thread.Start();
            timer1.Start();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (monitor_Thread!=null){
                //markup abortflag
                monitor_Thread.Abort();
            }
        }
    }

    class info
    {
        public Label lb;
        public string temp_value;
    }
}
