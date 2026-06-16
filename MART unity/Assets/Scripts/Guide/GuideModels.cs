using System;

[Serializable]
public class GuideSuggestionItem
{
    public string topicId;
    public string label;
}

[Serializable]
public class GuideSuggestionsDto
{
    public string welcome;
    public GuideSuggestionItem[] suggestions;
}

[Serializable]
public class GuideChatResponseDto
{
    public string reply;
    public string topicId;
    public string source;
}

[Serializable]
public class GuideChatTopicRequest
{
    public string topicId;
}

[Serializable]
public class GuideChatMessageRequest
{
    public string message;
}

[Serializable]
public class GuideTopic
{
    public string id;
    public string[] keywords;
    public string answer;
}

[Serializable]
public class GuideDataBundle
{
    public string welcome;
    public string offTopic;
    public string noMatch;
    public GuideSuggestionItem[] suggestions;
    public GuideTopic[] topics;
}
