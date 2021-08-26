using UnityEngine;
using System;
using System.Collections.Generic;

static class VTubeData
{
    public const string AuthenticationToken = "AuthenticationToken";
    public const string Authentication = "Authentication";
    public const string Statistics = "Statistics";
    public const string FolderInfo = "VTSFolderInfo";
    public const string CurrentModel = "CurrentModel";
    public const string AvailableModels = "AvailableModels";
    public const string ModelLoad = "ModelLoad";
    public const string MoveModel = "MoveModel";
    public const string HotkeysInCurrentModel = "HotkeysInCurrentModel";
    public const string HotkeyTrigger = "HotkeyTrigger";
    public const string ArtMeshList = "ArtMeshList";
    public const string ColorTint = "ColorTint";
    public const string InputParameterList = "InputParameterList";
    public const string ParameterValue = "ParameterValue";
    public const string Live2DParameterList = "Live2DParameterList";
    public const string ParameterCreation = "ParameterCreation";
    public const string ParameterDeletion = "ParameterDeletion";
    public const string InjectParameterData = "InjectParameterData";

    public static string GetType(string messageType)
    {
        if (messageType.EndsWith("Response"))
            return messageType.Replace("Response", "");

        if (messageType.EndsWith("Request"))
            return messageType.Replace("Request", "");

        return messageType;
    }

    public static bool IsResponse(string messageType)
    {
        return messageType.EndsWith("Response");
    }

    public static bool IsRequest(string messageType)
    {
        return messageType.EndsWith("Request");
    }
}



[Serializable]
public class VTubeMessage
{
    public string apiName = "VTubeStudioPublicAPI";
    public string apiVersion = "1.0";
    //public string requestID = "";
    public string messageType;

    public VTubeMessage() { }
    public VTubeMessage(string MessageType)
    {
        messageType = MessageType;
    }
}

[Serializable]
public class VTubeAuthenticationToken : VTubeMessage
{
    [Serializable]
    public class Package
    {
        public string pluginName;
        public string pluginDeveloper;
        //public string pluginIcon;
    }

    public Package data;

    public VTubeAuthenticationToken(Sprite icon)
    {
        messageType = VTubeData.AuthenticationToken + "Request";
        data = new Package();
        data.pluginName = Application.productName;
        data.pluginDeveloper = Application.companyName;
    }
}

[Serializable]
public class VTubeAuthentication : VTubeMessage
{
    [Serializable]
    public class Package
    {
        public string authenticationToken;
        public string pluginName;
        public string pluginDeveloper;
    }

    public Package data;

    public VTubeAuthentication(string AuthenticationToken)
    {
        messageType = VTubeData.Authentication + "Request";
        data = new Package();
        data.authenticationToken = AuthenticationToken;
        data.pluginName = Application.productName;
        data.pluginDeveloper = Application.companyName;
    }
}

[Serializable]
public class VTubeStatisticsRequest : VTubeMessage
{
    public VTubeStatisticsRequest()
    {
        messageType = VTubeData.Statistics + "Request";
    }
}

[Serializable]
public class VTubeStatisticsResponse : VTubeMessage
{
    public VTubeStatisticsResponse()
    {
        messageType = VTubeData.Statistics + "Request";
    }
}

[Serializable]
public class VTubeParameterCreationRequest : VTubeMessage
{
    [Serializable]
    public class Package
    {
        public string parameterName;
        public string explanation;
        public int min;
        public int max;
        public int defaultValue;
    }

    public Package data;

    public VTubeParameterCreationRequest(string parameterName, string explanation, int min, int max, int defaultValue = 0)
    {
        messageType = VTubeData.ParameterCreation + "Request";
        data = new Package();
        data.parameterName = parameterName;
        data.explanation = explanation;
        data.min = min;
        data.max = max;
        data.defaultValue = defaultValue;
    }
}

[Serializable]
public class InjectParameterData : VTubeMessage
{
    [Serializable]
    public class Package
    {
        [Serializable]
        public class ParameterValue
        {
            public string id;
            public float value;
            public float weight;
        }
        public List<ParameterValue> parameterValues;
    }

    public Package data;

    public InjectParameterData()
    {
        messageType = VTubeData.InjectParameterData + "Request";
        data = new Package();
        data.parameterValues = new List<Package.ParameterValue>();
    }

    public void AddParameter(string id, float value, float weight = 1.0f)
    {
        Package.ParameterValue val = new Package.ParameterValue();
        val.id = id;
        val.value = value;
        val.weight = weight;
        data.parameterValues.Add(val);
    }
}