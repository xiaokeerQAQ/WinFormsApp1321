using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public class ReadTool
{
    private string _filePath = @"C:\system\system.ini";
    private List<string> _fileLines;

    public ReadTool()
    {
        // 缓存文件内容
        _fileLines = File.ReadAllLines(_filePath).ToList();
    }

    // 读取指定 section 下的字符串配置，并返回 byte[]
    public byte[] ReadStringAsBytes(string section, string key)
    {
        string value = ReadString(section, key);
        return string.IsNullOrEmpty(value) ? new byte[0] : Encoding.ASCII.GetBytes(value);
    }

    // 读取指定 section 下的浮动值配置，并返回 byte[]
    public byte[] ReadFloatAsBytes(string section, string key)
    {
        float value = ReadFloat(section, key);
        return BitConverter.GetBytes(value);
    }

    // 读取指定 section 下的整数配置，并返回 byte[]
    public byte[] ReadIntAsBytes(string section, string key)
    {
        int value = ReadInt(section, key);
        return BitConverter.GetBytes(value);
    }

    // 读取指定 section 下的多个整数值，并返回 byte[] 数组
    public byte[] ReadDefectPositionsAsBytes(string section, string key)
    {
        List<int> positions = ReadDefectPositions(section, key);
        List<byte> bytes = new List<byte>();
        foreach (var position in positions)
        {
            bytes.AddRange(BitConverter.GetBytes(position));
        }
        return bytes.ToArray();
    }

    // 读取指定 section 下的字符串配置
    private string ReadString(string section, string key)
    {
        bool inSection = false;
        foreach (var line in _fileLines)
        {
            if (line.StartsWith($"[{section}]"))
            {
                inSection = true;
                continue;
            }

            // 如果已进入 section 并找到目标 key
            if (inSection && line.StartsWith(key))
            {
                return line.Split('=')[1].Trim();
            }

            // 如果遇到下一个 section，退出
            if (line.StartsWith("["))
            {
                inSection = false;
            }
        }
        return string.Empty;
    }

    // 读取指定 section 下的浮动值配置
    private float ReadFloat(string section, string key)
    {
        string value = ReadString(section, key);
        if (float.TryParse(value, out float result))
        {
            return result;
        }
        return 0.0f;
    }

    // 读取指定 section 下的整数配置
    private int ReadInt(string section, string key)
    {
        string value = ReadString(section, key);
        if (int.TryParse(value, out int result))
        {
            return result;
        }
        return 0;
    }

    // 读取指定 section 下的多个整数值（缺陷位置）
    private List<int> ReadDefectPositions(string section, string key)
    {
        string value = ReadString(section, key);
        if (!string.IsNullOrEmpty(value))
        {
            return value.Split(',')
                        .Select(v => int.TryParse(v, out int result) ? result : 0)
                        .ToList();
        }
        return new List<int>();
    }
}
