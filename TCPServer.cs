using BLL.Hardware.ScanGang;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace WinFormsApp1321
{
    public class TCPServer
    {
        private Socket _serverSocket;
        private List<Socket> _clients = new List<Socket>();
        private const int BufferSize = 1024;
        private bool _isRunning = false;
        private ScanGangBasic _scanGangBasic;
        private PLCClient _plcClient;



        public event Action<string> OnClientConnected;
        public event Action<string, string> OnMessageReceived;
        public event Action<string> OnClientDisconnected;
        public event Action<string> OnError;

        public TCPServer(PLCClient plcClient, ScanGangBasic scanGangBasic)
        {
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _plcClient = plcClient;
            _scanGangBasic = scanGangBasic;
        }

        /*public void StartWoLiu()
        {
            try
            {
                _serverSocket.Bind(new IPEndPoint(IPAddress.Loopback, 6065));
                _serverSocket.Listen(10);
                _isRunning = true;
                Console.WriteLine("服务器已启动，等待客户端连接...");
                AcceptClient();
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"服务器启动失败: {ex.Message}");
            }
        }

        private void AcceptClient()
        {
            _serverSocket.BeginAccept(asyncResult =>
            {
                try
                {
                    Socket clientSocket = _serverSocket.EndAccept(asyncResult);
                    _clients.Add(clientSocket);
                    string clientIP = clientSocket.RemoteEndPoint.ToString();
                    Console.WriteLine($"客户端连接: {clientIP}");
                    OnClientConnected?.Invoke(clientIP);

                    AcceptClient(); // 继续监听新客户端
                    ReceiveMessage(clientSocket); // 监听该客户端的消息
                }
                catch (Exception ex)
                {
                    OnError?.Invoke($"接受客户端失败: {ex.Message}");
                }
            }, null);
        }

        private void ReceiveMessage(Socket client)
        {
            byte[] buffer = new byte[BufferSize];
            try
            {
                client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, asyncResult =>
                {
                    try
                    {
                        int receivedLength = client.EndReceive(asyncResult);
                        if (receivedLength > 0)
                        {
                            byte[] receivedData = new byte[receivedLength];
                            Array.Copy(buffer, receivedData, receivedLength); // 复制有效数据
                            Console.WriteLine($"收到客户端消息: {BitConverter.ToString(receivedData)}");
                            OnMessageReceived?.Invoke(client.RemoteEndPoint.ToString(), BitConverter.ToString(receivedData));

                            
                            SendMessage(client, receivedData);

                            // 继续接收新的消息
                            ReceiveMessage(client);
                        }
                        else
                        {
                            DisconnectClient(client);
                        }
                    }
                    catch (Exception ex)
                    {
                        OnError?.Invoke($"接收消息异常: {ex.Message}");
                        DisconnectClient(client);
                    }
                }, null);
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"客户端连接异常: {ex.Message}");
                DisconnectClient(client);
            }
        }

        public void SendMessage(Socket client, byte[] receivedData)
        {
            try
            {
                // 解析并自定义返回数据
                byte[] response = GenerateResponse(receivedData);

                client.BeginSend(response, 0, response.Length, SocketFlags.None, asyncResult =>
                {
                    try
                    {
                        int sent = client.EndSend(asyncResult);
                        Console.WriteLine($"发送消息成功: {BitConverter.ToString(response)}");
                    }
                    catch (Exception ex)
                    {
                        OnError?.Invoke($"发送消息失败: {ex.Message}");
                    }
                }, null);
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"发送数据异常: {ex.Message}");
            }
        }*/

        public async Task StartWoLiuAsync()
        {
            try
            {
                _serverSocket.Bind(new IPEndPoint(IPAddress.Loopback, 6065));
                _serverSocket.Listen(10);
                _isRunning = true;
                Console.WriteLine("服务器已启动，等待客户端连接...");

                while (_isRunning)
                {
                    Socket client = await AcceptClientAsync();
                    if (client != null)
                    {
                        _ = HandleClientAsync(client);
                    }
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"服务器启动失败: {ex.Message}");
            }
        }

        private async Task<Socket?> AcceptClientAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    Socket clientSocket = _serverSocket.Accept();
                    _clients.Add(clientSocket);
                    string clientIP = clientSocket.RemoteEndPoint?.ToString() ?? "未知客户端null"; // 检查 RemoteEndPoint 是否为 null
                    Console.WriteLine($"客户端连接: {clientIP}");
                    OnClientConnected?.Invoke(clientIP);
                    return clientSocket;
                }
                catch (Exception ex)
                {
                    OnError?.Invoke($"接受客户端失败: {ex.Message}");
                    return null;
                }
            });
        }

        private async Task HandleClientAsync(Socket client)
        {
            while (client.Connected && _isRunning)
            {
                byte[] receivedData = await ReceiveMessageAsync(client);
                if (receivedData != null && receivedData.Length > 0)
                {
                    SendMessage(client, receivedData);
                }
                else
                {
                    DisconnectClient(client);
                    break;
                }
            }
        }

