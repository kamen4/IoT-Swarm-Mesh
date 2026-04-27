using System.IO.Ports;

namespace UartLS;

class Program
{
    static void Main(string[] args)
    {
        string portName = args.Length > 0 ? args[0] : PromptPortName();
        int baudRate = args.Length > 1 && int.TryParse(args[1], out int b) ? b : 115200;

        using var port = new SerialPort(portName, baudRate);

        port.DataReceived += (_, e) =>
        {
            if (e.EventType != SerialData.Chars) return;
            try
            {
                string data = port.ReadExisting();
                Console.WriteLine("[DEVICE] " + data);
            }
            catch { }
        };

        port.Open();
        Console.WriteLine($"Opened {portName} at {baudRate} baud. Type to send, Ctrl+C to exit.");

        while (true)
        {
            string? input = Console.ReadLine();
            if (input == null) break;
            try { port.Write(input); }
            catch { break; }
        }
    }

    static string PromptPortName()
    {
        string[] available = SerialPort.GetPortNames();
        if (available.Length > 0)
            Console.WriteLine("Available ports: " + string.Join(", ", available));
        Console.Write("Enter COM port name: ");
        return Console.ReadLine() ?? throw new InvalidOperationException("No port name provided.");
    }
}

