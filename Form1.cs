using BLL.Hardware.ScanGang;
using System.Net;
using System.Windows.Forms;

namespace WinFormsApp1321
{
    public partial class Form1 : Form
    {
        private bool isOn = false; // 按钮状态
        private int currentCycle = 0; // 当前循环次数
        private int totalCycles = 0; // 总循环次数
        private CancellationTokenSource cancellationTokenSource; // 控制循环停止
        private bool isCalibrationMode = false;



        private TCPServer _tcpServer;
        private PLCClient _plcClient;
        private ScanGangBasic _scanGangBasic;

        public Form1()
        {
            InitializeComponent();
            // 初始化 PLC 和扫码枪
            _plcClient = new PLCClient("127.0.0.1", 6000);
            _scanGangBasic = new ScanGangBasic();

            // 初始化 TCPServer，并传入 PLC 和扫码枪实例
            _tcpServer = new TCPServer(_plcClient, _scanGangBasic);
        }





        private async void button1_Click(object sender, EventArgs e)
        {
            // 判断当前状态
            if (!isOn)
            {
                Console.WriteLine("尝试启动自校准模式...");

                // 寄存器写入 3，表示启动自校准模式
                bool writeSuccess = await _plcClient.WriteDRegisterAsync(2130, 3);

                if (writeSuccess)
                {
                    TCPServer.Mode = 1;
                    // 写入成功，进入自校准模式，弹出文件选择窗口
                    SelectionForm selectionForm = new SelectionForm();
                    selectionForm.ShowDialog();

                    if (selectionForm.DialogResult == DialogResult.OK)
                    {
                        // 放入样棒框
                        DialogResult result = MessageBox.Show(
                            $"系统文件：C:\\system\\system.ini\n" +
                            $"标样文件：{selectionForm.StandardFilePath}\n" +
                            $"标定循环次数：{selectionForm.CalibrationCount}\n" +
                            $"时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n" +
                            "放入样棒后点击确认？",
                            "放入样棒",
                            MessageBoxButtons.OKCancel,
                            MessageBoxIcon.Question
                        );

                        if (result == DialogResult.Cancel)
                        {
                            MessageBox.Show("操作已取消，自校准模式未开启。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        int[] response = await _plcClient.ReadDRegisterAsync(2132, 1);

                        if (response != null)
                        {
                            int scanAreaStatus = response[0];

                            // 判断扫码区是否存在样棒或待检棒
                            if (scanAreaStatus == 1)
                            {
                                MessageBox.Show("扫码区存在样棒或待检棒，发送扫码成功", "扫码成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                //

                                // 发送扫码成功信号给 PLC
                                bool confirmWriteSuccess = await _plcClient.WriteDRegisterAsync(2132, 3);
                                if (!confirmWriteSuccess)
                                {
                                    MessageBox.Show("无法通知 PLC 开始循环（D2132 = 3 失败）", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    return;
                                }
                                string selectedStandardFile = selectionForm.StandardFilePath;
                                totalCycles = selectionForm.CalibrationCount;
                                currentCycle = 0;

                                isOn = true;
                                button1.Text = "自校准模式已开启";
                                label1.Text = "当前状态：自校准模式";
                                button2.Enabled = false;


                                // 启动循环任务
                                cancellationTokenSource = new CancellationTokenSource();
                                CancellationToken token = cancellationTokenSource.Token;
                                //Task.Run(() => RunCalibrationLoop(selectedStandardFile, token));

                            }
                            else
                            {
                                MessageBox.Show("扫码区没有样棒或待检棒", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                return;
                            }
                        }


                    }
                }
                else
                {
                    bool errorReportSuccess = await _plcClient.WriteDRegisterAsync(2135, 1);
                    if (errorReportSuccess)
                    {
                        MessageBox.Show("无法向 D2135 发送异常报告！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    //  MessageBox.Show("无法写入 D 寄存器！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    StopCalibration(true);
                }
            }
            else
            {
                Console.WriteLine("尝试停止自校准模式...");


                bool writeSuccess = await _plcClient.WriteDRegisterAsync(2133, 1);

                if (writeSuccess)
                {

                    StopCalibration(false);


                    isOn = false;
                    button1.Text = "启动自校准模式";
                    label1.Text = "当前状态：待机状态";
                    button2.Enabled = false;
                }
                else
                {
                    // 写入 D 寄存器失败时，弹出错误提示
                    MessageBox.Show("无法停止自校准模式，写入 D 寄存器失败！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }


        private async void button2_Click(object sender, EventArgs e)
        {
            if (!isOn)  // 当前未开启
            {
                Console.WriteLine("尝试进入检测模式...");

                // 向 D2130 发送 1，通知 PLC 开启检测模式
                bool writeSuccess = await _plcClient.WriteDRegisterAsync(2130, 1);

                if (writeSuccess)
                {
                    // 更新状态
                    isOn = true;
                    button2.Text = "退出检测模式";
                    label1.Text = "当前状态：检测模式";
                    button1.Enabled = false;

                    Form2 form2 = new Form2();
                    var result = form2.ShowDialog();

                    if (result == DialogResult.Cancel)
                    {
                        Console.WriteLine("用户取消检测模式，恢复待机状态...");
                        await StopDetectionAsync();
                    }
                    else
                    {
                        // Form2 返回 OK，弹出确认框
                        string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                        string barcode = form2.BarcodeText;

                        DialogResult confirmationResult = MessageBox.Show(
                            $"当前时间：{currentTime}\n条码：{barcode}\n\n确认返回主界面？",
                            "确认信息",
                            MessageBoxButtons.OKCancel,
                            MessageBoxIcon.Question
                        );


                        if (confirmationResult == DialogResult.Cancel)
                        {

                            MessageBox.Show("操作已取消，退出检测模式。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            await StopDetectionAsync();


                            isOn = false;
                            button2.Text = "进入检测模式";
                            label1.Text = "当前状态：待机状态";
                            button1.Enabled = true;
                        }

                        else if (confirmationResult == DialogResult.OK)
                        {
                            form2.SaveBarcodeToFile(form2.BarcodeText);
                            form2.Close();
                        }
                    }
                }
                else
                {
                    MessageBox.Show("无法进入检测模式，PLC通信失败！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else  // 已经进入检测模式，点击后退出
            {
                Console.WriteLine("尝试退出检测模式...");

                bool writeSuccess = await _plcClient.WriteDRegisterAsync(2134, 1);

                if (writeSuccess)
                {
                    await StopDetectionAsync();
                }
                else
                {
                    MessageBox.Show("无法退出检测模式，PLC通信失败！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }



        private async Task StopDetectionAsync()
        {

            // 关闭检测模式界面（Form2）
            foreach (Form form in Application.OpenForms)
            {
                if (form is Form2)
                {
                    form.Close();
                    break;
                }
            }

            // 启用自校准按钮
            button1.Enabled = true;

            // 状态更新
            isOn = false;
            button2.Text = "进入检测模式";
            label1.Text = "当前状态：待机状态";
        }

        private void button3_Click(object sender, EventArgs e)
        {

        }



        private void WriteDeadlineToIni(string iniPath, DateTime deadline)
        {
            try
            {
                List<string> lines = new List<string>();

                if (File.Exists(iniPath))
                {
                    lines = File.ReadAllLines(iniPath).ToList();
                }

                bool found = false;
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].StartsWith("Deadline="))
                    {
                        lines[i] = $"Deadline={deadline:yyyy-MM-dd HH:mm:ss}"; // 直接更新
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    lines.Add($"Deadline={deadline:yyyy-MM-dd HH:mm:ss}"); // 确保一行
                }

                File.WriteAllLines(iniPath, lines);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"写入系统文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        private void UpdateValidUntilLabel(DateTime validUntil)
        {
            if (label3.InvokeRequired)
            {
                label3.Invoke(new Action(() => UpdateValidUntilLabel(validUntil)));
            }
            else
            {
                label3.Text = $"检测有效期限：{validUntil:yyyy-MM-dd HH:mm:ss}";
            }
        }



        private DateTime ReadDeadlineFromIni(string iniPath)
        {
            try
            {
                if (!File.Exists(iniPath))
                    return DateTime.MinValue;

                string[] lines = File.ReadAllLines(iniPath);
                foreach (string line in lines)
                {
                    if (line.StartsWith("Deadline="))
                    {
                        string deadlineStr = line.Split('=')[1].Trim();
                        if (DateTime.TryParse(deadlineStr, out DateTime deadline))
                        {
                            return deadline;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"读取系统文件失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return DateTime.MinValue;
        }


        private async void Form1_Load(object sender, EventArgs e)
        {
            string iniPath = "C:\\system\\system.ini";
            DateTime deadline = ReadDeadlineFromIni(iniPath);
            if (deadline != DateTime.MinValue)
            {
                UpdateValidUntilLabel(deadline);
            }

            Task.Run(() => CheckDeadline()); // 启动检查任务

            try
            {

                // 连接 PLC
                bool plcConnected = await _plcClient.ConnectAsync();
                if (plcConnected)
                {
                    Console.WriteLine("PLC 连接成功");
                }
                else
                {
                    Console.WriteLine("PLC 连接失败");
                }


                // 连接扫码枪
                string scannerIp = "127.0.0.1"; // 你的扫码枪 IP
                int scannerPort = 5001; // 端口号
                string deviceId = "Scanner_01"; // 设备 ID
                string errorMessage = string.Empty;
                bool scannerConnected = _scanGangBasic.Connect(scannerIp, scannerPort, deviceId, out errorMessage);
                if (scannerConnected)
                {
                    Console.WriteLine("扫码枪连接成功");
                }
                else
                {
                    Console.WriteLine("扫码枪连接失败");
                }

                // 启动 TCP 服务器
                await _tcpServer.StartWoLiuAsync();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"初始化失败: {ex.Message}");
            }
        }


        private async void CheckDeadline()
        {
            while (true)
            {
                string iniPath = "C:\\system\\system.ini";
                DateTime deadline = ReadDeadlineFromIni(iniPath);
                DateTime now = DateTime.Now;

                if (deadline != DateTime.MinValue)
                {
                    TimeSpan remaining = deadline - now;

                    if (remaining.TotalMinutes <= 60 && remaining.TotalMinutes > 59)
                    {
                        MessageBox.Show("检测有效期即将到期！剩余不到 1 小时。", "提醒", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else if (remaining.TotalSeconds <= 0)
                    {
                        MessageBox.Show("检测有效期已过期！", "警告", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        // 使用 Invoke 确保 UI 线程操作
                        if (button2.InvokeRequired)
                        {
                            button2.Invoke(new Action(() => button2.Enabled = false));
                        }
                        else
                        {
                            button2.Enabled = false;
                        }
                    }
                    /* else
                     {
                         // 使用 Invoke 确保 UI 线程操作
                         if (button2.InvokeRequired)
                         {
                             button2.Invoke(new Action(() => button2.Enabled = true));
                         }
                         else
                         {
                            button2.Enabled = true;
                         }
                     }*/
                }

                await Task.Delay(1800000); // 每 30fz检查一次
            }
        }





        private void UpdateCycleLabel()
        {
            if (label2.InvokeRequired)
            {
                // 如果在非UI线程，使用Invoke来回到UI线程更新
                label2.Invoke(new Action(UpdateCycleLabel));
            }
            else
            {
                label2.Text = $"当前循环次数：{currentCycle} / {totalCycles}";
            }
        }

        private void StopCalibration(bool isManualStop = false)
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
                cancellationTokenSource = null;
            }

            bool isCalibrationSuccessful = (currentCycle > 0 && currentCycle >= totalCycles);

            currentCycle = 0;
            totalCycles = 0;
            isOn = false;

            this.Invoke(new Action(() =>
            {
                button1.Text = "自校准模式关闭";
                label1.Text = "当前状态：待机状态";
                label2.Text = "当前循环次数：0";

                // 手动停止 or 异常终止，都应该禁用检测模式
                button2.Enabled = isCalibrationSuccessful && !isManualStop;
            }));
        }


        private void toolStripComboBox1_Click(object sender, EventArgs e)
        {

        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }



        /* private void Form1_Load(object sender, EventArgs e)
         {

         }*/


        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }
    }
}
