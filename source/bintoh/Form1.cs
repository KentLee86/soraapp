using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using bintoh.Properties;

namespace bintoh
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            DragOver += Form_DragOver;

            DragDrop += Form_DragDrop;


        }

        private void Form_DragDrop(object sender, DragEventArgs e)
        {
            string[] filePathArray = e.Data.GetData(DataFormats.FileDrop) as string[];
            
            if (filePathArray != null)
            {
                foreach (string filePath in filePathArray)
                {
                    MakeHeaderFile(new FileInfo(filePath));
                }
            }
        }

        private void Form_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        void LogAdd(string s)
        {
            s = $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} {s}\r";

            var ac = new Action(delegate
            {
                richTextBox1.AppendText(s);
                richTextBox1.ScrollToCaret();
            });

            if (InvokeRequired)
            {
                Invoke(ac);
            }
            else
            {
                ac();
            }
        }

        void MakeHeaderFile(FileInfo fi)
        {
            lbCRC.Text = "";
            var oo = fi.FullName;
            oo = oo.Replace(fi.Extension, ".h");

            LogAdd($"생성 시작 : {fi.FullName}");


            StreamWriter fo = null;
            if (ss.AutoFileGen)
                fo = File.CreateText(oo);

            var b = File.ReadAllBytes(fi.FullName);

            UInt32 crc = 0;

            try
            {
                for (int i = 0; i < b.Length; i++)
                {
                    crc += b[i];

                    if (ss.AutoFileGen)
                    {
                        fo.Write($"0x{b[i]:X02}, ");
                        if (i % 10 == 9)
                        {
                            fo.Write($"\r");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogAdd($"생성 실패 : {e.Message}");
            }
            finally
            {
                lbCRC.Text = $"{crc:X04}";

                if(ss.AutoFileGen)
                    LogAdd($"생성 성공 : {oo}");
                LogAdd($"CRC8 : 0x{(byte)crc:X02}, CRC16 : 0x{(UInt16)crc:X04}, CRC32 : 0x{(UInt32)crc:X08} ");
                
                
                if(ss.AutoFileGen)
                    fo.Close();
            }
        }


        private void Form1_Load(object sender, EventArgs e)
        {

            checkBox1.Checked = Properties.Settings.Default.AutoFileGen;

        }

        private void maskedTextBox1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                maskedTextBox1.Text = ofd.FileName;

                if (MessageBox.Show("헤더 파일을 생성할까요?","파일 생성", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    button1.PerformClick();
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (new FileInfo(maskedTextBox1.Text).Exists)
            {
                MakeHeaderFile(new FileInfo(maskedTextBox1.Text));
            }
            else
            {
                MessageBox.Show("파일을 선택해주세요.");
            }
        }

        private void maskedTextBox1_MaskInputRejected(object sender, MaskInputRejectedEventArgs e)
        {

        }


        private Settings ss = Properties.Settings.Default;

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            ss.AutoFileGen = checkBox1.Checked;
            ss.Save();

                
        }
    }
}
