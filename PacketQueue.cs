using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

public class PacketQueue
{
    struct S_PacketInfo
    {
        public int nOffset;
        public int nSize;
    };

    private int m_nOffset = 0;

    private MemoryStream m_streamBuffer = null;

    private List<S_PacketInfo> m_listOffset = null;

    public PacketQueue()
    {
        m_listOffset    = new List<S_PacketInfo>();
        m_streamBuffer  = new MemoryStream();
    }

    public int Enqueue(byte[] arrayData, int nSize)
    {
        S_PacketInfo packetInfo = new S_PacketInfo();

        packetInfo.nOffset = m_nOffset;
        packetInfo.nSize   = nSize;

        m_listOffset.Add(packetInfo);

        m_streamBuffer.Position = m_nOffset;

        m_streamBuffer.Write(arrayData, 0, nSize);
        m_streamBuffer.Flush();

        m_nOffset += nSize;

        return nSize;
    }

    public int Dequeue(ref byte[] buffer, int nSize)
    {
        if (m_listOffset.Count <= 0)
            return -1;

        int nRecvSize = 0;
        int nDataSize = 0;
        S_PacketInfo packInfo;

        packInfo                = m_listOffset[0];
        nDataSize               = Math.Min(nSize, packInfo.nSize);
        m_streamBuffer.Position = packInfo.nOffset;
        nRecvSize               = m_streamBuffer.Read(buffer, 0, nDataSize);

        if (nRecvSize > 0)
        {
            m_listOffset.RemoveAt(0);
        }

        if (m_listOffset.Count == 0)
        {
            Clear();

            m_nOffset = 0;
        }

        return nRecvSize;
    }

    public void Clear()
    {
        byte[] buffer = { };

        buffer = m_streamBuffer.GetBuffer();

        Array.Clear(buffer, 0, buffer.Length);

        m_streamBuffer.Position = 0;
        m_streamBuffer.SetLength(0);
    }
}