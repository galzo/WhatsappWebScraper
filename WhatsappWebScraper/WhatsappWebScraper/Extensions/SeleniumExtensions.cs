using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System;
using System.Threading.Tasks;
using VRTestApp1.Extensions;

namespace VRTestApp1.Extensions
{
    public static class SeleniumExtensions
    {
        public static void WaitForAjax(this IWebDriver driver)
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
            wait.Until(d =>
            {
                var jsEngine = d as IJavaScriptExecutor;
                return jsEngine != null && (bool)jsEngine.ExecuteScript("return !!window.jQuery && window.jQuery.active ==0");
            });
        }

        public static async Task GoToUrlAsync(this IWebDriver driver, string url)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));
            await Task.Run(() => driver.Navigate().GoToUrl(url));
        }

        public static void ScrollToPageBottom(this IWebDriver driver)
        {
            var jsEngine = driver as IJavaScriptExecutor;
            jsEngine.ExecuteScript("window.scrollTo(0, document.body.scrollHeight);");
        }

        public static void ScrollToPageTop(this IWebDriver driver)
        {
            driver.ExecuteJsScript("window.scrollTo(250, 0);");
        }

        public static void ExecuteJsScript(this IWebDriver driver, string script)
        {
            var jsEngine = driver as IJavaScriptExecutor;
            jsEngine.ExecuteScript(script);
        }

        public static void WaitForElement(this IWebDriver driver, By by)
        {
            var wait = new WebDriverWait(driver, TimeSpan.FromMinutes(5));
            wait.Until(d =>
            {
                try
                {
                    var res = d.FindElement(by);
                    return res;
                }

                catch
                {
                    return null;
                }
            });
        }
    }
}
