using System;
using System.Net.Sockets;
using System.Text;

public static class RconHelper
{
    private const int RCON_TIMEOUT = 5000; // 超时时间，单位：毫秒
    private const int RCON_PACKET_MAXSIZE = 99999999; // 最大数据包大小

    /// <summary>
    /// 连接 RCON 服务器并发送命令，返回命令响应内容。
    /// </summary>
    /// <param name="ip">RCON 服务器 IP 地址。</param>
    /// <param name="port">RCON 服务器端口号。</param>
    /// <param name="password">RCON 密码。</param>
    /// <param name="command">要执行的命令。</param>
    /// <returns>命令响应内容。</returns>
    public static string ExecuteCommand(string ip, int port, string password, string command)
    {
        try
        {
            // 连接 RCON 服务器
            using (var client = new TcpClient())
            {
                client.ReceiveTimeout = RCON_TIMEOUT;
                client.SendTimeout = RCON_TIMEOUT;
                client.Connect(ip, port);

                // 发送登录请求
                var loginPacket = CreatePacket(password, RconPacketType.Login);
                SendPacket(client, loginPacket);

                // 接收登录响应
                var loginResponsePacket = ReceivePacket(client);
                if (loginResponsePacket.Type == RconPacketType.LoginFailed)
                {
                    throw new Exception("RCON 登录失败，密码错误或服务器拒绝连接。");
                }

                // 发送命令请求
                var commandPacket = CreatePacket(command, RconPacketType.Command);
                SendPacket(client, commandPacket);

                // 接收命令响应
                var commandResponsePacket = ReceivePacket(client);
                if (commandResponsePacket.Type != RconPacketType.CommandResponse)
                {
                    throw new Exception("RCON 命令执行失败。");
                }

                return commandResponsePacket.Body;
            }
        }
        catch (Exception ex)
        {
            throw new Exception("执行 RCON 命令时出错：" + ex.Message);
        }
    }

    private static RconPacket CreatePacket(string body, RconPacketType type)
    {
        var packet = new RconPacket
        {
            Type = type,
            Body = body
        };

        packet.Size = packet.Body.Length + 2; // 2 个字节的结尾符

        return packet;
    }

    private static void SendPacket(TcpClient client, RconPacket packet)
    {
        var bytes = packet.GetBytes();
        if (bytes.Length > RCON_PACKET_MAXSIZE + 4)
        {
            bytes = bytes[..(RCON_PACKET_MAXSIZE + 4)];
        }

        client.GetStream().Write(bytes, 0, bytes.Length);
    }

    private static RconPacket ReceivePacket(TcpClient client)
    {
        var buffer = new byte[RCON_PACKET_MAXSIZE];
        var stream = client.GetStream();
        var bytesRead = stream.Read(buffer, 0, buffer.Length);

        var packet = new RconPacket();
        packet.SetBytes(buffer[..bytesRead]);
        return packet;
    }
}

/// <summary>
/// Rcon 数据包类型。
/// </summary>
public enum RconPacketType
{
    Command = 2,
    CommandResponse = 0,
    Login = 3,
    LoginFailed = -1,
    LoginSuccess = 2
}

/// <summary>
/// Rcon 数据包。
/// </summary>
public class RconPacket
{
    public int Size { get; set; }
    public RconPacketType Type { get; set; }
    public string Body { get; set; }

    public byte[] GetBytes()
    {
        var bytes = new byte[Size + 4];
        var bodyBytes = Encoding.UTF8.GetBytes(Body);

        if (bodyBytes.Length > Size - 2)
        {
            bodyBytes = bodyBytes[..(Size - 2)];
        }

        BitConverter.GetBytes(Size).CopyTo(bytes, 0);
        BitConverter.GetBytes((int)Type).CopyTo(bytes, 4);
        bodyBytes.CopyTo(bytes, 8);
        bytes[Size + 7] = 0;
        bytes[Size + 6] = 0;

        return bytes;
    }

    public void SetBytes(byte[] bytes)
    {
        Size = BitConverter.ToInt32(bytes, 0);
        Type = (RconPacketType)BitConverter.ToInt32(bytes, 4);
        Body = Encoding.UTF8.GetString(bytes, 8, Size - 2);
    }
}
