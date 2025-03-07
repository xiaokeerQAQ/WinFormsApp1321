using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsApp1321
{
    public partial class SelectionForm : Form
    {
        public string StandardFilePath { get; private set; } = "";//文件路径
        public int CalibrationCount { get; private set; } = 0;//次数
        public string SystemFilePath { get; private set; } = @"C:\system\system.ini";
        public SelectionForm()
        {
            InitializeComponent();
            textBox3.Text = SystemFilePath;
            textBox3.ReadOnly = true;
            CalibrationCount = ReadCalibrationCount();
            textBox2.Text = CalibrationCount.ToString();
        }
        private int ReadCalibrationCount()
        {
            if (File.Exists(SystemFilePath))
            {
                try
                {
                    string[] lines = File.ReadAllLines(SystemFilePath);
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("CalibrationCount="))
                        {
                            string value = line.Split('=')[1].Trim();
                            if (int.TryParse(value, out int count) && count > 0)
                            {
                                return count;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("读取系统文件失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            return 12; // 默认值，防止 `CalibrationCount` 变成 0
        }


        // 保存循环次数到系统文件
        private void SaveCalibrationCount(int count)
        {
            try
            {
                List<string> lines = new List<string>();

                if (File.Exists(SystemFilePath))
                {
                    lines = File.ReadAllLines(SystemFilePath).ToList();
                }

                bool found = false;
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].StartsWith("CalibrationCount="))
                    {
                        lines[i] = $"CalibrationCount={count}"; // 直接更新值
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    lines.Add($"CalibrationCount={count}"); // 确保一定有这一行
                }

                // 确保不会因 count = 0 而删除这一行
                File.WriteAllLines(SystemFilePath, lines);
            }
            catch (Exception ex)
            {
                MessageBox.Show("保存循环次数失败：" + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void SelectionForm_Load(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(StandardFilePath))
            {
                MessageBox.Show("请选择标样文件！", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(textBox2.Text, out int count) || count < 1)
            {
                MessageBox.Show("循环次数不得小于1！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBox2.Text = CalibrationCount.ToString(); // 恢复
                return;
            }

            CalibrationCount = count;
            SaveCalibrationCount(CalibrationCount); // 保存
            textBox2.Text = CalibrationCount.ToString();  // ✅ 立即更新 UI
            this.DialogResult = DialogResult.OK;
            this.Close();
        }


        private void button2_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "请选择标样文件";
            openFileDialog.Filter = "文本文件 (*.ini)|*.ini|所有文件 (*.*)|*.*";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                StandardFilePath = openFileDialog.FileName;
                textBox1.Text = StandardFilePath;  // 更新文本框显示
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
