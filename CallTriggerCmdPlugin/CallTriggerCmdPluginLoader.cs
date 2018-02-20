namespace TCX.CallTriggerCmd
{
    [MyPhonePlugins.CRMPluginLoader]
    public class CallTriggerCmdPluginLoader
    {
        private static object lockObj = new object();
        private static CallTriggerCmdPlugin instance = null;

        [MyPhonePlugins.CRMPluginInitializer]
        public static void Loader(MyPhonePlugins.IMyPhoneCallHandler callHandler)
        {
            lock (lockObj)
            {
                instance = new CallTriggerCmdPlugin(callHandler);
                instance.StartServiceAsync();
            }
        }

        [MyPhonePlugins.CRMPluginDispose]
        public static void Unloader()
        {
            lock(lockObj)
            {
                if ( instance != null )
                {
                    instance.Dispose();
                    instance = null;
                }
            }
        }

    }
}
