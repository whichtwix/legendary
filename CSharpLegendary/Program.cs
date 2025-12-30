using System.Diagnostics;
using System.Text.Json.Nodes;

namespace CSharpLegendary
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting Login Attempts");
            
            var api = new EpicApi();
            JsonNode session = null!;

            var savedSession = await TokenStore.LoadTokensAsync();

            if (savedSession != null)
            {
                string refreshToken = savedSession["refresh_token"].ToString();
                string accessToken = savedSession["access_token"].ToString();

                if (!string.IsNullOrEmpty(refreshToken))
                {
                    Console.WriteLine("Found saved session. Attempting refresh...");
                    try
                    {
                        session = await api.StartSessionAsync(refreshToken: refreshToken);
                        Console.WriteLine("Refresh Successful!");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Refresh failed (Token expired?): {ex.Message}");
                    }
                }
                
                if (session == null && !string.IsNullOrEmpty(accessToken))
                {
                    Console.WriteLine("Attempting to resume session with Access Token...");
                    try
                    {
                        session = await api.ResumeSessionAsync(accessToken);
                        Console.WriteLine("Session Resumed!");
                    }
                    catch
                    {
                        Console.WriteLine("Access Token expired.");
                    }
                }
            }

            if (session == null)
            {
                Console.WriteLine("\n--- Manual Login Required ---");
                string loginUrl = api.GetAuthUrl();
                
                Console.WriteLine("1. Open this URL in your browser if it does not open automatically:");
                Console.WriteLine(loginUrl);
                
                
                Process.Start(new ProcessStartInfo { FileName = loginUrl, UseShellExecute = true }); 
                

                Console.WriteLine("2. Copy Paste the 'authorizationCode' text showing on the browser into here.");
                Console.Write("Enter Code: ");
                string code = Console.ReadLine().Trim();

                if (string.IsNullOrEmpty(code)) 
                {
                    Console.WriteLine("No code was provided, please try again by reopening the exe");
                    Console.ReadKey();
                    return;
                }
                
                code = code.Replace("\"", "").Trim();

                try
                {
                    session = await api.StartSessionAsync(authCode: code);
                    Console.WriteLine("Login Successful!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"FATAL: Login failed. {ex.Message}");
                    return;
                }
            }

            if (session != null)
            {
                await TokenStore.SaveTokensAsync(session);
            }

            try 
            {
                string launchToken = await api.GetGameTokenAsync();
                Console.WriteLine("READY TO LAUNCH");

                var AUexe = new ProcessStartInfo() { FileName = "Among Us.exe" };
                AUexe.ArgumentList.Add($"-AUTH_PASSWORD={launchToken}");
                Process.Start(AUexe);

                Console.WriteLine("Among Us Process started");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get launch token: {ex.Message}");
            }
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}