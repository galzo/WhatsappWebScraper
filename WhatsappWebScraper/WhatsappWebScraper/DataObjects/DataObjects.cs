using System.Collections.Generic;

namespace VRTestApp1.DataObjects
{
    public class ChatBaseData
    {
        public string ChatName { get; set; }
        public List<ChatMessage> Messages { get; set; }
    }

    public class ChatMessage
    {
        public string Author { get; set; }
        public string Content { get; set; }
    }
}
