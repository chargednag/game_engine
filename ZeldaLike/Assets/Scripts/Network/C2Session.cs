﻿using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor.PackageManager;
using UnityEngine;

enum SessionState
{
    Uninitialized, // session 생성 이후 초기화 되지 않음.
    Initialized, 
    ConnectingToServer,
    Connected,
    Disconecting,
    Disconnected,
}

enum DisconnectReason
{
    None,
    DisconnectByServer,
    TimeoutDisconnect,
    Exception,
    DISCONECTING,
    DISCONNECTED,
}


public class C2Session //: MonoBehaviour
{
    private Socket              socket      = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    private C2PayloadVector     recvBuffer  = new C2PayloadVector();
    private C2PayloadVector     sendBuffer  = new C2PayloadVector();
    private Int64               recvBytes;
    private Int64               sendBytes;
    private IAsyncResult        retAsync;
    private SessionState        state       = SessionState.Uninitialized;
    private Int64               uniqueSessionId;
    private Int32               reconnectCount;
    private C2PacketHandler     handler     = new InitialiPacketHandler();
    private C2Client            client;


    public C2Session(C2Client client)
    {
        OnInit();
        this.client = client;
    }


    public void Service()
    {
        switch (state)
        {
            case SessionState.Uninitialized:
                break;

            case SessionState.Initialized:
                Connect();
                break;

            case SessionState.ConnectingToServer:
                ReConnect();
                break;

            case SessionState.Connected:
            {
                RecvPayload();
                SendPayload();
                break;
            }
            case SessionState.Disconecting:
                break;

            case SessionState.Disconnected:
                break;

            default:
                break;
        }

        //Debug.Log(state);
    }

    internal void SendPacket<T>(T packet)
    {
        sendBuffer.Wirte<T>(packet);
    }

    private void OnInit()
    {
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
        socket.Blocking = false;
        socket.NoDelay = true;

        this.state = SessionState.Initialized;
    }

    public void ReConnect()
    {
        /// 여러번 시도해서 안되면 안되는거임.
    }

    public void Connect()
    {
        retAsync = socket.BeginConnect(NetworkManager.serverIP, NetworkManager.serverPort, new AsyncCallback(OnConnectComplete), this);
        
        this.state = SessionState.ConnectingToServer;

        return;
    }

    public void TryReconnect()
    {
        /// 여러번 시도해서 안되면 안되는거임.
        //retSync.IsCompleted;
        
        //reconnectCount += 1;
        if(reconnectCount == 5)
        {
            socket.EndConnect(retAsync);

            this.state = SessionState.Disconnected;
        }

        return;
    }

    public void Disconnect()
    {
        socket.BeginDisconnect(true, OnDisconnectComplete(), null);

        return;
    }

    public void SendPayload()
    {
        if(sendBuffer.Empty) // 보낼것 없으면...
        {
            return;
        }

        int sentBytes = socket.Send(sendBuffer.GetBuffer(), sendBuffer.ReadHead, sendBuffer.Size, SocketFlags.None);
        sendBuffer.MoveReadHead(sentBytes);
        sendBuffer.Rewind();
        Debug.Log($"Sent bytes = { sentBytes } bytes");
    }

    public void RecvPayload()
    {
        if (true == socket.Poll(0, SelectMode.SelectRead) ) // 데이터를 읽을 수 있다면 ...
        {
            SocketError error;
            Int32 receivedBytes = socket.Receive(recvBuffer.GetBuffer(), recvBuffer.WriteHead, recvBuffer.FreeSize, SocketFlags.None, out error);
            if(0 < receivedBytes)
            {
                recvBuffer.MoveWriteHead(receivedBytes);

                OnRecv();
            }
        }
    }

    public void OnRecv()
    {
        PacketHeader header = default;
        Int32 headerSize = Marshal.SizeOf<PacketHeader>();
        Int32 readBytes = 0;

        for (;;)
        {
            if ( headerSize != recvBuffer.Peek<PacketHeader>(out header, headerSize) )
                break;

            if(header.size > recvBuffer.Size)
                break;

            // 범위 체크..
            handler[header.type](header, this.recvBuffer, this);

            recvBuffer.MoveReadHead(readBytes);
        }

        recvBuffer.Rewind();
    }

    public void ParsePacket(PacketHeader header)
    {
        switch (header.type)
        {
            case PacketType.S2C_LOGIN_OK:
            {
                sc_packet_login_ok loginPayload;
                break;
            }

            case PacketType.S2C_MOVE:
            {
                sc_packet_enter movePayload;
                break;
            }

            case PacketType.S2C_ENTER:
            {
                sc_packet_enter enterPayload;
                break;
            }

            case PacketType.S2C_LEAVE:
            {
                sc_packet_enter leavePayload;
                break;
            }
        }
    }

    public static void OnConnectComplete(IAsyncResult ar)
    {
        C2Session session = (C2Session)ar.AsyncState;

        Debug.Log($"OnConnectComplete : { session.socket.Connected }");

        if (session.socket.Connected == true)
        {
            session.state = SessionState.Connected;
            session.handler = new InitialiPacketHandler();

            cs_packet_login loginPacket;
            loginPacket.header.type = PacketType.C2S_LOGIN;
            loginPacket.header.size = (sbyte)Marshal.SizeOf(typeof(cs_packet_login));
            //byte[] utf16Bytes = Encoding.UTF8.GetBytes(session.client.nickname);
            //Marshal.Copy(utf16Bytes, loginPacket.name, , 50);

            session.SendPacket(loginPacket);
        }
        else
        {
            if ( ++session.reconnectCount == 10) 
                session.state = SessionState.Disconnected;
            else
                session.state = SessionState.Initialized;
        }
    }

    public AsyncCallback OnDisconnectComplete()
    {
        AsyncCallback async = default;

        this.state = SessionState.Disconnected;

        this.handler = null;

        return async;
    }

}
