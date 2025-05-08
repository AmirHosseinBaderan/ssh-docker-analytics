using Renci.SshNet;
using Spectre.Console;
using static System.Console;

string host = GetValidInput("Enter Server IP: ");
string username = GetValidInput("Enter Username: ");
string password = GetValidPassword("Enter Password: ");

using var client = new SshClient(host, username, password);
client.Connect();

while (true)
{
    var cmd = client.RunCommand("sudo docker stats --no-stream --format \"{{.Name}} {{.CPUPerc}} {{.MemUsage}}\"");
    var output = cmd.Result.Split("\n", StringSplitOptions.RemoveEmptyEntries);

    var statsList = output.Select(line =>
    {
        var parts = line.Split(' ');
        string name = parts[0];
        double cpuUsage = double.Parse(parts[1].Replace("%", ""));
        double memoryUsage = double.Parse(parts[2].Split('/')[0].Replace("MiB", "").Replace("GiB", "")) * (parts[2].Contains("GiB") ? 1024 : 1);
        return (name, cpuUsage, memoryUsage);
    }).ToList();

    Console.Clear();

    AnsiConsole.Write(new BarChart()
        .Width(60)
        .Label("CPU Usage (via SSH)")
        .AddItems(statsList.Select(c => new BarChartItem(c.name, c.cpuUsage, Color.Green))));

    AnsiConsole.Write(new BarChart()
        .Width(60)
        .Label("Memory Usage (via SSH)")
        .AddItems(statsList.Select(c => new BarChartItem(c.name, c.memoryUsage, Color.Blue))));

    await Task.Delay(5000);
}

/// Function to get a valid input from user
static string GetValidInput(string prompt)
{
    string? input;
    do
    {
        Write(prompt);
        input = ReadLine()?.Trim();
        if (string.IsNullOrEmpty(input))
            WriteLine("Input cannot be empty. Please enter a valid value.");
    } while (string.IsNullOrEmpty(input));

    return input;
}

/// Function to securely read the password without displaying it in the console
static string GetValidPassword(string prompt)
{
    string password = "";
    Write(prompt);

    while (true)
    {
        var key = ReadKey(true);
        if (key.Key == ConsoleKey.Enter) break;
        if (key.Key == ConsoleKey.Backspace && password.Length > 0)
        {
            password = password[..^1];
            Write("\b \b"); // Remove last character
        }
        else
        {
            password += key.KeyChar;
            Write("*"); // Mask input
        }
    }
    WriteLine();

    if (string.IsNullOrEmpty(password))
    {
        WriteLine("Password cannot be empty. Please enter a valid password.");
        return GetValidPassword(prompt);
    }

    return password;
}
