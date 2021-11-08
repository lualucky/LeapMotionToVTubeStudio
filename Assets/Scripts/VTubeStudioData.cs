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
        public string pluginIcon;
    }

    public Package data;

    public VTubeAuthenticationToken(Sprite icon)
    {
        messageType = VTubeData.AuthenticationToken + "Request";
        data = new Package();
        data.pluginName = Application.productName;
        data.pluginDeveloper = Application.companyName;

        if (icon.texture.width == 128 && icon.texture.height == 128)
            data.pluginIcon = System.Convert.ToBase64String(icon.texture.EncodeToPNG());
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
public class VTubeInjectParameterData : VTubeMessage
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

    public VTubeInjectParameterData()
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

[Serializable]
public class VTubeColorTintRequest : VTubeMessage
{
    [Serializable]
    public class Package
    {
        [Serializable]
        public class ColorPackage
        {
            public int colorR;
            public int colorG;
            public int colorB;
            public int colorA;
            public bool jeb_;
        }
        [Serializable]
        public class ArtMeshPackage
        {
            public bool tintAll;
            public List<int> artMeshNumber;
            public List<string> nameExact;
            public List<string> nameContains;
            public List<string> tagExact;
            public List<string> tagContains;
        }

        public ColorPackage colorTint;
        public ArtMeshPackage artMeshMatcher;
    }

    public Package data;

    public VTubeColorTintRequest(Color color, List<int> artMeshNumber, List<string> nameExact, List<string> nameContains, List<string> tagExact, List<string> tagContains, bool tintAll = false)
    {
        messageType = VTubeData.ColorTint + "Request";
        data = new Package();
        data.artMeshMatcher = new Package.ArtMeshPackage();
        data.colorTint = new Package.ColorPackage();

        data.colorTint.colorR = Mathf.RoundToInt(color.r * 255);
        data.colorTint.colorG = Mathf.RoundToInt(color.g * 255);
        data.colorTint.colorB = Mathf.RoundToInt(color.b * 255);
        data.colorTint.colorA = Mathf.RoundToInt(color.a * 255);

        data.artMeshMatcher.tintAll = tintAll;
        data.artMeshMatcher.artMeshNumber = artMeshNumber;
        data.artMeshMatcher.nameExact = nameExact;
        data.artMeshMatcher.nameContains = nameContains;
        data.artMeshMatcher.tagExact = tagExact;
        data.artMeshMatcher.tagContains = tagContains;
    }

    public VTubeColorTintRequest(List<int> artMeshNumber, List<string> nameExact, List<string> nameContains, List<string> tagExact, List<string> tagContains, bool tintAll = false)
    {
        messageType = VTubeData.ColorTint + "Request";
        data = new Package();
        data.artMeshMatcher = new Package.ArtMeshPackage();
        data.colorTint = new Package.ColorPackage();

        data.colorTint.jeb_ = true;

        data.artMeshMatcher.tintAll = tintAll;
        data.artMeshMatcher.artMeshNumber = artMeshNumber;
        data.artMeshMatcher.nameExact = nameExact;
        data.artMeshMatcher.nameContains = nameContains;
        data.artMeshMatcher.tagExact = tagExact;
        data.artMeshMatcher.tagContains = tagContains;
    }
}

[Serializable]
public class VTubeParameterValueRequest : VTubeMessage
{
    [Serializable]
    public class Package
    {
        public string name;
    }

    public Package data;

    public VTubeParameterValueRequest(string parameterName)
    {
        messageType = VTubeData.ParameterValue + "Request";
        data = new Package();
        data.name = parameterName;
    }
}

public class VTubeParameterValueData
{
    public string name;
    public float value;
    public string addedBy;
    public float min;
    public float max;
    public float defaultValue;

    public VTubeParameterValueData(string parameterName)
    {
        name = parameterName;
    }

    public void SetValue(float newValue)
    {
        value = newValue;
    }
}

[Serializable]
public class VTubeLive2DParameterListRequest : VTubeMessage
{
    public VTubeLive2DParameterListRequest()
    {
        messageType = VTubeData.Live2DParameterList + "Request";
    }
}