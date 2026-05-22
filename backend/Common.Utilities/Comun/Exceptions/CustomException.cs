using Common.Utilities.Bases;

namespace Common.Utilities.Exceptions;


public class CustomException : Exception
{
    public MessageTypes messageType { get; set; }
    public IEnumerable<BaseError> Errors { get; }
public bool SaveLog = false;
    public CustomException() : base()
    {
        Errors = new List<BaseError>();
    }

    public CustomException(IEnumerable<BaseError> errors) : this()
    {
        Errors = errors;
    }
    public CustomException(string message, MessageTypes typeMsg) : base(message)
    {
        messageType = typeMsg;
        Errors = [];
    }
    public CustomException(string message, Exception innerException) : base(message, innerException)
    {
        Errors = [];
    }
    public CustomException(string message, Exception innerException, bool saveLog=false) : base(message, innerException)
    {
        Errors = [];
        SaveLog=saveLog;
    }
    public CustomException(string message,Exception innerException, MessageTypes typeMsg) : base(message,innerException)
    {
        messageType = typeMsg;
        Errors = [];
    }
    public CustomException(string message) : base(message)
    {
        Errors = new List<BaseError>();
    }
}
