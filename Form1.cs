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
            // 调用 WriteDRegisterAsync 方法, 写入 D 寄存器进入自校准模式
            bool writeSuccess = await _plcClient.WriteDRegisterAsync(2130, 3);

            // 如果写入成功，才继续执行文件选择窗口
            if (writeSuccess)
            {
                // 进入自校准模式后，弹出文件选择窗口
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

                    // 如果点击了“取消”，则直接返回
                    if (result == DialogResult.Cancel)
                    {
                        MessageBox.Show("操作已取消，自校准模式未开启。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    string selectedStandardFile = selectionForm.StandardFilePath;
                    totalCycles = selectionForm.CalibrationCount;
                    currentCycle = 0;

                    // 确认后，创建 循环任务 并启动循环
                    cancellationTokenSource = new CancellationTokenSource();
                    CancellationToken token = cancellationTokenSource.Token;
                    await Task.Run(() => RunCalibrationLoop(selectedStandardFile, token));

                    // 更新状态
                    isOn = true;
                    button1.Text = "自校准模式已开启";
                    label1.Text = "当前状态：自校准模式";
                    button2.Enabled = false;
                }
            }
            else
            {
                // 写入 D 寄存器失败时，弹出错误提示
                MessageBox.Show("无法写入 D 寄存器！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                StopCalibration(true);
            }
        }



        private void button2_Click(object sender, EventArgs e)
        {



            label1.Text = "当前状态：检测模式";
            isOn = !isOn; // 切换状态
            button2.Text = isOn ? "检测模式已开启" : "检测模式关闭";
            if (isOn == false)
            {
                label1.Text = "当前状态：待机状态";
            }
            //MessageBox.Show("检测模式已关闭！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            button1.Enabled = !isOn;

        }
        private void button3_Click(object sender, EventArgs e)
        {
           /* // 复位状态
            //isCalibrationMode = false;
            isOn = false;


            label1.Text = "当前状态：待机状态";


            button1.Enabled = true;
            button2.Enabled = true;
            button1.Text = "自校准模式关闭";
            button2.Text = "检测模式关闭";

            MessageBox.Show("系统已恢复为待机状态！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
         //   StopCalibration(true);*/
        }

        /*  private async Task RunCalibrationLoop(string selectedStandardFile, CancellationToken token)
          {
              DateTime lastCycleEndTime = DateTime.Now;
              string iniPath = "C:\\system\\system.ini";

              while (currentCycle < totalCycles)
              {
                  if (token.IsCancellationRequested)
                  {
                      MessageBox.Show("自校准任务已停止！", "停止", MessageBoxButtons.OK, MessageBoxIcon.Information);
                      StopCalibration();
                      return;
                  }

                  currentCycle++;
                  UpdateCycleLabel();

                  bool isMatched = CompareIniFiles("D:\\标样\\样管1.ini", selectedStandardFile);

                  if (!isMatched)
                  {
                      MessageBox.Show("出现缺陷数据异常！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                      StopCalibration();
                      return;
                  }

                  lastCycleEndTime = DateTime.Now;  // 记录本次循环的结束时间

                  if (currentCycle >= totalCycles)
                  {
                      MessageBox.Show("检测完成！所有循环已执行。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);

                      DateTime validUntil = lastCycleEndTime.AddHours(2); // 计算有效期限
                      WriteDeadlineToIni(iniPath, validUntil);  // 写入 system.ini
                      UpdateValidUntilLabel(validUntil); // 更新 UI

                      StopCalibration();
                  }

                  await Task.Delay(10000, token);
              }
          }*/
        private async Task RunCalibrationLoop(string selectedStandardFile, CancellationToken token)
        {
            DateTime lastCycleEndTime = DateTime.Now;
            string iniPath = "C:\\system\\system.ini";
            string sampleFolder = "D:\\标样\\yangguang"; // 样管文件夹路径

            int fileIndex = 1; // 样管文件索引

            while (currentCycle < totalCycles)
            {
                if (token.IsCancellationRequested)
                {
                    MessageBox.Show("自校准任务已停止！", "停止", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    StopCalibration();
                    return;
                }

                currentCycle++;
                UpdateCycleLabel();

                // 生成当前循环的样管文件名
                string sampleFile = Path.Combine(sampleFolder, $"样管{fileIndex}.ini");

                // 检查文件是否存在
                if (!File.Exists(sampleFile))
                {
                    MessageBox.Show($"缺少样管文件: {sampleFile}！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    StopCalibration();
                    return;
                }

              

                if (currentCycle >= totalCycles)
                {
                    MessageBox.Show("检测完成！所有循环已执行。", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    DateTime validUntil = lastCycleEndTime.AddHours(2); // 计算有效期限
                    WriteDeadlineToIni(iniPath, validUntil); // 写入 system.ini
                    UpdateValidUntilLabel(validUntil); // 更新 UI

                    this.Invoke(new Action(() =>
                    {
                        button2.Enabled = true;  // 只有成功完成才启用检测模式
                    }));

                    StopCalibration(false);
                }

                await Task.Delay(10000, token); // 等待 10 秒，进入下一次循环
            }
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
                bool scannerConnected = _scanGangBasic.Connect(scannerIp,scannerPort,deviceId,out errorMessage);
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


        /*  private void StopCalibration()
          {
              if (cancellationTokenSource != null)
              {
                  cancellationTokenSource.Cancel();  // 取消任务
                  cancellationTokenSource.Dispose(); // 释放资源
                  cancellationTokenSource = null;
              }

              currentCycle = 0;
              totalCycles = 0;
              isOn = false;

              // 在UI线程上更新
              this.Invoke(new Action(() =>
              {
                  button1.Text = "自校准模式关闭";
                  label1.Text = "当前状态：待机状态";
                  label2.Text = "当前循环次数：0";
                  button2.Enabled = true;  // 启用按钮2
              }));
          }*/




        /* private Dictionary<string, int> ReadIniValues(string iniPath, string section)
         {
             Dictionary<string, int> values = new Dictionary<string, int>();

             string[] lines = File.ReadAllLines(iniPath);
             bool inSection = false;

             foreach (string line in lines)
             {
                 string trimmedLine = line.Trim();

                 if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                 {
                     inSection = trimmedLine.Equals($"[{section}]", StringComparison.OrdinalIgnoreCase);
                     continue;
                 }

                 if (inSection && trimmedLine.Contains("="))
                 {
                     string[] parts = trimmedLine.Split('=');
                     if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out int value))
                     {
                         values[parts[0].Trim()] = value;
                     }
                 }
             }

             return values;
         }*/


        /*  private int ReadIniTolerance(string iniPath)
          {
              string[] lines = File.ReadAllLines(iniPath);
              foreach (string line in lines)
              {
                  if (line.StartsWith("Value=") && int.TryParse(line.Split('=')[1].Trim(), out int tolerance))
                  {
                      return tolerance;
                  }
              }
              return 10; // 默认误差±10
          }
  */
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
