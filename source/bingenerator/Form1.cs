using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExcelDataReader;

namespace bingenerator
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        void Gen(string file)
        {
            new DirectoryInfo("temp").Create();
            var vv = new FileInfo(file);
            var o = vv.CopyTo($"temp\\temp{DateTime.Now.ToString("yyyyMMddHHmmssfff")}.{vv.Extension}");

            var pa = vv.FullName.Replace(vv.Extension, ".bin");



            using (var stream = File.Open(o.FullName, FileMode.Open, FileAccess.Read))
            {
                List<Int32> li = new List<int>();

                List<byte> lData = new List<byte>();

                // Auto-detect format, supports:
                //  - Binary Excel files (2.0-2003 format; *.xls)
                //  - OpenXml Excel files (2007 format; *.xlsx)
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    // Choose one of either 1 or 2:

                    // 1. Use the reader methods
                    do
                    {
                        while (reader.Read())
                        {
                            var p = reader.GetString(0);

                            var ooo = p.Replace("0x", "");

                            var i = Convert.ToInt32(ooo, 16);
                            li.Add(i);
                            
                            byte[] intBytes = BitConverter.GetBytes(i);

                            lData.AddRange(intBytes);


                        }
                    } while (reader.NextResult());


                    File.WriteAllBytes(pa, lData.ToArray());
                }
            }

            o.Delete();
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files)
            {
                Console.WriteLine(file);
                Gen(file);
            }

            
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;

        }


        List<byte> TextToByteList(string text)
        {
            string[] lines = text.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            );
            
            var l = lines.Select(s => s.Trim()).ToArray();
            var o = l.Where(s => !string.IsNullOrEmpty(s)).ToArray();
            
            if (l.Length != o.Length)
            {
                // 공란 데이터 체크
                if ((l.Length - 1) == o.Length)
                {
                    if (l[l.Length - 1] == "")
                    {
                        // 마지막 줄 공란이라면 패스 해줌.

                    }
                    else
                    {
                        throw new Exception("내용에 공란이 있습니다.");
                    }
                }
                else
                {
                    throw new Exception("내용에 공란이 있습니다.");
                }
            }

            List<Int32> li = new List<int>();

            List<byte> lData = new List<byte>();

            foreach (var line in o)
            {
                var p = line;

                var ooo = p.Replace("0x", "");

                var i = Convert.ToInt32(ooo, 16);
                li.Add(i);

                byte[] intBytes = BitConverter.GetBytes(i);

                lData.AddRange(intBytes);

            }

            return lData;
        }

        void SaveBin(string filename, List<byte> data)
        {
            File.WriteAllBytes(filename, data.ToArray());
        }

        void ConvertToFile()
        {
            if (textBox1.Text == "")
            {
                MessageBox.Show("내용을 입력하세요.");
                return;
            }

            try
            {

                var o = TextToByteList(textBox1.Text);
                var s = new SaveFileDialog();
                s.AddExtension = true;
                s.Filter = "bin|*.bin";

                if (s.ShowDialog(this) == DialogResult.OK)
                {
                    SaveBin(s.FileName, o);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "변환에 실패 하였습니다.");
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            ConvertToFile();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
#if false
            textBox1.Text = @"0x5555
0x7283838
0x23
0x5555
0x5555
0x7283838
0x23
0x5555
0x7283838
0x23
0x5555
0x7283838
0x23
";

            button1.PerformClick();
#endif
        }
    }
}
