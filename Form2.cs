using BLL.Hardware.ScanGang;
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

    public partial class Form2 : Form
    {






        public string BarcodeText { get; set; }

        public Form2()
        {
            InitializeComponent();
        }

        public void SaveBarcodeToFile(string barcode)  // ⚠ 去掉 static
        {
            if (string.IsNullOrWhiteSpace(barcode))
            {
                MessageBox.Show("条码不能为空！", "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string directoryPath = @"C:\system\";
            string filePath = Path.Combine(directoryPath, $"{timestamp}.txt");

            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                string content = $"时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}\n条码：{barcode}\n-------------------\n";
                File.AppendAllText(filePath, content, Encoding.UTF8);

                MessageBox.Show($"条码已保存到 {filePath}", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存条码失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void label2_Click(object sender, EventArgs e)
        {

        }

        // 确认按钮
        private async void button1_Click(object sender, EventArgs e)
        {
            BarcodeText = textBox1.Text.Trim();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        // 取消按钮
        private void button2_Click(object sender, EventArgs e)
        {

            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
