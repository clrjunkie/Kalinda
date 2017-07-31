using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Kalinda.Server
{
    internal static class HttpApi
    {
        internal static unsafe void SetRequestQueueLength(System.Net.HttpListener listener, long length)
        {
            var requestQueueHandlePropertyInfo = typeof(System.Net.HttpListener).GetProperty("RequestQueueHandle", BindingFlags.NonPublic | BindingFlags.Instance);

            if (requestQueueHandlePropertyInfo == null || requestQueueHandlePropertyInfo.PropertyType != typeof(CriticalHandle))
            {
                throw new PlatformNotSupportedException();
            }

            var requestQueueHandle = (CriticalHandle)requestQueueHandlePropertyInfo.GetValue(listener, null);
            var result = HttpSetRequestQueueProperty(
                requestQueueHandle,
                HTTP_SERVER_PROPERTY.HttpServerQueueLengthProperty,
                new IntPtr((void*)&length),
                (UInt32)Marshal.SizeOf(length),
                0,
                IntPtr.Zero
                );

            if (result != 0)
            {
                throw new Win32Exception((int)result);
            }
        }

        // https://msdn.microsoft.com/en-us/library/windows/desktop/aa364501(v=vs.85).aspx

        [DllImport("httpapi.dll", CallingConvention = CallingConvention.StdCall, SetLastError = true, ExactSpelling = true)]
        internal static extern UInt32 HttpSetRequestQueueProperty(
                                        CriticalHandle Handle,
                                        HTTP_SERVER_PROPERTY Property,
                                        IntPtr pPropertyInformation,
                                        UInt32 PropertyInformationLength,
                                        UInt32 Reserved,
                                        IntPtr pReserved);

        // https://msdn.microsoft.com/en-us/library/windows/desktop/aa364639(v=vs.85).aspx

        internal enum HTTP_SERVER_PROPERTY : uint
        {
            HttpServerAuthenticationProperty,
            HttpServerLoggingProperty,
            HttpServerQosProperty,
            HttpServerTimeoutsProperty,
            HttpServerQueueLengthProperty,
            HttpServerStateProperty,
            HttpServer503VerbosityProperty,
            HttpServerBindingProperty,
            HttpServerExtendedAuthenticationProperty,
            HttpServerListenEndpointProperty,
            HttpServerChannelBindProperty,
            HttpServerProtectionLevelProperty
        }
    }
}