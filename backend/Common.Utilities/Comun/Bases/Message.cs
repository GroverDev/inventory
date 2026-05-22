
using System;
using System.Collections.Generic;
using System.Text;
using Serilog;

namespace Common.Utilities;


public enum MessageTypes
{
    Nothing = 0,
    Warning = 1,
    Error = 2,
    Success = 3,
    Info = 4,
    Question = 5,

    //Obtiene nombre del string => Enum.GetName(MessageTypes.Nothing).ToLower();
}

public class Msg
{
    public string Description { get; set; }
    public string MessageType { get; set; }
    public string Id { get; set; }

    public Msg()
    {

        Description = "";
        MessageType = getMessageType(MessageTypes.Nothing);
        Id = "0";
    }



    internal void SetMessage(MessageTypes typeMessage, string description)
    {
        this.MessageType = getMessageType(typeMessage);
        this.Description = description;
        this.Id = "0";
    }
    internal void SetLogMessage(MessageTypes typeMessage, string description, Exception ex)
    {
        this.MessageType = getMessageType(typeMessage);
        this.Description = description;
        this.Id = "[" + GetTimeStamp() + "]";

        switch (typeMessage)
        {
            case MessageTypes.Nothing:
                Log.Information(ex, "Id: {IdMensaje}, {Mensaje}", this.Id, this.Description);
                break;
            case MessageTypes.Warning:
                Log.Warning(ex, "Id: {IdMensaje}, {Mensaje}", this.Id, this.Description);
                break;
            case MessageTypes.Error:
                Log.Error(ex, "Id: {IdMensaje}, {Mensaje}", this.Id, this.Description);
                break;
            case MessageTypes.Info:
                Log.Information(ex, "Id: {IdMensaje}, {Mensaje}", this.Id, this.Description);
                break;
            case MessageTypes.Success:
                Log.Information(ex, "Id: {IdMensaje}, {Mensaje}", this.Id, this.Description);
                break;
            default:
                Log.Information(ex, "Id: {IdMensaje}, {Mensaje}", this.Id, this.Description);
                break;
        }

    }
    private string getMessageType(MessageTypes messageType)
    {
        string type = "";
        switch (messageType)
        {
            case MessageTypes.Nothing:
                type = "Nothing";
                break;
            case MessageTypes.Warning:
                type = "Warning";
                break;
            case MessageTypes.Success:
                type = "Success";
                break;
            case MessageTypes.Error:
                type = "Error";
                break;
            case MessageTypes.Info:
                type = "Info";
                break;
            case MessageTypes.Question:
                type = "Question";
                break;
            default:
                type = "Nothing";
                break;

        }
        return type.ToLower();
    }
    private static string GetTimeStamp()
    {
        return DateTime.Now.ToString("yyyyMMddHHmmssfff");
    }
}

