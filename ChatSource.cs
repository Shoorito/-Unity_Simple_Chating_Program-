using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Net;
using System;

public class ChatSource : MonoBehaviour
{
    enum E_ROOM_STATE
    {
        E_NONE,
        E_SELECT_ROOM,   // 선택 룸.
        E_CHATTING_ROOM, // 채팅 룸.
        E_LEAVE,         // 나가기.
        E_ERROR,		 // 오류.
    }

    private int             m_nPrevRecvNum   = 0;
    private bool            m_isServer       = false;
    private string          m_strSendComment = "";
    private string          m_strPrevComment = "";
    private string          m_strChatMessage = "";
    private List<string>[]  m_arrayMessage   = { };
    private byte[]          m_arrayRecvData  = { };
    private E_ROOM_STATE    m_eRoomState     = E_ROOM_STATE.E_NONE;
    private E_ROOM_STATE    m_ePrevState     = E_ROOM_STATE.E_NONE;

    public int              m_nUsePort       = 50765;
    public string           m_strHostAddress = "게임 실행 후 이 문자열이 표시되면 오류입니다.";
    public TransSpot_TCP    m_TCPServer      = null;

    /* 채팅 프로그램에 사용 되는 텍스쳐 리소스 */

    // 타이틀 룸 사용 텍스쳐
    public Texture m_imgTitle               = null;
    public Texture m_imgBackGround          = null;

    // 채팅 룸 사용 텍스쳐
    public Texture m_imgBallon              = null;
    public Texture m_imgHost                = null;
    public Texture m_imgGuest               = null;

    // 채팅 박스 사용 텍스쳐
    public Texture m_imgBox                 = null;
    public Texture m_imgBox_Round_LeftUp    = null;
    public Texture m_imgBox_Round_LeftDown  = null;
    public Texture m_imgBox_Round_RightUp   = null;
    public Texture m_imgBox_Round_RightDown = null;

    public float m_fFontSize             = 13.0f;
    public float m_fTypingPongSize       = 18.0f;
    public float m_fDialogueTextureSize  = 16.0f;

    public int m_nMessageMaxLine         = 18;
    public int m_nChatingMember          = 2;

    private void Start()
    {
        IPHostEntry iphostEntry   = null;
        IPAddress   ipaddressHost = null;

        iphostEntry      = Dns.GetHostEntry(Dns.GetHostName());
        ipaddressHost    = iphostEntry.AddressList[0];
        m_arrayMessage   = new List<string>[m_nChatingMember];
        m_arrayRecvData  = new byte[1400];
        m_strHostAddress = "127.0.0.1";

        m_TCPServer.RegisterEventHandler(OnEventHandling);

        for (int nMember = 0; nMember < m_nChatingMember; nMember++)
        {
            m_arrayMessage[nMember] = new List<string>();
        }

        m_eRoomState = E_ROOM_STATE.E_SELECT_ROOM;

        Input.imeCompositionMode = IMECompositionMode.On;

        Debug.Log("Open Server");
    }

    private void Update()
    {
        switch (m_eRoomState)
        {
            case E_ROOM_STATE.E_SELECT_ROOM:
            {
                UpdateInitChating();
            }
            break;

            case E_ROOM_STATE.E_CHATTING_ROOM:
            {
                UpdateChatting();
            }
            break;

            case E_ROOM_STATE.E_LEAVE:
            {
                UpdateLeave();
            }
            break;
        }

        m_ePrevState = m_eRoomState;
    }

    private void UpdateInitChating()
    {
        if (m_ePrevState == m_eRoomState)
            return;

        for (int nMember = 0; nMember < m_nChatingMember; nMember++)
        {
            m_arrayMessage[nMember].Clear();
        }
    }

    private void UpdateChatting()
    {
        for (int nPrev = 0; nPrev < m_nPrevRecvNum; nPrev++)
        {
            m_arrayRecvData[nPrev] = 0;
        }

        m_nPrevRecvNum = m_TCPServer.Receive(ref m_arrayRecvData, m_arrayRecvData.Length);

        if (m_nPrevRecvNum > 0)
        {
            int    nId        = 0;
            string strMessage = "";

            nId        = (m_isServer == true) ? 1 : 0;
            strMessage = System.Text.Encoding.UTF8.GetString(m_arrayRecvData);

            m_strChatMessage += strMessage + '\n';

            AddMessage(ref m_arrayMessage[nId], strMessage);

            Debug.Log("Receive_Message:" + strMessage);
        }
    }

