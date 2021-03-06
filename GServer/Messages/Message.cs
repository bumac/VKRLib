﻿using System;
using System.IO;
using GServer.Connection;
using GServer.Containers;

// ReSharper disable ArrangeAccessorOwnerBody

namespace GServer.Messages
{
    /// <summary>
    /// Default message types used in internal host logic
    /// </summary>
    [Reserve(0, 40)]
    public enum MessageType : short
    {
        Empty = 0,
        Handshake = 1,
        Ack = 2,
        Token = 3,
        Ping = 4,
        Resend = 5,
        SendToEndPoint = 6
    }

    /// <summary>
    /// Reliable udp mods
    /// </summary>
    [Flags]
    public enum Mode : byte
    {
        None = 0x0,
        Reliable = 0x1,
        Sequenced = 0x2,
        Ordered = 0x4,
    }

    internal class MessageHeader : ISerializable
    {
        public short Type { get; private set; }
        private Mode _mode;
        public Token ConnectionToken { get; internal set; }
        internal MessageCounter MessageId { get; set; }

        internal bool Reliable {
            get { return (_mode & Mode.Reliable) == Mode.Reliable; }
        }

        internal bool Sequenced {
            get { return (_mode & Mode.Sequenced) == Mode.Sequenced; }
        }

        internal bool Ordered {
            get { return (_mode & Mode.Ordered) == Mode.Ordered; }
        }

        private MessageHeader() { }

        public MessageHeader(short type, Mode mode) {
            Type = type;
            _mode = mode;
        }

        public byte[] Serialize() {
            using (var m = new MemoryStream()) {
                using (var writer = new BinaryWriter(m)) {
                    writer.Write(Type);
                    writer.Write((byte) _mode);
                    if (Type != (short) MessageType.Empty && Type != (short) MessageType.Handshake) {
                        writer.Write(ConnectionToken.Serialize());
                    }
                    writer.Write(MessageId.ToShort());
                }
                return m.ToArray();
            }
        }

        public static MessageHeader Deserialize(MemoryStream m) {
            var result = new MessageHeader();
            var reader = new BinaryReader(m);
            result.Type = reader.ReadInt16();
            result._mode = (Mode) reader.ReadByte();
            if (result.Type != (short) MessageType.Empty && result.Type != (short) MessageType.Handshake) {
                result.ConnectionToken = new Token(reader.ReadString());
            }
            result.MessageId = reader.ReadInt16();
            return result;
        }
    }

    public class Message : ISerializable
    {
        internal MessageHeader Header { get; private set; }

        internal Token ConnectionToken {
            get { return Header.ConnectionToken; }
            set { Header.ConnectionToken = value; }
        }

        internal MessageCounter MessageId {
            get { return Header.MessageId; }
            set { Header.MessageId = value; }
        }

        internal bool Reliable {
            get { return Header.Reliable; }
        }

        internal bool Ordered {
            get { return Header.Ordered; }
        }

        internal bool Sequenced {
            get { return Header.Sequenced; }
        }

        public byte[] Body { get; set; }

        public byte[] Serialize() {
            using (var m = new MemoryStream()) {
                using (var writer = new BinaryWriter(m)) {
                    writer.Write(Header.Serialize());
                    if (Body != null) {
                        writer.Write(Body);
                    }
                }
                return m.ToArray();
            }
        }

        public static Message Deserialize(byte[] buffer) {
            var result = new Message();
            using (var m = new MemoryStream(buffer)) {
                using (var reader = new BinaryReader(m)) {
                    result.Header = (MessageHeader.Deserialize(m));
                    result.Body = reader.ReadBytes((int) (m.Length - m.Position));
                }
            }
            return result;
        }

        public Message(short type, Mode mode, ISerializable body) {
            Header = new MessageHeader(type, mode);
            // ReSharper disable once MergeConditionalExpression
            Body = body != null ? body.Serialize() : null;
        }

        public Message(short type, Mode mode, byte[] body) {
            Header = new MessageHeader(type, mode);
            Body = body;
        }

        public Message(short type, Mode mode) {
            Header = new MessageHeader(type, mode);
            Body = new byte[0];
        }

        private static readonly Message _handshake = new Message((short) MessageType.Handshake, Mode.None);

        // ReSharper disable once ConvertToAutoProperty
        internal static Message Handshake {
            get { return _handshake; }
        }

        internal static Message Ack(short type, MessageCounter msgId, Token conToken, int ackBitField) {
            var ds = DataStorage.CreateForWrite();
            ds.Push(ackBitField);
            ds.Push(type);
            var res = new Message((short) MessageType.Ack, Mode.None, ds) {
                MessageId = msgId,
                ConnectionToken = conToken
            };
            return res;
        }

        private Message() { }
    }
}