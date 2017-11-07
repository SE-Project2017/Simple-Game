namespace Assets.Test.Scripts
{
    public class GlobalContext
    {
        public bool IsServer;
        public bool IsClient;

        private static GlobalContext sInstance;

        public static GlobalContext Instance
        {
            get { return sInstance ?? (sInstance = new GlobalContext()); }
        }

        protected GlobalContext() { }
    }
}
