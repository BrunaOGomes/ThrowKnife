using UnityEngine;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class UDPReceive : MonoBehaviour
{
    Thread receiveThread;
    UdpClient client;
    volatile bool isRunning;
    public int port = 6767;
    public bool startReceiving = true;
    public bool printToConsole = false;
    public string data;

    private void OnEnable()
    {
        if (!startReceiving)
            return;

        try
        {
            client = new UdpClient(port);
        }
        catch (SocketException err)
        {
            Debug.LogError($"Could not start UDP receiver on port {port}: {err.Message}", this);
            return;
        }

        isRunning = true;
        receiveThread = new Thread(ReceiveData)
        {
            IsBackground = true
        };
        receiveThread.Start();
    }

    private void OnDisable()
    {
        isRunning = false;
        client?.Close();

        if (receiveThread != null && receiveThread.IsAlive)
            receiveThread.Join();

        client = null;
        receiveThread = null;
    }

    private void ReceiveData()
    {
        while (isRunning)
        {
            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                byte[] dataByte = client.Receive(ref anyIP);
                data = Encoding.UTF8.GetString(dataByte);

                if (printToConsole) { print(data); }
            }
            catch (SocketException) when (!isRunning)
            {
                break;
            }
            catch (ObjectDisposedException) when (!isRunning)
            {
                break;
            }
            catch (Exception err)
            {
                print(err.ToString());
                break;
            }
        }
    }
}