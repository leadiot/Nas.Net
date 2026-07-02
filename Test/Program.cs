using Com.Scm.Utils;

internal class Program
{
    private static void Main(string[] args)
    {
        GetBasicToken();
    }

    private static void GetBasicToken()
    {
        var terminalId = 2070757555625398272L;
        var userId = 1000000000000001030;
        var accessToken = "";

        var time = TimeUtils.GetUnixTime();
        var data = terminalId + ":" + userId + ":" + time;

        var key = data + ":" + accessToken;
        var hash = TextUtils.Md5(key);

        var token = data + ":" + hash;
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        //return Basic(Convert.ToBase64String(bytes));
        token = HttpUtils.ToBase64String(bytes);
        Console.WriteLine(token);
    }
}