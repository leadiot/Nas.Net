namespace Com.Scm.Nas.Sync.Dvo
{
    public class PostLogResult
    {
        public bool success { get; set; }
        public int code { get; set; }
        public string message { get; set; }

        public static PostLogResult Success()
        {
            return new PostLogResult { success = true };
        }

        public static PostLogResult Failure(string message)
        {
            return new PostLogResult { success = false, message = message };
        }

        public static PostLogResult Failure(int code, string message)
        {
            return new PostLogResult { success = false, code = code, message = message };
        }
    }
}
