using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
//using Furion.RemoteRequest.Extensions;
using Newtonsoft.Json.Linq;
using Flurl.Http;

namespace MineCosmos.Bot.Helper.Minecraft
{
    public static class MinecraftDataApi
    {

        public async static Task<UUIDModel> GetUuidByName(string name)
        {
            string url = $"https://api.mojang.com/users/profiles/minecraft/{name}";
            var content = await url.GetAsync().ReceiveJson<UUIDModel>();
            return content;
        }

        public async static Task<Profile> GetProfileByUuid(string uuid)
        {
            string url = $"https://sessionserver.mojang.com/session/minecraft/profile/{uuid}";

            var profile = await url.GetAsync().ReceiveJson<Profile>();

            return profile;
        }

        public async static Task<ServerInfoModel> GetServerInfo(string serverAddress, int port)
        {
            using var tcpClient = new TcpClient(serverAddress, port);
            using var networkStream = tcpClient.GetStream();
            using var bufferedStream = new BufferedStream(networkStream);
            using var writer = new BinaryWriter(bufferedStream);
            using var reader = new BinaryReader(bufferedStream);

            // 发送握手请求
            var handshakePacket = new MemoryStream();
            var handshakeWriter = new BinaryWriter(handshakePacket);

            handshakeWriter.Write((byte)0x00); // 数据包 ID
            WriteVarInt(handshakeWriter, 47); // 协议版本
            WriteVarInt(handshakeWriter, serverAddress.Length);
            handshakeWriter.Write(Encoding.ASCII.GetBytes(serverAddress));
            handshakeWriter.Write((ushort)port);
            WriteVarInt(handshakeWriter, 1); // 下一个状态为 status

            WriteVarInt(writer, (int)handshakePacket.Length);
            handshakePacket.WriteTo(bufferedStream);
            writer.Flush();

            // 发送状态请求
            WriteVarInt(writer, 1); // 数据包长度
            writer.Write((byte)0x00); // 数据包 ID
            writer.Flush();

            // 读取服务器响应
            ReadVarInt(reader); // 数据包长度
            byte packetId = reader.ReadByte(); // 数据包 ID

            if (packetId != 0x00)
            {
                throw new InvalidOperationException($"Unexpected packet ID: {packetId}");
            }

            string jsonResponse = ReadString(reader);
            Console.WriteLine(jsonResponse);

            // 将 JSON 响应解析

            var obj = JObject.Parse(jsonResponse);
            int.TryParse(obj["players"]["max"].ToString(), out int max);
            int.TryParse(obj["players"]["online"].ToString(), out int online);
            var lst = obj["description"]["extra"];
            StringBuilder sb = new StringBuilder();
            foreach (var item in lst)
            {
                sb.Append($"{item["text"]}");
            }

            string title = (sb.ToString().Replace(" ", string.Empty).Replace("\n", string.Empty).Replace("\t", string.Empty)).Trim();

            return new ServerInfoModel
            {
                Max = max,
                OnLine = online,
                Title = title,
                Version = obj["version"]["name"].ToString()
            };


        }

        private static void WriteVarInt(BinaryWriter writer, int value)
        {
            while ((value & -128) != 0)
            {
                writer.Write((byte)(value & 127 | 128));
                value >>= 7;
            }
            writer.Write((byte)value);
        }

        private static int ReadVarInt(BinaryReader reader)
        {
            int numRead = 0;
            int result = 0;
            byte read;
            do
            {
                read = reader.ReadByte();
                int value = read & 0b01111111;
                result |= value << (7 * numRead);

                numRead++;
                if (numRead > 5)
                {
                    throw new InvalidOperationException("VarInt is too big");
                }
            } while ((read & 0b10000000) != 0);

            return result;
        }

        private static string ReadString(BinaryReader reader)
        {
            int length = ReadVarInt(reader);
            byte[] stringData = reader.ReadBytes(length);
            return Encoding.UTF8.GetString(stringData);
        }

        #region 参数类

        public class ServerInfoModel
        {
            public string Title { get; set; }
            public string Version { get; set; }
            public int Max { get; set; }
            public int OnLine { get; set; }
        }

        public class UUIDModel
        {
            public string id { get; set; }
            public string name { get; set; }
        }

        public class Profile
        {
            public string id { get; set; }
            public string name { get; set; }
            public Property1[] properties { get; set; }
        }

        public class Property1
        {
            public string name { get; set; }
            public string value { get; set; }
        }

        #endregion


    }
}
