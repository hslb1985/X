﻿using System;
using System.IO;
using NewLife.Data;
using NewLife.Log;
using NewLife.Messaging;

namespace NewLife.Remoting
{
    /// <summary>编码器</summary>
    public interface IEncoder
    {
        /// <summary>编码 请求/响应</summary>
        /// <param name="action"></param>
        /// <param name="code"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Packet Encode(String action, Int32 code, Packet value);

        /// <summary>创建请求</summary>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        Packet CreateRequest(String action, Object args);

        IMessage CreateResponse(IMessage msg, String action, Int32 code, Object value);

        /// <summary>解码 请求/响应</summary>
        /// <param name="msg">消息</param>
        /// <param name="action">服务动作</param>
        /// <param name="code">错误码</param>
        /// <param name="value">参数或结果</param>
        /// <returns></returns>
        Boolean Decode(IMessage msg, out String action, out Int32 code, out Packet value);

        /// <summary>编码 请求/响应</summary>
        /// <param name="action">服务动作</param>
        /// <param name="code">错误码</param>
        /// <param name="value">参数或结果</param>
        /// <returns></returns>
        Packet Encode(String action, Int32 code, Object value);

        /// <summary>解码</summary>
        /// <param name="action"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        Object Decode(String action, Packet data);

        /// <summary>转换为对象</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        T Convert<T>(Object obj);

        /// <summary>转换为目标类型</summary>
        /// <param name="obj"></param>
        /// <param name="targetType"></param>
        /// <returns></returns>
        Object Convert(Object obj, Type targetType);

        /// <summary>日志提供者</summary>
        ILog Log { get; set; }
    }

    /// <summary>编码器基类</summary>
    public abstract class EncoderBase
    {
        #region 编码/解码
        /// <summary>编码 请求/响应</summary>
        /// <param name="action"></param>
        /// <param name="code"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual Packet Encode(String action, Int32 code, Packet value)
        {
            var ms = new MemoryStream();
            ms.Seek(8, SeekOrigin.Begin);

            // 请求：action + args
            // 响应：action + code + result
            var writer = new BinaryWriter(ms);
            writer.Write(action);
            if (code != 0) writer.Write(code);

            // 参数或结果
            var pk2 = value as Packet;
            if (pk2 != null && pk2.Data != null)
            {
                var len = pk2.Total;

                // 不管有没有附加数据，都会写入长度
                writer.Write(len);
            }

            var pk = new Packet(ms.GetBuffer(), 8, (Int32)ms.Length - 8);
            if (pk2 != null && pk2.Data != null) pk.Next = pk2;

            return pk;
        }

        /// <summary>创建请求</summary>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual Packet CreateRequest(String action, Object args)
        {
            // 二进制优先
            if (args is Packet pk)
            {
            }
            else if (args is Byte[] buf)
                pk = new Packet(buf);
            else
                pk = (this as IEncoder).Encode(action, 0, args);
            pk = Encode(action, 0, pk);

            return pk;
        }

        /// <summary>解码 请求/响应</summary>
        /// <param name="msg">消息</param>
        /// <param name="action">服务动作</param>
        /// <param name="code">错误码</param>
        /// <param name="value">参数或结果</param>
        /// <returns></returns>
        public virtual Boolean Decode(IMessage msg, out String action, out Int32 code, out Packet value)
        {
            action = null;
            code = 0;
            value = null;

            // 请求：action + args
            // 响应：action + code + result
            var ms = msg.Payload.GetStream();
            var reader = new BinaryReader(ms);

            action = reader.ReadString();
            if (msg.Reply && msg.Error) code = reader.ReadInt32();
            if (action.IsNullOrEmpty()) throw new Exception("解码错误，无法找到服务名！");

            // 参数或结果
            if (ms.Length > ms.Position)
            {
                var len = reader.ReadInt32();
                if (len > 0) value = msg.Payload.Slice((Int32)ms.Position, len);
            }

            return true;
        }
        #endregion

        #region 日志
        /// <summary>日志提供者</summary>
        public ILog Log { get; set; } = Logger.Null;

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public virtual void WriteLog(String format, params Object[] args) => Log?.Info(format, args);
        #endregion
    }
}