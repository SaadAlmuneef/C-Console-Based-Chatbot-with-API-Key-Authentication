using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

class Program
{
    private static readonly HttpClient client = new HttpClient();
    private static string apiKey;
    private static string LogFilePath = "login_attempts.log";
    private const string Passcode = "1234";

    static async Task Main(string[] args)
    {
        // Display main menu
        while (true)
        {
            Console.Clear(); // Clear the console
            Console.WriteLine("1. Login");
            Console.WriteLine("2. How to get an API key");
            Console.WriteLine("3. View logs");
            Console.Write("Choose an option: ");
            string choice = Console.ReadLine();

            if (choice == "1")
            {
                await HandleLogin();
            }
            else if (choice == "2")
            {
                ShowApiKeyInstructions();
            }
            else if (choice == "3")
            {
                ViewLogs();
            }
            else
            {
                Console.WriteLine("Invalid option. Please choose again.");
            }
        }
    }

    private static async Task HandleLogin()
    {
        Console.Clear(); // Clear the console
        // Ask the user for the passcode
        Console.Write("Enter passcode: ");
        string enteredPasscode = Console.ReadLine();

        if (enteredPasscode != Passcode)
        {
            Console.WriteLine("Invalid passcode. Exiting...");
            return;
        }

        // Ask the user for the API key
        Console.Write("Please enter your OpenAI API key: ");
        apiKey = Console.ReadLine();

        // Validate the API key
        if (!await ValidateApiKey(apiKey))
        {
            Console.WriteLine("Invalid API key. Exiting...");
            LogLoginAttempt("Login failed", apiKey);
            return;
        }

        // Set the valid API key in the request headers
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        Console.WriteLine("API key validated successfully.");
        LogLoginAttempt("Login successful", apiKey);
        Console.WriteLine("___________________________________________________________________________________________________________________");
        Console.WriteLine("Welcome to the Chatbot! Type your messages below.");
        Console.WriteLine("Type 'exit' to quit.\n");

        while (true)
        {
            Console.Write("You: ");
            string userInput = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(userInput) || userInput.ToLower() == "exit") break;

            try
            {
                var response = await GetChatbotResponse(userInput);
                PrintAIResponse(response);
                Console.Beep();
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
            }
        }
    }

    private static void ShowApiKeyInstructions()
    {
        Console.Clear(); // Clear the console
        Console.WriteLine("To get an API key, follow these steps:");
        Console.WriteLine("1. Go to https://platform.openai.com/signup.");
        Console.WriteLine("2. Create an account or log in.");
        Console.WriteLine("3. Navigate to the API section to generate a new API key.");
        Console.WriteLine("4. Copy the API key and use it in this application.");
        Console.WriteLine("\nPress any key to return to the main menu...");
        Console.ReadKey(); // Wait for the user to press a key
    }

    private static async Task<bool> ValidateApiKey(string key)
    {
        // Create a test request to check if the API key is valid
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);
        var testRequestBody = new
        {
            model = "gpt-3.5-turbo",
            messages = new[] { new { role = "user", content = "test" } },
            max_tokens = 1
        };

        var json = JsonConvert.SerializeObject(testRequestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
            return response.IsSuccessStatusCode; // Returns true if the API key is valid
        }
        catch
        {
            return false; // If any error occurs, consider the API key invalid
        }
    }

    private static async Task<string> GetChatbotResponse(string input)
    {
        var requestBody = new
        {
            model = "gpt-3.5-turbo",
            messages = new[] { new { role = "user", content = input } },
            max_tokens = 150
        };

        var json = JsonConvert.SerializeObject(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        dynamic result = JsonConvert.DeserializeObject(responseBody);

        if (result.choices != null && result.choices.Count > 0)
        {
            return result.choices[0].message.content.ToString().Trim();
        }
        else
        {
            return "No response from AI.";
        }
    }

    private static void PrintAIResponse(string response)
    {
        Console.WriteLine("\n----------------------------------------");
        Console.WriteLine($"AI: {response}");
        Console.WriteLine("----------------------------------------\n");
    }

    private static void LogLoginAttempt(string state, string apiKey)
    {
        try
        {
            using (StreamWriter writer = new StreamWriter(LogFilePath, true))
            {
                // Log format: Date, State, API Key
                writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t{state}\t{apiKey}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to log login attempt: {ex.Message}");
        }
    }

    private static void ViewLogs()
    {
        Console.Clear(); // Clear the console
        try
        {
            if (File.Exists(LogFilePath))
            {
                using (StreamReader reader = new StreamReader(LogFilePath))
                {
                    string logContent = reader.ReadToEnd();
                    Console.WriteLine("\n--- Login Attempts Log ---");
                    Console.WriteLine(logContent);
                    Console.WriteLine("---------------------------\n");
                }
            }
            else
            {
                Console.WriteLine("No log file found.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to read log file: {ex.Message}");
        }

        Console.WriteLine("Press any key to return to the main menu...");
        Console.ReadKey(); // Wait for the user to press a key
    }
}
