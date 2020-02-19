using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 이벤트 종류.
public enum E_NET_EVENT_TYPE
{
    E_CONNECT,        // 접속 됨.
    E_DISCONNECT,     // 접속이 끊김(또는 종료 됨).
    E_SEND_ERROR,      // 송신 오류 발생.
    E_RECEIVE_ERROR   // 수신 오류 발생.
}

// 이벤트 결과.
public enum E_EVENT_RESULT
{
    E_FAILURE = -1,
    E_SUCCESS = 0
}

// 이벤트 상태 통지.
public struct S_NetEventState
{
    public E_NET_EVENT_TYPE m_eEventType;	// 이벤트 타입.
    public E_EVENT_RESULT   m_eEventResult;	// 이벤트 결과.
}
