using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace WinFormsApp1321
{
    public class PLCClient
    {
        private readonly string plcIp;
        private readonly int plcPort;
        private TcpClient client;
        private NetworkStream stream;

        public PLCClient(string ip, int port)
        {
            plcIp = ip;
            plcPort = port;
        }

        // 连接PLC (异步)
        public async Task<bool> ConnectAsync()
        {
            try
            {
                client = new TcpClient();
                await client.ConnectAsync(plcIp, plcPort);
                stream = client.GetStream();
                Console.WriteLine("✅ 连接PLC成功");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 连接PLC失败: {ex.Message}");

                // 连接失败时弹出窗口提示
                MessageBox.Show($"无法连接到PLC: {ex.Message}", "连接失败", MessageBoxButtons.OK, MessageBoxIcon.Error);

                return false;
            }
        }


        // 关闭连接
        public void Close()
        {
            stream?.Dispose();  // 直接调用 Dispose()
            client?.Close();
            Console.WriteLine("🔌 已断开PLC连接");
        }


        // 发送SLMP指令并接收响应 (异步)
        private async Task<byte[]> SendAndReceiveAsync(byte[] command)
        {
            try
            {
                await stream.WriteAsync(command, 0, command.Length);

                byte[] response = new byte[512]; // 预留足够空间
                int bytesRead = await stream.ReadAsync(response, 0, response.Length);
                Array.Resize(ref response, bytesRead); // 截取有效数据
                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 通信错误: {ex.Message}");
                return null;
            }
        }

        // 读取 D 寄存器 (异步) (例如: 读取 D100)
        public async Task<byte[]> ReadDRegisterAsync(int address)
        {
            byte[] command = BuildReadDCommand(address);
            byte[] response = await SendAndReceiveAsync(command);

            // 确保响应不为空且长度足够
            if (response == null || response.Length < 15)
            {
                Console.WriteLine("❌ 无效的 PLC 响应（数据为空或长度不足）");
                return null;
            }                                                                       
            // 检查结束代码 (response[9] 和 response[10] 组成的 2 字节)
            if (response[9] != 0x00 || response[10] != 0x00)
            {
                Console.WriteLine($"⚠️ PLC 返回异常，结束代码: 0x{response[9]:X2}{response[10]:X2}");
                return null; // 你也可以选择返回特定错误信息
            }

            // 提取 response[9] 到 response[14] 的字节并返回
            byte[] data = new byte[4];
            Array.Copy(response, 11, data, 0, 4);
            return data;
        }


        //    写入 D 寄存器 (异步) (例如: D100 = 5678)
        public async Task<bool> WriteDRegisterAsync(int address, int value)
        {
            // 构造写入指令
            byte[] command = BuildWriteDCommand(address, value);

            // 发送指令并接收响应
            byte[] response = await SendAndReceiveAsync(command);

            // 如果响应为空或者响应长度不足，说明出错
            if (response == null || response.Length < 11)
            {
                Console.WriteLine("❌ 响应无效，长度不足");
                return false;
            }
            // 检查响应中的结束代码（假设结束代码在第9和第10字节）
            int endCode = BitConverter.ToUInt16(response, 9);

            // 如果结束代码为0x0000，表示写入成功
            if (endCode == 0x0000)
            {
                Console.WriteLine("✅ D寄存器写入成功");
                return true;
            }
            else
            {
                // 如果结束代码不是0x0000，表示写入失败或发生异常
                Console.WriteLine($"❌ D寄存器写入失败，错误代码: {endCode}");

                // 可根据需要提取异常信息并打印
                byte[] exceptionData = new byte[response.Length - 11];
                Array.Copy(response, 11, exceptionData, 0, exceptionData.Length);
                Console.WriteLine($"异常信息: {BitConverter.ToString(exceptionData)}");

                return false;
            }
        }

        // 生成读取 D 寄存器的 SLMP 指令
        private byte[] BuildReadDCommand(int address)
        {
            return new byte[]
            {
            0x50, 0x00, 0x00, 0xFF, 0xFF, 0x03, 0x00,  // 头部
            0x0C, 0x00,  // 数据长度12
            0x00, 0x00,  // 保留
            0x01, 0x04,  // 指令
            0x00, 0x00,  // 子命令
            (byte)(address & 0xFF),          // 低字节
            (byte)((address >> 8) & 0xFF),   // 中间字节
            (byte)((address >> 16) & 0xFF),  // 高字节
            0xA8,// D寄存器标识符 (0xA8)
            0x02, 0x00  //软元件点数
            };
        }

        // 生成写入 D 寄存器的 SLMP 指令
        private byte[] BuildWriteDCommand(int address, int value)
        {
            return new byte[]
            {
            0x50, 0x00, 0x00, 0xFF, 0xFF, 0x03, 0x00,  // 头部  
            0x0E, 0x00,  // 数据长度14
            0x00, 0x00,  // 保留
            0x01, 0x04,  // 指令
            0x00, 0x00,  // 子命令
            (byte)(address & 0xFF),          // 低字节
            (byte)((address >> 8) & 0xFF),   // 中间字节
            (byte)((address >> 16) & 0xFF),  // 高字节
            (byte)(address & 0xFF), (byte)((address >> 8) & 0xFF),  // 地址
            0xA8,  // D寄存器标识符 (0xA8)
            0x01, 0x00,  //软元件点数
            // 将 value 按照 2 字节插入
           (byte)(value & 0xFF),  // 低字节
           (byte)((value >> 8) & 0xFF),  // 高字节
            };
        }
    }
}
