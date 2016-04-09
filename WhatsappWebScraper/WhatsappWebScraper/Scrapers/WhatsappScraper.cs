using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRTestApp1.DataObjects;
using VRTestApp1.Extensions;

namespace VRTestApp1.Scrapers
{
    public class WhatsappScraper : IDisposable
    {
        private ChromeDriver Driver { get; set; }
        public WhatsappScraper()
        {
            Driver = new ChromeDriver();
            Driver.Manage().Window.Maximize();
        }

        public List<ChatBaseData> ScrapeWhatsapp()
        {
            var res = new List<ChatBaseData>();

            // set default load timeout to 15 seconds and load whatsapp entry point
            Driver.Manage().Timeouts().SetPageLoadTimeout(TimeSpan.FromSeconds(15));
            Driver.Navigate().GoToUrl("https://web.whatsapp.com/");

            // wait for specific elements within whatsapp's main page to load
            // this will indicate for the system that the chat was loaded 
            // (User has logged in). NOTE: Timeout is 10 minutes, waiting
            // on the login page for more than 10 minutes will crash the program
            //NOTE: THIS ARE EXTENSION METHODS. I implemented them. see SeleniumExtensions.cs file
            Driver.WaitForElement(By.ClassName("pane-two"));
            Driver.WaitForElement(By.CssSelector(".pane-body.pane-list-body"));

            // Load the chat items on the left sidebar
            var chats = Driver.FindElementsByCssSelector("div.infinite-list-item");
            foreach (var chat in chats)
            {
                var chatData = ScrapeChat(chat);
                res.Add(chatData);
            }

            return res;
        }

        /// <summary>
        /// Scrapes a specific chat
        /// </summary>
        /// <param name="chatButton"> the chat item on the left sidebar in the browser</param>
        private ChatBaseData ScrapeChat(IWebElement chatButton)
        {
            // fetch and load the current chat DOM
            var chatDom = LoadChat(chatButton);

            // extract the chat title
            var chatTitle = chatDom["h2.chat-title"]
                .Select(x => x.Cq().Text()).FirstOrDefault();

            // extract list of raw outcoming messages from the chat
            var outcomingMessages = chatDom[".message.message-out"];

            // extract list of raw incoming messages from the chat
            var incomingMessages = chatDom[".message.message-in"];

            var formattedOutcoming = FormatMessages(outcomingMessages, true);
            var formattedIncoming = FormatMessages(incomingMessages, false);
            var formattedMessages = new List<ChatMessage>();
            formattedMessages.AddRange(formattedOutcoming);
            formattedMessages.AddRange(formattedIncoming);

            return new ChatBaseData
            {
                ChatName = chatTitle,
                Messages = formattedMessages
            };
        }

        /// <summary>
        /// Dynamically loads the specified chat
        /// </summary>
        /// <param name="chatElement">The chat button on the left sidebar on the browser</param>
        /// <returns></returns>
        private CsQuery.CQ LoadChat(IWebElement chatElement)
        {
            // clicking on the chat item will open the chat
            // BUG: this might crash the program. in a scenario where
            // a message was received on another chat and the whole list of chats
            // has changed formation. we need to keep track of all clicked chats
            // and refresh the list after scraping a chat
            chatElement.Click();

            // validate that we loaded the chat by checking that the chat title is present
            // TODO: not sure if this is the best method for checking that the chat is loaded
            // maybe should use AJAX check (check if ajax call is complete)
            Driver.WaitForElement(By.CssSelector("h2.chat-title"));

            // click on the chat container to gain focus
            var chatContainer = Driver.FindElementByCssSelector(".message-list");
            chatContainer.Click();

            // scroll up the chat to create AJAX calls and load more dynamic content
            // TODO: add a PROPER method to scroll all the way up through the chat, this is
            // more of a proof of concept, just an ugly temp solution :P
            for (var i = 0; i < 10; i++)
            {
                var actions = new Actions(Driver);
                actions.SendKeys(Keys.PageUp).Perform();
                System.Threading.Thread.Sleep(1000);
            }

            // wrap the page source code with CsQuery for easy element selection           
            return CsQuery.CQ.CreateDocument(Driver.PageSource);
        }

        /// <summary>
        /// Formats the given CsQuery DOM Elements into a list Message objects with all 
        /// the relevant information
        /// </summary>
        /// 
        /// <param name="messages"> 
        /// The list of all DOM elements containing the chat messages
        /// </param>
        /// 
        /// <param name="isOutMessage"> 
        /// Indicates whether these are messages sent by the user (outcoming)
        /// or received by the user (sent by other users - incoming)
        /// </param>
        private List<ChatMessage> FormatMessages(CsQuery.CQ messages, bool isOutMessage)
        {
            var res = messages
                .Select(x => x.Cq())
                .Select(x =>
                {
                    // get the message author. in case this are out-messages, then the author is me
                    var author = (isOutMessage) ? "Me" : x.Find(".message-author .text-clickable").Text().Trim();

                    // try fetch the content. in case we can't fetch anything - then
                    // lets assume that the current message contains a photo and try
                    // fetch its url
                    var content = x.Find(".selectable-text").Text().Trim();

                    // in case the message contains an image instead of text
                    // TODO: understand how to use the string to extract the image
                    // TODO: add support for videos as well
                    if (string.IsNullOrEmpty(content))
                    {
                        content = x.Find(".image-thumb > img").Attr("src");
                    }

                    // TODO: handle this properly, right now we skip any messages
                    // that we couldn't find any text or url for
                    // we skip them by returning null here and then filtering
                    // any null messages, see few lines below \/ \/ \/ \/ \/
                    if (string.IsNullOrEmpty(content))
                    {
                        return null;
                    }

                    return new ChatMessage
                    {
                        Author = author,
                        Content = content
                    };
                })
                .Where(x => x != null)
                .ToList();

            // resolve author names for incoming messages
            if (!isOutMessage) ResolveAuthors(res);
            return res;
        }

        private void ResolveAuthors(List<ChatMessage> messages)
        {
            ChatMessage prevMsg = null;

            foreach (var msg in messages)
            {
                // in case the author of the message is empty, then it means that this is a continuation
                // message. in such scenario - we want to fetch the author from the previous message
                if (string.IsNullOrEmpty(msg.Author))
                {
                    msg.Author = prevMsg?.Author ?? "";
                }
                prevMsg = msg;
            }
        }

        public void Dispose()
        {
            Driver?.Dispose();
        }
    }
}
