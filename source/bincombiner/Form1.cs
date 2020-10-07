using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace bincombiner
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        class Config
        {
            public int PageSize { get; set; }
            public FileInfos.CheckSum checksum { get; set; }


            public static Config Load()
            {
                try
                {
                    var s = File.ReadAllText("config.json");
                    return JsonConvert.DeserializeObject<Config>(s);
                }
                catch (Exception )
                {
                    
                    Config cfg = new Config();
                    cfg.PageSize = 4;
                    cfg.checksum = FileInfos.CheckSum.CRC32;
                    return cfg;
                }
                return null;
            }

            public void Save()
            {
                File.WriteAllText("config.json", JsonConvert.SerializeObject(this, Formatting.Indented));
            }
        }

        private Config cfg;

        private void Form1_Load(object sender, EventArgs e)
        {
            foreach (DataGridViewColumn v in dataGridView1.Columns)
            {
                Debug.WriteLine(v);
                if (v.HeaderText == "fi")
                {
                    v.Visible = false;
                }
            }

            cfg = Config.Load();

            numericUpDown1.Value = cfg.PageSize;

            switch (cfg.checksum)
            {
                case FileInfos.CheckSum.CRC8:
                    radioButton1.Checked = true;
                    break;
                case FileInfos.CheckSum.CRC16:
                    radioButton2.Checked = true;
                    break;
                case FileInfos.CheckSum.CRC32:
                    radioButton3.Checked = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }



            // bindingSource1.Add(new FileInfos(){fi = new FileInfo("bincombiner.exe") });
            // bindingSource1.Add(new FileInfos(){fi = new FileInfo("bincombiner.pdb"), Offset = 0x5000});


            RefreshCrc();
        }

        private void bindingSource1_CurrentChanged(object sender, EventArgs e)
        {

        }

        private void dataGridView1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        private void dataGridView1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            foreach (string file in files)
            {
                Console.WriteLine(file);

                bindingSource1.Add(new FileInfos()
                {
                    fi = new FileInfo(file)
                });
            }
            RefreshCrc();


        }

        private void button3_Click(object sender, EventArgs e)
        {
            var p = bindingSource1.Position;

            var np = p + 1;

            if (sender == button3)
            {
                np = p - 1;
            }

            if (np >= 0 && np < bindingSource1.Count)
            {
                var c = bindingSource1.Current;
                bindingSource1.RemoveAt(p);
                bindingSource1.Insert(np, c);

                var sr = dataGridView1.SelectedCells[0].RowIndex;
                var sc = dataGridView1.SelectedCells[0].ColumnIndex;


                var cell = dataGridView1.SelectedCells[0];

                cell.OwningRow.Selected = false;

                dataGridView1.Rows[np].Cells[sr].Selected = true;
                bindingSource1.Position = np;
            }
        }

        void RefreshCrc()
        {

            foreach (FileInfos v in bindingSource1.List)
            {
                v.checksum = cfg.checksum;
                v.CalcChecksum();
            }

            dataGridView1.Refresh();
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (sender == radioButton1)
            {
                cfg.checksum = FileInfos.CheckSum.CRC8;
            }
            else if (sender == radioButton2)
            {
                cfg.checksum = FileInfos.CheckSum.CRC16;
            }
            else if (sender == radioButton3)
            {
                cfg.checksum = FileInfos.CheckSum.CRC32;
            }

            RefreshCrc();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                cfg.Save();
            }
            catch (Exception exception)
            {
                
            }
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            cfg.PageSize = (int) numericUpDown1.Value;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (bindingSource1.Position != -1)
            {
                bindingSource1.RemoveAt(bindingSource1.Position);

            }
        }


        void Log(string s)
        {
            richTextBox1.AppendText($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}>{s}\r");
            richTextBox1.ScrollToCaret();
        }


        void Gereration(string filename)
        {
            File.Delete(filename);

            var f = File.OpenWrite(filename);

            try
            {
                FileInfos prv = null;

                long length = 0;

                foreach (FileInfos b in bindingSource1)
                {
                    if (prv != null)
                    {
                        if (length > b.Offset)
                        {
                            MessageBox.Show($"파일 오프셋을 확인하세요.\r{b.FileName} {b.Offset}");
                            return;
                        }
                    }

                    if (b.fi == null || !b.fi.Exists)
                    {
                        MessageBox.Show($"파일을 확인하세요.\r{b.FileName}");
                        return;
                    }

                    if (prv == null)
                    {
                        // 처음건 오프셋 더하기
                        length += b.Offset;
                    }

                    length += b.fi.Length;
                    prv = b;
                }

                prv = null;

                length = 0;

                for (int n = 0; n < bindingSource1.Count; n++)
                {
                    var b = bindingSource1[n] as FileInfos;

                    if (prv != null)
                    {
                        if (length < b.Offset)
                        {
                            var len = b.Offset - length;
                            f.Write(new byte[len], 0, (int)len);
                            length += len;
                        }
                    }

                    if (prv == null)
                    {

                        var len = b.Offset;
                        f.Write(new byte[len], 0, (int)len);

                        // 처음건 오프셋 더하기
                        length += b.Offset;
                    }

                    var r = File.ReadAllBytes(b.FileName);
                    length += b.fi.Length;
                    f.Write(r, 0, r.Length);


                    if (n == bindingSource1.Count - 1)
                    {
                        // 마지막 채우기?

                        if (checkBox1.Checked)
                        {

                            int pagesize = 0;
                            if (cfg.PageSize != 0)
                            {
                                pagesize = cfg.PageSize * 1024;
                            }

                            if (pagesize != 0)
                            {
                                var len = length % pagesize;
                                length += len;

                                f.Write(new byte[len], 0, (int)len);

                            }

                        }

                    }


                    prv = b;
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                Log($"Gerneration Error : " + RetriveException(exception));
            }
            finally
            {
                f.Close();
            }

        }

        private void button6_Click(object sender, EventArgs e)
        {

            SaveFileDialog ofd = new SaveFileDialog();

            ofd.Filter = "binary file(*.bin)|*.bin";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                Gereration(ofd.FileName);
            }
        }

        string RetriveException(Exception e)
        {
            StringBuilder s = new StringBuilder();

            s.AppendLine($"ERR : {e.Message}");
            s.AppendLine(e.StackTrace);

            return s.ToString();
        }


        private void dataGridView1_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            var c = dataGridView1.Columns[e.ColumnIndex];
            Debug.WriteLine(e.FormattedValue);

            if (c.HeaderText.Contains("Offset"))
            {
                var v = e.FormattedValue as string;

                try
                {
                    Convert.ToInt64(v, 16);
                }
                catch (Exception exception)
                {
                    e.Cancel = true;
                }
            }
            else if (c.HeaderText.Contains("FileSize"))
            {
                var v = e.FormattedValue as string;

                try
                {
                    Convert.ToInt64(v, 16);
                }
                catch (Exception exception)
                {
                    e.Cancel = true;
                }
            }

        }

        private void button8_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();

            ofd.Filter = "File List(*.json)|*.json";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                bindingSource1.List.Clear();

                try
                {
                    var i = JsonConvert.DeserializeObject<List<FileInfos>>(File.ReadAllText(ofd.FileName));
                    foreach (var f in i)
                    {
                        bindingSource1.List.Add(f);
                    }

                    RefreshCrc();
                }
                catch (Exception exception)
                {
                    MessageBox.Show($"저장 파일 파싱 실패\n{exception.Message}");
                }


            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            SaveFileDialog ofd = new SaveFileDialog();

            ofd.Filter = "File List(*.json)|*.json";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(ofd.FileName, JsonConvert.SerializeObject(bindingSource1.List, Formatting.Indented));
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            try
            {
                FileInfos prv = null;
                long length = 0;

                int pagesize = 0;
                if (cfg.PageSize != 0)
                {
                    pagesize = cfg.PageSize * 1024;
                }

                foreach (FileInfos b in bindingSource1)
                {
                    if (prv != null)
                    {
                        b.Offset = length;
                    }

                    if (b.fi == null || !b.fi.Exists)
                    {

                    }


                    if (prv == null)
                    {
                        // 처음건 오프셋 더하기
                        length += b.Offset;
                    }


                    length += b.fi.Length;

                    if (pagesize != 0)
                    {
                        var rem = length % pagesize;
                        length += rem;
                    }

                    prv = b;
                }

                dataGridView1.Refresh();

                prv = null;

            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                MessageBox.Show($"오프셋 정렬 실패 : {exception.Message}");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();

            ofd.Filter = "binanry(*.bin)|*.bin|all(*.*)|*.*";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                bindingSource1.List.Clear();

                try
                {
                    var i = JsonConvert.DeserializeObject<List<FileInfos>>(File.ReadAllText(ofd.FileName));
                    foreach (var f in i)
                    {
                        bindingSource1.List.Add(f);
                    }

                    RefreshCrc();
                }
                catch (Exception exception)
                {
                    MessageBox.Show($"저장 파일 파싱 실패\n{exception.Message}");
                }


            }
        }
    }
}
