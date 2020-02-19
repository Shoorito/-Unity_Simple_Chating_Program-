using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class TransSpot_TCP : MonoBehaviour
{
    // 연결용 소켓
    private Socket m_skListener          = null;
    // 클라이언트-접속용 소켓
    private Socket m_skSocket            = null;

    // 서버 시작 여부
    private bool   m_isServer            = false;

    // 클라이언트-서버 연결 여부
    private bool   m_isConnected         = false;

    // 스레드 갱신 허용 여부
    protected bool m_isThreadLoop        = false;

    // 데이터 전송용 패킷 큐
    private PacketQueue m_pqSend         = null;

    // 데이터 수신용 패킷 큐
    private PacketQueue m_pqRecv         = null;

    // 서버 이벤트 핸들러 변수
    private EventHandler m_eventHandler  = null;

    // 데이터 송,수신용 스레드
    protected Thread m_thread            = null;

    // 데이터 송,수신 바이트 최대치
    public static int m_nTransBytesSize  = 1400;

    // 서버 이벤트 핸들러 용 델리게이트 변수(함수 리스트)
    public delegate void EventHandler(S_NetEventState nesState);

    // 서버 시작 시 초기화
    private void Start()
    {
        m_pqSend = new PacketQueue();
        m_pqRecv = new PacketQueue();
    }

    // 현재는 사용되지 않습니다.
    private void Update()
    {
        
    }

    // 서버 연결 구문

    public bool StartServer(int nPort, int nConnectionNum)
    {
        Debug.Log("서버를 시작합니다..");

        try
        {
            IPEndPoint ipEndPoint = null;

            m_skListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            ipEndPoint   = new IPEndPoint(IPAddress.Any, nPort);

            m_skListener.Bind(ipEndPoint);
            m_skListener.Listen(nConnectionNum);
        }
        catch
        {
            Debug.Log("서버를 구동하는데에 문제가 발생했습니다.");

            return false;
        }

        m_isServer = true;

        return LaunchThread();
    }

    public void StopServer()
    {
        m_isThreadLoop = false;

        if (m_thread != null)
        {
            m_thread.Join();

            m_thread = null;
        }

        Disconnect();

        if (m_skListener != null)
        {
            m_skListener.Close();

            m_skListener = null;
        }

        m_isServer = false;

        Debug.Log("서버를 정지시킵니다..");
    }

    public bool Connect(string strAddress, int nPort)
    {
        Debug.Log("\"TransportTCP\"서버를 통해 연결을 시도합니다.");

        if (m_skListener != null)
        {
            return false;
        }

        bool isThreadLaunched = false;

        try
        {
            m_skSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            m_skSocket.NoDelay = true;

            m_skSocket.Connect(strAddress, nPort);

            isThreadLaunched = LaunchThread();
        }
        catch
        {
            m_skSocket = null;
        }

        if(isThreadLaunched)
        {
            m_isConnected = true;

            Debug.Log("스레드를 통한 연결망 구축에 성공 했습니다.");
        }
        else
        {
            m_isConnected = false;

            Debug.Log("스레드를 통한 연결망 구축에 실패 했습니다..");
        }

        if (m_eventHandler != null)
        {
            // 접속 결과를 통지합니다. 

            S_NetEventState nesState = new S_NetEventState();

            nesState.m_eEventType   = E_NET_EVENT_TYPE.E_CONNECT;
            nesState.m_eEventResult = (m_isConnected) ? E_EVENT_RESULT.E_SUCCESS : E_EVENT_RESULT.E_FAILURE;

            m_eventHandler(nesState);

            Debug.Log("이벤트 핸들러에 연결 로그를 남깁니다.");
        }

        return m_isConnected;
    }

    public void Disconnect()
    {
        m_isConnected = false;

        if(m_skSocket != null)
        {
            m_skSocket.Shutdown(SocketShutdown.Both);
            m_skSocket.Close();

            m_skSocket = null;
        }

        if (m_eventHandler != null)
        {
            S_NetEventState nesState = new S_NetEventState();

            nesState.m_eEventType    = E_NET_EVENT_TYPE.E_DISCONNECT;
            nesState.m_eEventResult  = E_EVENT_RESULT.E_SUCCESS;

            m_eventHandler(nesState);
        }
    }

    public int Send(byte[] arrayData, int nSize)
    {
        if(m_pqSend == null)
        {
            return 0;
        }

        return m_pqSend.Enqueue(arrayData, nSize);
    }

    public int Receive(ref byte[] arrayBuffer, int nSize)
    {
        if(m_pqRecv == null)
        {
            return 0;
        }

        return m_pqRecv.Dequeue(ref arrayBuffer, nSize);
    }

    public void RegisterEventHandler(EventHandler eventHandler)
    {
        m_eventHandler += eventHandler;
    }

    public void UnRegisterEventHandler(EventHandler eventHandler)
    {
        m_eventHandler -= eventHandler;
    }

    // 스레드 구문

    private void AcceptClient()
    {
        if (m_skListener != null && m_skListener.Poll(0, SelectMode.SelectRead))
        {
            S_NetEventState nesState = new S_NetEventState();

            m_skSocket    = m_skListener.Accept();
            m_isConnected = true;

            nesState.m_eEventType   = E_NET_EVENT_TYPE.E_CONNECT;
            nesState.m_eEventResult = E_EVENT_RESULT.E_SUCCESS;

            m_eventHandler(nesState);

            Debug.Log("클라이언트가 접속 했습니다.");
        }
    }

    public void Dispatch()
    {
        Debug.Log("스레드를 통한 송,수신 업데이트가 시작 되었습니다.");

        while (m_isThreadLoop)
        {
            AcceptClient();

            if (m_skSocket != null && m_isConnected)
            {
                DispatchSend();

                DispatchReceive();
            }

            Thread.Sleep(5);
        }

        Debug.Log("스레드를 통한 업데이트를 종료 했습니다.");
    }

    private void DispatchSend()
    {
        try
        {
            if (m_skSocket.Poll(0, SelectMode.SelectWrite))
            {
                int nSendSize = 0;
                byte[] arrayBuffer = { };

                arrayBuffer = new byte[m_nTransBytesSize];
                nSendSize   = m_pqSend.Dequeue(ref arrayBuffer, arrayBuffer.Length);

                while (nSendSize > 0)
                {
                    m_skSocket.Send(arrayBuffer, nSendSize, SocketFlags.None);

                    nSendSize = m_pqSend.Dequeue(ref arrayBuffer, arrayBuffer.Length);
                }
            }
        }
        catch
        {
            Debug.Log("스레드를 통한 송신처리에 실패했습니다.");

            return;
        }
    }

    private void DispatchReceive()
    {
        try
        {
            while (m_skSocket.Poll(0, SelectMode.SelectRead))
            {
                int nRecvSize = 0;
                byte[] arrayBuffer = { };

                arrayBuffer = new byte[m_nTransBytesSize];
                nRecvSize = m_skSocket.Receive(arrayBuffer, arrayBuffer.Length, SocketFlags.None);

                if (nRecvSize == 0)
                {
                    Disconnect();

                    Debug.Log("클라이언트로부터 연결이 종료 되었습니다.");
                }
                else if (nRecvSize > 0)
                {
                    m_pqRecv.Enqueue(arrayBuffer, nRecvSize);
                }
            }
        }
        catch
        {
            Debug.Log("스레드를 통한 수신처리에 실패했습니다.");

            return;
        }
    }

    private bool LaunchThread()
    {
        try
        {
            m_isThreadLoop = true;

            m_thread = new Thread(new ThreadStart(Dispatch));

            m_thread.Start();
        }
        catch
        {
            Debug.Log("현재 스레드 실행이 불가합니다.");

            return false;
        }

        return true;
    }

    public bool IsServerOpen()
    {
        return m_isServer;
    }

    public bool IsConnected()
    {
        return m_isConnected;
    }

    public bool IsThreadUpdated()
    {
        return m_isThreadLoop;
    }
}
