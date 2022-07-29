using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Dan.Common.Helpers.Util;

[StructLayout(LayoutKind.Sequential)]
public struct SystemTime
{
    public short Year;
    public short Month;
    public short DayOfWeek;
    public short Day;
    public short Hour;
    public short Minute;
    public short Second;
    public short Milliseconds;
}

public static class MarshalHelper
{
    public static void ErrorCheck(bool nativeCallSucceeded)
    {
        if (!nativeCallSucceeded)
            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
    }
}

public static class DateTimeExtensions
{

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool FileTimeToSystemTime(ref long fileTime, out SystemTime systemTime);

    public static SystemTime ToSystemTime(this DateTime dateTime)
    {
        long fileTime = dateTime.ToFileTime();
        SystemTime systemTime;
        MarshalHelper.ErrorCheck(FileTimeToSystemTime(ref fileTime, out systemTime));
        return systemTime;
    }
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "NotAccessedField.Local")]
public class X509Certificate2Helper
{

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern bool CryptAcquireContextW(out IntPtr providerContext, string container, string? provider, int providerType, int flags);

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool CryptReleaseContext(IntPtr providerContext, int flags);

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool CryptGenKey(IntPtr providerContext, int algorithmId, int flags, out IntPtr cryptKeyHandle);

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool CryptDestroyKey(IntPtr cryptKeyHandle);

    [DllImport("crypt32.dll", SetLastError = true)]
    static extern bool CertStrToNameW(int certificateEncodingType, IntPtr x500, int strType, IntPtr reserved, byte[]? encoded, ref int encodedLength, out IntPtr errorString);

    [DllImport("crypt32.dll", SetLastError = true)]
    static extern IntPtr CertCreateSelfSignCertificate(IntPtr providerHandle, ref CryptoApiBlob subjectIssuerBlob, int flags, ref CryptKeyProviderInformation keyProviderInformation, IntPtr signatureAlgorithm, ref SystemTime startTime, ref SystemTime endTime, IntPtr extensions);

    [DllImport("crypt32.dll", SetLastError = true)]
    static extern bool CertFreeCertificateContext(IntPtr certificateContext);

    public static X509Certificate2 GenerateSelfSignedCertificate(string? name = "CN = Example", DateTime? startTime = null, DateTime? endTime = null)
    {
        if (name == null)
            name = string.Empty;
        if (startTime == null || (DateTime)startTime < DateTime.FromFileTimeUtc(0))
            startTime = DateTime.FromFileTimeUtc(0);
        var startSystemTime = ((DateTime)startTime).ToSystemTime();
        if (endTime == null)
            endTime = DateTime.MaxValue;
        var endSystemTime = ((DateTime)endTime).ToSystemTime();
        string containerName = Guid.NewGuid().ToString();
        GCHandle dataHandle = new GCHandle();
        IntPtr providerContext = IntPtr.Zero;
        IntPtr cryptKey = IntPtr.Zero;
        IntPtr certificateContext = IntPtr.Zero;
        IntPtr algorithmPointer = IntPtr.Zero;

        //RuntimeHelpers.PrepareConstrainedRegions();

        try
        {
            MarshalHelper.ErrorCheck(CryptAcquireContextW(out providerContext, containerName, null, 1, 0x8));
            MarshalHelper.ErrorCheck(CryptGenKey(providerContext, 1, 0x8000001, out cryptKey));
            IntPtr errorStringPtr;
            int nameDataLength = 0;
            byte[]? nameData;
            dataHandle = GCHandle.Alloc(name, GCHandleType.Pinned);
            if (!CertStrToNameW(0x00010001, dataHandle.AddrOfPinnedObject(), 3, IntPtr.Zero, null, ref nameDataLength, out errorStringPtr))
            {
                string? error = Marshal.PtrToStringUni(errorStringPtr);
                throw new ArgumentException(error);
            }
            nameData = new byte[nameDataLength];
            if (!CertStrToNameW(0x00010001, dataHandle.AddrOfPinnedObject(), 3, IntPtr.Zero, nameData, ref nameDataLength, out errorStringPtr))
            {
                string? error = Marshal.PtrToStringUni(errorStringPtr);
                throw new ArgumentException(error);
            }
            dataHandle.Free();
            dataHandle = GCHandle.Alloc(nameData, GCHandleType.Pinned);
            CryptoApiBlob nameBlob = new CryptoApiBlob { cbData = nameData.Length, pbData = dataHandle.AddrOfPinnedObject() };
            dataHandle.Free();
            CryptKeyProviderInformation keyProvider = new CryptKeyProviderInformation { pwszContainerName = containerName, dwProvType = 1, dwKeySpec = 1 };
            CryptAlgorithmIdentifier algorithm = new CryptAlgorithmIdentifier { pszObjId = "1.2.840.113549.1.1.13", Parameters = new CryptoApiBlob() };
            algorithmPointer = Marshal.AllocHGlobal(Marshal.SizeOf(algorithm));
            Marshal.StructureToPtr(algorithm, algorithmPointer, false);
            certificateContext = CertCreateSelfSignCertificate(providerContext, ref nameBlob, 0, ref keyProvider, algorithmPointer, ref startSystemTime, ref endSystemTime, IntPtr.Zero);
            MarshalHelper.ErrorCheck(certificateContext != IntPtr.Zero);
            return new X509Certificate2(certificateContext);
        }
        finally
        {
            if (dataHandle.IsAllocated)
                dataHandle.Free();
            if (certificateContext != IntPtr.Zero)
                CertFreeCertificateContext(certificateContext);
            if (cryptKey != IntPtr.Zero)
                CryptDestroyKey(cryptKey);
            if (providerContext != IntPtr.Zero)
                CryptReleaseContext(providerContext, 0);
            if (algorithmPointer != IntPtr.Zero)
            {
                Marshal.DestroyStructure(algorithmPointer, typeof(CryptAlgorithmIdentifier));
                Marshal.FreeHGlobal(algorithmPointer);
            }
        }
    }

    struct CryptoApiBlob
    {
        public Int32 cbData;
        public IntPtr pbData;
    }

    struct CryptAlgorithmIdentifier
    {
        public String pszObjId;
        public CryptoApiBlob Parameters;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    struct CryptKeyProviderInformation
    {
        public String pwszContainerName;
        public String pwszProvName;
        public Int32 dwProvType;
        public Int32 dwFlags;
        public Int32 cProvParam;
        public IntPtr rgProvParam;
        public Int32 dwKeySpec;
    }
}