/*        private async Task<byte[]> ReceiveMessageAsync(Socket client)
        {
            return await Task.Run(() =>
            {
                byte[] buffer = new byte[BufferSize];
                try
                {
                    int receivedLength = client.Receive(buffer);
                    if (receivedLength > 0)
                    {
                        byte[] receivedData = new byte[receivedLength];
                        Array.Copy(buffer, receivedData, receivedLength);
                        Console.WriteLine($"收到客户端消息: {BitConverter.ToString(receivedData)}");
                        OnMessageReceived?.Invoke(client.RemoteEndPoint.ToString(), BitConverter.ToString(receivedData));

                        return receivedData;
                    }
                }
                catch (Exception ex)
                {
                    OnError?.Invoke($"接收消息异常: {ex.Message}");
                    DisconnectClient(client);
                }
                return null;
            });
        }*/

        private async Task<byte[]> ReceiveMessageAsync(Socket client)
        {
            byte[] buffer = new byte[BufferSize];
            try
            {
                int receivedLength = await client.ReceiveAsync(buffer, SocketFlags.None);
                if (receivedLength > 0)
                {
                    byte[] receivedData = new byte[receivedLength];
                    Array.Copy(buffer, receivedData, receivedLength);
                    Console.WriteLine($"收到客户端消息: {BitConverter.ToString(receivedData)}");
                    OnMessageReceived?.Invoke(client.RemoteEndPoint.ToString(), BitConverter.ToString(receivedData));
                    return receivedData;
                }
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"接收消息异常: {ex.Message}");
            }
            return null;
        }


        public void SendMessage(Socket client, byte[] receivedData)
        {
            try
            {
                byte[] response = GenerateResponse(receivedData);
                client.Send(response);
                Console.WriteLine($"发送消息成功: {BitConverter.ToString(response)}");
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"发送消息失败: {ex.Message}");
            }
        }

        private void DisconnectClient(Socket client)
        {
            if (client != null && client.Connected)
            {
                string clientIP = client.RemoteEndPoint.ToString();
                Console.WriteLine($"客户端断开连接: {clientIP}");
                OnClientDisconnected?.Invoke(clientIP);
                _clients.Remove(client);
                client.Shutdown(SocketShutdown.Both);
                client.Close();
            }
        }

        public void Stop()
        {
            _isRunning = false;
            foreach (var client in _clients)
            {
                DisconnectClient(client);
            }
            _serverSocket.Close();
            Console.WriteLine("服务器已关闭");
        }

        /// <summary>
        /// 计算 CheckSum
        /// </summary>
        /// <param name="id">参数 ID</param>
        /// <param name="values">参数值</param>
        /// <returns>CheckSum 值</returns>
        private byte CalculateCheckSum(byte id, byte[] values)
        {
            byte checkSum = id;
            foreach (byte value in values)
            {
                checkSum ^= value;
            }
            return checkSum;
        }


        /// <summary>
        /// 验证 CheckSum
        /// </summary>
        /// <param name="data">完整的数据包</param>
        /// <returns>true 表示数据有效，false 表示数据无效</returns>
        private byte GetCheckSumBit(byte[] data)
        {
            // 数据包格式：Head(2) + ClientID(1) + Len_Total(1) + ID(1) + Value1(1) + Value2(N) + CheckSum(1)
            if (data.Length < 7)
            {
                throw new ArgumentException("数据包长度不足");
            }

            // 提取 ID 和 Value
            byte id = data[4];
            byte[] values = new byte[data.Length - 7];
            Array.Copy(data, 5, values, 0, values.Length);

            // 计算 CheckSum
            byte calculatedCheckSum = CalculateCheckSum(id, values);

/*            // 获取数据包中的 CheckSum
            byte receivedCheckSum = data[data.Length - 1];
*/
            // 返回校验和的最低有效位（即最低的 bit）
            return (byte)(calculatedCheckSum & 0x01);  // 只返回最低的 bit
        }



        /// <summary>
        /// 解析收到的数据，并根据不同情况返回自定义数据
        /// </summary>
        private byte[] GenerateResponse(byte[] receivedData)
        {
            // 示例：客户端发送 "FE 55 AA 02 00 E0"
            // 服务器返回 "FD 55 AA 02 00 F0"
            // 调用 ReadDRegisterAsync 获取字节数组


            if (receivedData.Length == 7 &&
                receivedData[0] == 0xFE &&
                receivedData[1] == 0x55 &&
                receivedData[2] == 0xAA &&
                receivedData[3] == 0x02 &&
                receivedData[4] == 0x80 &&
                receivedData[5] == 0xE0)
            {
                // 获取校验和的最低有效位
                byte checkSumBit = GetCheckSumBit(receivedData);
                List<byte> response = new List<byte> { 0xFD, 0x55, 0xAA, 0x02, 0x00, 0xF0 };
                response.Add(checkSumBit);
                return response.ToArray();
            }
            else if(receivedData.Length == 7 &&
                receivedData[0] == 0xFE &&
                receivedData[1] == 0x55 &&
                receivedData[2] == 0xBB &&
                receivedData[3] == 0x02 &&
                receivedData[4] == 0x80 &&
                receivedData[5] == 0xE0)
            {
                // 获取校验和的最低有效位
                byte checkSumBit = GetCheckSumBit(receivedData);
                List<byte> response = new List<byte> { 0xFD, 0x55, 0xAA, 0x02, 0x00, 0xF0 };
                response.Add(checkSumBit);
                return response.ToArray();
            }
            //AA样棒扫码
            else if (receivedData[0] == 0xFE &&
                receivedData[1] == 0x55 &&
                receivedData[2] == 0xAA &&
                receivedData[3] == 0x02 &&
                receivedData[4] == 0x80 &&
                receivedData[5] == 0xE0 )
            {

                // 获取条码长度字节数组
                byte[] barcodeLength = _scanGangBasic.GetBarcodeLength();

                // 获取条码字节数组
                byte[] barcodeBytes = _scanGangBasic.GetBarcodeBytes();

                // 获取校验和的最低有效位
                byte checkSumBit = GetCheckSumBit(receivedData);

                // 返回拼接后的字节数组
                List<byte> response = new List<byte> { 0xFD, 0x55, 0xAA, 0x02, 0x00, 0xFA };
                response.AddRange(barcodeLength);  // 添加条码长度
                response.AddRange(barcodeBytes);   // 添加条码字节

                //TODO 缺陷允许误差(Float:4Byte) 样棒缺陷数量(Int:4Byte) 缺陷位置(N * Float:4Bytes)]



                response.Add(checkSumBit);
                return response.ToArray();
            }
            //BB样棒扫码
            else if (receivedData[0] == 0xFE &&
                receivedData[1] == 0x55 &&
                receivedData[2] == 0xBB &&
                receivedData[3] == 0x02 &&
                receivedData[4] == 0x80 &&
                receivedData[5] == 0xE0 )
            {

                // 获取条码长度字节数组
                byte[] barcodeLength = _scanGangBasic.GetBarcodeLength();

                // 获取条码字节数组
                byte[] barcodeBytes = _scanGangBasic.GetBarcodeBytes();

                // 获取校验和的最低有效位
                byte checkSumBit = GetCheckSumBit(receivedData);

                // 返回拼接后的字节数组
                List<byte> response = new List<byte> { 0xFD, 0x55, 0xBB, 0x02, 0x00, 0xFA };
                response.AddRange(barcodeLength);  // 添加条码长度
                response.AddRange(barcodeBytes);   // 添加条码字节

                //TODO 缺陷允许误差(Float:4Byte) 样棒缺陷数量(Int:4Byte) 缺陷位置(N * Float:4Bytes)]



                response.Add(checkSumBit);
                return response.ToArray();
            }

            //AA产品扫码
            else if (receivedData[0] == 0xFE &&
                receivedData[1] == 0x55 &&
                receivedData[2] == 0xAA &&
                receivedData[3] == 0x02 &&
                receivedData[4] == 0x80 &&
                receivedData[5] == 0xE0 )
            {
                // 获取条码长度字节数组
                byte[] barcodeLength = _scanGangBasic.GetBarcodeLength();

                // 获取条码字节数组
                byte[] barcodeBytes = _scanGangBasic.GetBarcodeBytes();

                // 获取校验和的最低有效位
                byte checkSumBit = GetCheckSumBit(receivedData);

                // 返回拼接后的字节数组
                List<byte> response = new List<byte> { 0xFD, 0x55, 0xAA, 0x02, 0x00, 0xFB };
                response.AddRange(barcodeLength);  // 添加条码长度
                response.AddRange(barcodeBytes);   // 添加条码字节

                //TODO 批次长度(4Bytes) 批次号

                response.Add(checkSumBit);
                return response.ToArray();
            }

            //BB产品扫码
            else if (receivedData[0] == 0xFE &&
                receivedData[1] == 0x55 &&
                receivedData[2] == 0xBB &&
                receivedData[3] == 0x02 &&
                receivedData[4] == 0x80 &&
                receivedData[5] == 0xE0 )
            {
                // 获取条码长度字节数组
                byte[] barcodeLength = _scanGangBasic.GetBarcodeLength();

                // 获取条码字节数组
                byte[] barcodeBytes = _scanGangBasic.GetBarcodeBytes();

                // 获取校验和的最低有效位
                byte checkSumBit = GetCheckSumBit(receivedData);

                // 返回拼接后的字节数组
                List<byte> response = new List<byte> { 0xFD, 0x55, 0xBB, 0x02, 0x00, 0xFB };
                response.AddRange(barcodeLength);  // 添加条码长度
                response.AddRange(barcodeBytes);   // 添加条码字节

                //TODO 批次长度(4Bytes) 批次号


                response.Add(checkSumBit);
                return response.ToArray();
            }

            //上一次心跳AA收到样棒条码
            else if (receivedData[0] == 0xFE &&
                receivedData[1] == 0x55 &&
                receivedData[2] == 0xAA &&
                receivedData[3] == 0x02 &&
                receivedData[4] == 0x80 &&
                receivedData[5] == 0xE3)
            {

                // 获取校验和的最低有效位
                byte checkSumBit = GetCheckSumBit(receivedData);
                List<byte> response = new List<byte> { 0xFD, 0x55, 0xAA, 0x02, 0x00, 0xF3 };
                response.Add(checkSumBit);
                return response.ToArray();
            }
            //上一次心跳AA收到试件条码及批次
            else if (receivedData[0] == 0xFE &&
                receivedData[1] == 0x55 &&
                receivedData[2] == 0xAA &&
                receivedData[3] == 0x02 &&
                receivedData[4] == 0x80 &&
                receivedData[5] == 0xE4 )
            {
                // 获取校验和的最低有效位
                byte checkSumBit = GetCheckSumBit(receivedData);
                List<byte> response = new List<byte> { 0xFD, 0x55, 0xAA, 0x02, 0x00, 0xF4 };
                response.Add(checkSumBit);
                return response.ToArray();
            }
            //上一次心跳BB收到样棒条码
            else if (receivedData[0] == 0xFE &&
                receivedData[1] == 0x55 &&
                receivedData[2] == 0xBB &&
                receivedData[3] == 0x02 &&
                receivedData[4] == 0x80 &&
                receivedData[5] == 0xE3)
            {
                // 获取校验和的最低有效位
                byte checkSumBit = GetCheckSumBit(receivedData);
                List<byte> response = new List<byte> { 0xFD, 0x55, 0xBB, 0x02, 0x00, 0xF3 };
                response.Add(checkSumBit);
                return response.ToArray();
            }
            //上一次心跳收BB到试件条码及批次
            else if (receivedData[0] == 0xFE &&
                receivedData[1] == 0x55 &&
                receivedData[2] == 0xBB &&
                receivedData[3] == 0x02 &&
                receivedData[4] == 0x80 &&
                receivedData[5] == 0xE4 )
            {
                // 获取校验和的最低有效位
                byte checkSumBit = GetCheckSumBit(receivedData);
                List<byte> response = new List<byte> { 0xFD, 0x55, 0xBB, 0x02, 0x00, 0xF4 };
                response.Add(checkSumBit);
                return response.ToArray();
            }

            //TODO: 其他情况
            else 
            {
                return null;
            }
        }
    }
}
