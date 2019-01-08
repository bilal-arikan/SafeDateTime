using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public static class SafeDateTime
{
    /// <summary>
    /// NtpServer Time Offset
    /// </summary>
    readonly static DateTime timeOffset = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Default olarak telefonun zamanını kullanıyoruz (internet olmazsa vs durumlar için)
    /// </summary>
    static DateTime ServerTime = DateTime.UtcNow;

    /// <summary>
    /// Oyunun orta yerinde Senkronize edilirse, bu zamana kadar geçen süreyi çıkarmak için
    /// </summary>
    static float AlreadyPassedUnityTime;

    /// <summary>
    /// Sunucudan başarılı şekilde zamanı aldıysa True olur
    /// </summary>
    public static bool Synched { get; private set; }

    public static DateTime UtcNow
    {
        get
        {
            if (!Synched)
                Sync();
            return ServerTime + 
                TimeSpan.FromSeconds(Time.realtimeSinceStartup - AlreadyPassedUnityTime) + 
                Difference; // Zaman gösterimlerimlerinde standart DateTime ile aynı zamanı göstermesi için
        }
    }

    /// <summary>
    /// Sunucu ile Aygıt arasındaki zaman farkı
    /// </summary>
    public static TimeSpan Difference { get; private set; }

    /*private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
            Debug.Log(Time.unscaledTime + "\n" + Time.realtimeSinceStartup + "\n" + DateTime.UtcNow + "\n");
        if (Input.GetKeyDown(KeyCode.Y))
            Debug.Log(Time.time + " : " + Time.fixedTime);
        if (Input.GetKeyDown(KeyCode.U))
            Debug.Log(DDateTime.UtcNow);
    }*/

    public static void Sync()
    {
        /*API.Manager.GetConfiguration().Then((resp) =>
        {
            if (resp.Success)
            {
            }
        });*/
        double mSec = 0;
        if(NtpTime(out mSec))
        {
            /*Debug.Log(mSec);
            Debug.Log( DateTime.MinValue );
            Debug.Log(LocalTime());
            Debug.Log( TimeSpan.FromMilliseconds(mSec));*/
            ServerTime = timeOffset + TimeSpan.FromMilliseconds(mSec);//resp.Seconds gibi bişey olacak
            Difference = DateTime.UtcNow - ServerTime;
            Synched = true;
            AlreadyPassedUnityTime = Time.unscaledTime;
            Debug.Log("Time Synched: Difference > " + Difference);
        }
        else
            Debug.LogError("Time Synched Failed !!!");
    }

    public static bool IsCheating(float AcceptableTimeDifference = 36000)
    {
        if ((DateTime.UtcNow - UtcNow).TotalSeconds < 0)
            return -(DateTime.UtcNow - UtcNow).TotalSeconds > AcceptableTimeDifference;
        else
            return (DateTime.UtcNow - UtcNow).TotalSeconds > AcceptableTimeDifference;
    }

    public static bool NtpTime(out double milliSeconds)
    {
        try
        {
            byte[] ntpData = new byte[48];

            //LeapIndicator = 0 (no warning), VersionNum = 3 (IPv4 only), Mode = 3 (Client Mode)
            ntpData[0] = 0x1B;

            IPAddress[] addresses = Dns.GetHostEntry("pool.ntp.org").AddressList;
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            socket.Connect(new IPEndPoint(addresses[0], 123));
            socket.ReceiveTimeout = 1000;

            socket.Send(ntpData);
            socket.Receive(ntpData);
            socket.Close();

            ulong intc = (ulong)ntpData[40] << 24 | (ulong)ntpData[41] << 16 | (ulong)ntpData[42] << 8 | (ulong)ntpData[43];
            ulong frac = (ulong)ntpData[44] << 24 | (ulong)ntpData[45] << 16 | (ulong)ntpData[46] << 8 | (ulong)ntpData[47];

            Debug.Log("NTP time " + intc + " " + frac);
            milliSeconds = (double)((intc * 1000) + ((frac * 1000) / 0x100000000L));
            return true;
        }
        catch (Exception exception)
        {
            Debug.Log("Could not get NTP time");
            Debug.Log(exception);
            milliSeconds = LocalTime();
            return false;
        }
    }

    static double LocalTime()
    {
        return DateTime.UtcNow.Subtract(new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
    }
}
