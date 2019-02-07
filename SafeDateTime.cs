using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public static class SafeDateTime
{
    /// <summary>
    /// NtpServer Default Time Offset
    /// </summary>
    readonly static DateTime timeOffset = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Default olarak telefonun zamanını kullanıyoruz (internet olmazsa vs durumlar için)
    /// defaultly use device time (if there is no internet)
    /// </summary>
    static DateTime ServerTime = DateTime.UtcNow;

    /// <summary>
    /// Oyunun orta yerinde Senkronize edilirse, bu zamana kadar Unityde geçen süreyi çıkarmak için
    /// if sync in runtime, we substract passed unity time
    /// </summary>
    static float AlreadyPassedUnityTime;

    /// <summary>
    /// Sunucudan başarılı şekilde zamanı aldıysa True olur
    /// Received Time Succesfully
    /// </summary>
    public static bool Synched { get; private set; }

    /// <summary>
    /// HILE KORUMALI SU AN ki ZAMAN
    /// CHEAT PROTECTED UTC NOW
    /// </summary>
    public static DateTime UtcNow
    {
        get
        {
            if (!Synched)
                Sync();
            return ServerTime + 
                TimeSpan.FromSeconds(Time.realtimeSinceStartup - AlreadyPassedUnityTime) + 
                Difference; // Zaman gösterimlerimlerinde standart DateTime ile aynı değeri göstermesi için
                            // To avoid time jump
        }
    }

    /// <summary>
    /// Sunucu ile Aygıt arasındaki zaman farkı
    /// Time Difference Between Server and Device
    /// </summary>
    public static TimeSpan Difference { get; private set; }

    public static void Sync()
    {
        double mSec = 0;
        if(NtpTime(out mSec))
        {
            /*Debug.Log(mSec);
            Debug.Log( LocalTime());
            Debug.Log( TimeSpan.FromMilliseconds(mSec));*/
            ServerTime = timeOffset + TimeSpan.FromMilliseconds(mSec);
            Difference = DateTime.UtcNow - ServerTime;
            Synched = true;
            AlreadyPassedUnityTime = Time.unscaledTime;
            Debug.Log("Time Synched: Difference > " + Difference);
        }
        else
            Debug.LogError("Time Synched Failed !!!");
    }
    public static void SyncManuel(DateTime now)
    {
        ServerTime = now;
        Difference = DateTime.UtcNow - ServerTime;
        Synched = true;
        AlreadyPassedUnityTime = Time.unscaledTime;
        Debug.Log("Time Synched(Manuely): Difference > " + Difference);
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

            milliSeconds = (double)((intc * 1000) + ((frac * 1000) / 0x100000000L));
            //Debug.Log("NTP time " + milliSeconds);
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
        return DateTime.UtcNow.Subtract(timeOffset).TotalMilliseconds;
    }
}