    private void UpdateLeave()
    {
        if (m_isServer == true)
        {
            m_TCPServer.StopServer();
        }
        else
        {
            m_TCPServer.Disconnect();
        }

        UpdateInitChating();

        m_eRoomState = E_ROOM_STATE.E_SELECT_ROOM;
    }

    void OnGUI()
    {
        switch (m_eRoomState)
        {
            case E_ROOM_STATE.E_SELECT_ROOM:
            {
                GUI.DrawTexture(new Rect(0, 0, 800, 600), m_imgTitle);

                CreateHostTypeGUI();
            }
            break;
            
            case E_ROOM_STATE.E_CHATTING_ROOM:
            {
                GUI.DrawTexture(new Rect(0, 0, 800, 600), m_imgBackGround);

                CreateChattingGUI();
            }
            break;
            
            case E_ROOM_STATE.E_ERROR:
            {
                GUI.DrawTexture(new Rect(0, 0, 800, 600), m_imgTitle);

                CreateErrorGUI();
            }
            break;
        }
    }

    private void CreateHostTypeGUI()
    {
        float fWidth      = 800.0f;
        float fHeight     = 600.0f;
        float fPositionX  = fWidth  * 0.5f - 100.0f;
        float fPositionY  = fHeight * 0.75f;
        Rect  rectLabel   = Rect.zero;
        Rect  rectText    = Rect.zero;

        GUIStyle guiStyle = null;

        if (GUI.Button(new Rect(fPositionX, fPositionY, 200, 30), "채팅방 만들기"))
        {
            m_TCPServer.StartServer(m_nUsePort, 1);

            m_eRoomState = E_ROOM_STATE.E_CHATTING_ROOM;
            m_isServer   = true;
        }

        guiStyle         = new GUIStyle();
        rectText         = new Rect(fPositionX, fPositionY + 100, 200, 30);
        rectLabel        = new Rect(fPositionX, fPositionY + 80, 200, 30);
        m_strHostAddress = GUI.TextField(rectText, m_strHostAddress);

        guiStyle.fontStyle        = FontStyle.Normal;
        guiStyle.normal.textColor = Color.black;

        GUI.Label(rectLabel, "상대방 IP 주소", guiStyle);

        if (GUI.Button(new Rect(fPositionX, fPositionY + 40, 200, 30), "채팅방 입장"))
        {
            bool isConnect = false;

            isConnect = m_TCPServer.Connect(m_strHostAddress, m_nUsePort);

            if (isConnect)
                m_eRoomState = E_ROOM_STATE.E_CHATTING_ROOM;
            else
                m_eRoomState = E_ROOM_STATE.E_ERROR;
        }
    }

