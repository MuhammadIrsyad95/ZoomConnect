namespace Zoom.Helper
{
    public class MessageResult
    {

        public int ErrorType { get; set; }
        public string ErrorMessage { get; set; }
    }
    public class ResponData : MessageResult
    {
        Dictionary<string, object> dataDictionary { get; set; }
        List<Dictionary<string, object>> dataDictionaryList { get; set; }
    }


    public enum ErrorTypes
    {
        Success = 0,
        Error = 1,
        Warning = 2

    }
}
