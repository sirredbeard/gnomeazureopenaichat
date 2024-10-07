using System;
using System.IO;
using System.Threading.Tasks;
using System.ClientModel;
using System.Reflection;
using Gtk;
using Azure;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using static System.Environment;

class Program
{
    static string apiKey = "";
    static string endpoint = "";
    static string systemMessage = "";
    static string deployment = "";
    static string configFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gnomeazureopenaichat");

    static void Main(string[] args)
    {
        Application.Init();
        LoadConfig();

        var win = new Window("Chat with Azure OpenAI");
        win.SetDefaultSize(600, 800);
        win.DeleteEvent += delegate { Application.Quit(); };

        var vbox = new VBox();
        var conversationTextView = new TextView { Editable = false, WrapMode = WrapMode.Word };
        var conversationScroll = new ScrolledWindow();
        conversationScroll.Add(conversationTextView);

        var entryBox = new HBox();
        var entry = new Entry();
        entry.KeyPressEvent += async (o, args) =>
        {
            if (args.Event.Key == Gdk.Key.Return || args.Event.Key == Gdk.Key.KP_Enter)
            {
                await SendMessage(entry, conversationTextView);
                args.RetVal = true; // Prevent the default handler from running
            }
        };

        var sendButton = new Button("Send");
        sendButton.Clicked += async (sender, e) => await SendMessage(entry, conversationTextView);

        entryBox.PackStart(entry, true, true, 0);
        entryBox.PackStart(sendButton, false, false, 0);

        var topButtonBox = new HBox();
        var stopButton = new Button();
        stopButton.Image = new Image(Stock.Stop, IconSize.Button); // GTK stop symbol
        stopButton.Clicked += (sender, e) => conversationTextView.Buffer.Text = "";

        var infoButton = new Button();
        infoButton.Image = new Image(Stock.Info, IconSize.Button); // GTK info symbol
        infoButton.Clicked += (sender, e) => ShowInfoDialog(win);

        topButtonBox.PackEnd(infoButton, false, false, 0);
        topButtonBox.PackEnd(stopButton, false, false, 0);

        vbox.PackStart(topButtonBox, false, false, 0); // Move buttonBox to the top
        vbox.PackStart(conversationScroll, true, true, 0);
        vbox.PackStart(entryBox, false, false, 0);

        win.Add(vbox);
        win.ShowAll();
        Application.Run();
    }

    static async Task SendMessage(Entry entry, TextView conversationTextView)
    {
        string userMessage = entry.Text;
        if (string.IsNullOrWhiteSpace(userMessage)) return;

        conversationTextView.Buffer.Text += $"You: {userMessage}\n";
        entry.Text = "";

        try
        {
            AzureOpenAIClient azureClient = new(
                new Uri(endpoint),
                new ApiKeyCredential(apiKey));
            var chatClient = azureClient.GetChatClient(deployment);

            var chatUpdates = chatClient.CompleteChatStreamingAsync(
                new ChatMessage[]
                {
                    new SystemChatMessage(systemMessage),
                    new UserChatMessage(userMessage)
                });

            string assistantResponse = "";
            await foreach (var chatUpdate in chatUpdates)
            {
                foreach (var contentPart in chatUpdate.ContentUpdate)
                {
                    assistantResponse += contentPart.Text;
                }
            }
            conversationTextView.Buffer.Text += $"Assistant: {assistantResponse}\n";
        }
        catch (Exception ex)
        {
            conversationTextView.Buffer.Text += $"Error: {ex.Message}\n";
        }
    }

    static void ShowInfoDialog(Window parent)
    {
        using (var dialog = new Dialog("API Configuration", parent, DialogFlags.Modal))
        {
            var endpointEntry = new Entry { Text = endpoint };
            var apiKeyEntry = new Entry { Text = apiKey };
            var deploymentEntry = new Entry { Text = deployment };
            var systemMessageEntry = new Entry { Text = systemMessage };

            dialog.ContentArea.PackStart(new Label("Endpoint:"), false, false, 0);
            dialog.ContentArea.PackStart(endpointEntry, false, false, 0);
            dialog.ContentArea.PackStart(new Label("API Key:"), false, false, 0);
            dialog.ContentArea.PackStart(apiKeyEntry, false, false, 0);
            dialog.ContentArea.PackStart(new Label("Deployment:"), false, false, 0);
            dialog.ContentArea.PackStart(deploymentEntry, false, false, 0);
            dialog.ContentArea.PackStart(new Label("System Message:"), false, false, 0);
            dialog.ContentArea.PackStart(systemMessageEntry, false, false, 0);

            var saveButton = new Button("Save");
            saveButton.Clicked += (sender, e) =>
            {
                endpoint = endpointEntry.Text;
                apiKey = apiKeyEntry.Text;
                deployment = deploymentEntry.Text;
                systemMessage = systemMessageEntry.Text;
                SaveConfig();
                dialog.Destroy();
            };

            dialog.ActionArea.PackStart(saveButton, false, false, 0);
            dialog.ShowAll();
            dialog.Run();
        }
    }

    static void LoadConfig()
    {
        if (File.Exists(configFilePath))
        {
            var lines = File.ReadAllLines(configFilePath);
            if (lines.Length >= 4)
            {
                apiKey = lines[0];
                endpoint = lines[1];
                systemMessage = lines[2];
                deployment = lines[3];
            }
        }
    }

    static void SaveConfig()
    {
        var lines = new[] { apiKey, endpoint, systemMessage, deployment };
        File.WriteAllLines(configFilePath, lines);
    }
}