    private void CreateChattingGUI()
    {
        bool isSent      = false;
        Rect rectComment = new Rect(220, 450, 300, 30);

        m_strSendComment = GUI.TextField(rectComment, m_strSendComment, 15);
        isSent           = GUI.Button(new Rect(530, 450, 100, 30), "대화 전송");

        if ((Event.current.isKey) && (Event.current.keyCode == KeyCode.Return))
        {
            if (m_strSendComment == m_strPrevComment)
            {
                isSent           = true;
                m_strPrevComment = "";
            }
            else
            {
                m_strPrevComment = m_strSendComment;
            }
        }

        if (isSent && m_strSendComment.Length > 0)
        {
            byte[] arrayBuffer = { };
            string strMessage  = "";

            strMessage       = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + m_strSendComment;
            arrayBuffer      = System.Text.Encoding.UTF8.GetBytes(strMessage);

            m_TCPServer.Send(arrayBuffer, arrayBuffer.Length);

            AddMessage(ref m_arrayMessage[(m_isServer) ? 0 : 1], strMessage);

            m_strSendComment = "";
        }

        if (GUI.Button(new Rect(700, 560, 80, 30), "나가기"))
        {
            m_eRoomState = E_ROOM_STATE.E_LEAVE;
        }

        // 호스트(서버 운영자) 측의 메시지 표시
        if (m_TCPServer.IsServerOpen() || !m_TCPServer.IsServerOpen() && m_TCPServer.IsConnected())
        {
            DispBalloon(ref m_arrayMessage[0], new Vector2(200.0f, 200.0f), new Vector2(340.0f, 360.0f), Color.cyan, true);

            GUI.DrawTexture(new Rect(50.0f, 370.0f, 145.0f, 200.0f), m_imgHost);
        }

        // 게스트(상대 클라이언트) 측의 메시지 표시
        if (!m_TCPServer.IsServerOpen() || m_TCPServer.IsServerOpen() && m_TCPServer.IsConnected())
        {
            DispBalloon(ref m_arrayMessage[1], new Vector2(600.0f, 200.0f), new Vector2(340.0f, 360.0f), Color.green, false);

            GUI.DrawTexture(new Rect(600.0f, 370.0f, 145.0f, 200.0f), m_imgGuest);
        }
    }

    private void CreateErrorGUI()
    {
        float sx = 800.0f;
        float sy = 600.0f;
        float px = sx * 0.5f - 150.0f;
        float py = sy * 0.5f;

        if (GUI.Button(new Rect(px, py, 300, 80), "접속에 실패했습니다.\n\n버튼을 누르세요."))
        {
            m_eRoomState = E_ROOM_STATE.E_SELECT_ROOM;
        }
    }

    private void DispBalloon(ref List<string> listMessages, Vector2 vecPos, Vector2 vecSize, Color color, bool isLeft)
    {
        // 말풍선 테두리를 설정합니다.
        DrawBallonFrame(vecPos, vecSize, color, isLeft);

        // 채팅 문장을 표시합니다. 	
        foreach (string strMessage in listMessages)
        {
            DrawText(strMessage, vecPos, vecSize);

            vecPos.y += m_fTypingPongSize;
        }
    }

    private void DrawBallonFrame(Vector2 position, Vector2 size, Color color, bool left)
    {
        Vector2 vecScale    = Vector2.zero;
        Vector2 vecPosition = Vector2.zero;

        GUI.color  = color;

        vecScale.x = size.x - m_fDialogueTextureSize * 2.0f;
        vecScale.y = size.y;

        // Center
        vecPosition = position - vecScale / 2.0f;

        GUI.DrawTexture
        (
            new Rect
            (
                vecPosition.x,
                vecPosition.y,
                vecScale.x,
                vecScale.y
            ),

            m_imgBox
        );

        // Left
        vecPosition.x = position.x - vecScale.x / 2.0f - m_fDialogueTextureSize;
        vecPosition.y = position.y - vecScale.y / 2.0f + m_fDialogueTextureSize;

        GUI.DrawTexture
        (
            new Rect
            (
                vecPosition.x,
                vecPosition.y,
                m_fDialogueTextureSize,
                size.y - m_fDialogueTextureSize * 2.0f
            ),
            
            m_imgBox
        );

        // Right
        vecPosition.x = position.x + vecScale.x / 2.0f;
        vecPosition.y = position.y - vecScale.y / 2.0f + m_fDialogueTextureSize;

        GUI.DrawTexture
        (
            new Rect
            (
                vecPosition.x,
                vecPosition.y,
                m_fDialogueTextureSize,
                size.y - m_fDialogueTextureSize * 2.0f
            ),
            
            m_imgBox
        );

        // LeftTop
        vecPosition.x = position.x - vecScale.x / 2.0f - m_fDialogueTextureSize;
        vecPosition.y = position.y - vecScale.y / 2.0f;

        GUI.DrawTexture
        (
            new Rect
            (
                vecPosition.x,
                vecPosition.y,
                m_fDialogueTextureSize,
                m_fDialogueTextureSize
            ),
            
            m_imgBox_Round_LeftUp
        );

        // RightTop
        vecPosition.x = position.x + vecScale.x / 2.0f;
        vecPosition.y = position.y - vecScale.y / 2.0f;

        GUI.DrawTexture
        (
            new Rect
            (
                vecPosition.x,
                vecPosition.y,
                m_fDialogueTextureSize,
                m_fDialogueTextureSize
            ),
            
            m_imgBox_Round_RightUp
        );

        // LeftDown
        vecPosition.x = position.x - vecScale.x / 2.0f - m_fDialogueTextureSize;
        vecPosition.y = position.y + vecScale.y / 2.0f - m_fDialogueTextureSize;

        GUI.DrawTexture
        (
            new Rect
            (
                vecPosition.x,
                vecPosition.y,
                m_fDialogueTextureSize,
                m_fDialogueTextureSize
            ),
            
            m_imgBox_Round_LeftDown
        );

        // RightDown
        vecPosition.x = position.x + vecScale.x / 2.0f;
        vecPosition.y = position.y + vecScale.y / 2.0f - m_fDialogueTextureSize;

        GUI.DrawTexture
        (
            new Rect
            (
                vecPosition.x,
                vecPosition.y,
                m_fDialogueTextureSize,
                m_fDialogueTextureSize
            ), 
            
            m_imgBox_Round_RightDown
        );

        // 말풍선 기호.
        vecPosition.x = position.x - m_fDialogueTextureSize;
        vecPosition.y = position.y + vecScale.y / 2.0f;

        GUI.DrawTexture
        (
            new Rect
            (
                vecPosition.x,
                vecPosition.y,
                m_fDialogueTextureSize,
                m_fDialogueTextureSize
            ), 
            
            m_imgBallon
        );

        GUI.color = Color.white;
    }

    private void DrawText(string strMessage, Vector2 vecPosition, Vector2 vecSize)
    {
        if (strMessage == "")
        {
            return;
        }

        Rect    recLabel         = Rect.zero;
        Vector2 vecTextSize      = Vector2.zero;
        Vector2 vecBallonSize    = Vector2.zero;
        Vector2 vecLabelPosition = Vector2.zero;
        GUIStyle guiStyle        = new GUIStyle();

        guiStyle.fontSize         = 16;
        guiStyle.normal.textColor = Color.white;

        vecTextSize.x = strMessage.Length * m_fFontSize;
        vecTextSize.y = m_fTypingPongSize;

        vecBallonSize.x = vecTextSize.x + m_fDialogueTextureSize * 2.0f;
        vecBallonSize.y = vecTextSize.y + m_fDialogueTextureSize;

        vecLabelPosition.x = vecPosition.x - vecSize.x / 2.0f + m_fDialogueTextureSize;
        vecLabelPosition.y = vecPosition.y - vecSize.y / 2.0f + m_fDialogueTextureSize;

        recLabel = new Rect(vecLabelPosition.x, vecLabelPosition.y, vecTextSize.x, vecTextSize.y);

        GUI.Label(recLabel, strMessage, guiStyle);
    }

    private void AddMessage(ref List<string> listMessages, string strDialogue)
    {
        while (listMessages.Count >= m_nMessageMaxLine)
        {
            listMessages.RemoveAt(0);
        }

        listMessages.Add(strDialogue);
    }

    void OnApplicationQuit()
    {
        if (m_TCPServer != null)
        {
            m_TCPServer.StopServer();
        }
    }

    public void OnEventHandling(S_NetEventState state)
    {
        switch (state.m_eEventType)
        {
            case E_NET_EVENT_TYPE.E_CONNECT:
            {
                if (m_TCPServer.IsServerOpen())
                {
                    AddMessage(ref m_arrayMessage[1], "'게스트'가 입장했습니다.");
                }
                else
                {
                    AddMessage(ref m_arrayMessage[0], "'호스트'와 이야기 할 수 있습니다.");
                }
            }
            break;

            case E_NET_EVENT_TYPE.E_DISCONNECT:
            {
                if (m_TCPServer.IsServerOpen())
                {
                    AddMessage(ref m_arrayMessage[0], "'게스트'가 나갔습니다.");
                }
                else
                {
                    AddMessage(ref m_arrayMessage[1], "'호스트'가 나갔습니다.");
                }
            }
            break;
        }
    }
}
